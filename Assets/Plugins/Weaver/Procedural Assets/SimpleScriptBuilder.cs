// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only]
    /// A simple <see cref="ScriptBuilder"/> which builds a script containing a single class.
    /// </summary>
    public abstract class SimpleScriptBuilder : ScriptBuilder
    {
        /************************************************************************************************************************/

        /// <summary>The method used to generate the <see cref="ProceduralAsset"/>.</summary>
        public readonly Action<StringBuilder> GeneratorMethod;

        /// <summary>Indicates whether the procedural asset has been gathered.</summary>
        private bool _IsInitialized;

        /// <summary>The <see cref="ProceduralAsset"/> which uses this builder.</summary>
        public ProceduralAsset ProceduralAsset { get { Initialize(); return _ProceduralAsset; } }
        private ProceduralAsset _ProceduralAsset;

        /// <summary>The information about an existing type with the same name as this script (if any).</summary>
        public CachedTypeInfo ExistingType { get { Initialize(); return _ExistingType; } }
        private CachedTypeInfo _ExistingType;

        /// <summary>The builder for the root type of the script.</summary>
        public TypeBuilder RootType { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// If true: the generated script will include all previous members that have been removed and give them
        /// [<see cref="ObsoleteAttribute"/>].
        /// </summary>
        public bool RetainObsoleteMembers { get; set; } = true;

        /// <summary>If true, errors encountered while building the script will be logged. Default true.</summary>
        public bool LogBuildErrors { get; set; } = true;

        /// <summary>
        /// If true, <c>#pragma warning disable</c> will be put at the top of the script to disable all warnings.
        /// Default true.
        /// </summary>
        public bool DisableAllWarnings { get; set; } = true;

        /// <summary>The namespace to put the root type in.</summary>
        public override string Namespace => ProceduralAssetSettings.Instance.scriptsUseWeaverNamespace ? nameof(Weaver) : null;

        /// <summary>Override to return false when you don't want the script to be generated.</summary>
        public virtual bool Enabled => true;

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="SimpleScriptBuilder"/> which will be used to generate the procedural script
        /// associated with the specified `generatorMethod`.
        /// </summary>
        protected SimpleScriptBuilder(Action<StringBuilder> generatorMethod)
        {
            GeneratorMethod = generatorMethod;
        }

        /************************************************************************************************************************/

        private void Initialize()
        {
            if (_IsInitialized)
                return;

            _IsInitialized = true;

            _ProceduralAsset = ProceduralAsset.GetFromGenerator(GeneratorMethod.Method);

            if (ProceduralAsset == null)
            {
                InjectorManager.AssertIsInitialized();
                Debug.LogError($"There is no {nameof(ProceduralAsset)} associated with {GeneratorMethod.Method.GetNameCS()}.");
                return;
            }

            var name = ProceduralAsset.Injector.FileName;
            if (name == null)
                throw new NullReferenceException($"An {nameof(AssetInjectionAttribute)}.{nameof(AssetInjectionAttribute.FileName)}" +
                    $" must be specified for the procedural script to use as the root type name: {ProceduralAsset}.");

            var shouldRebuild = false;
            _ExistingType = CachedTypeInfo.FindExistingType(name, "Weaver." + name, ref shouldRebuild);
        }

        /************************************************************************************************************************/

        /// <summary>If <see cref="ShouldBuild"/> returns true, this method calls <see cref="AppendScript"/>.</summary>
        public bool BuildScript(StringBuilder text, bool forceBuild = false)
        {
            if (ShouldBuild() || forceBuild)
            {
                AppendScript(text);
                forceBuild = true;
            }
            else
            {
                text.Length = 0;
                forceBuild = false;
            }

            RetainObsoleteMembers = true;
            return forceBuild;
        }

        /************************************************************************************************************************/

        /// <summary>Indicates whether the script should be rebuilt based on whether its contents need to be changed.</summary>
        public virtual bool ShouldBuild()
        {
            ReleaseElementsToPool();
            RootType = null;

            var proceduralAsset = ProceduralAsset;
            if (proceduralAsset == null)
                return false;

            if (!Enabled)
            {
                var script = proceduralAsset.Injector.Asset;
                if (script != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(script);
                    Debug.Log($"Deleting {assetPath} because its generator has been disabled.");
                    AssetDatabase.DeleteAsset(assetPath);
                }

                return false;
            }

            try
            {
                if (WeaverEditorUtilities.IsPreprocessingBuild ||
                    !ProceduralAssetSettings.Instance.scriptsKeepObsoleteMembers)
                    RetainObsoleteMembers = false;

                SetName(ProceduralAssetSettings.Instance.scriptsUseWeaverNamespace ? nameof(Weaver) : null);

                RootType = AddType(proceduralAsset.Injector.FileName, ExistingType);
                RootType.CommentBuilder = (comment) =>
                {
                    comment
                        .Append("This class was procedurally generated by ")
                        .Append(ScriptGenerator.AliasAttribute.GetAlias(GeneratorMethod.Method))
                        .Append('.');
                };

                GatherScriptDetails();

                if (PrepareToBuild(RetainObsoleteMembers, LogBuildErrors))
                    return true;

                if (proceduralAsset.Injector.Asset == null)
                {
                    LogRebuildReason("The script does not exist yet");
                    return true;
                }

                if (WeaverEditorUtilities.ForceGenerate)
                {
                    LogRebuildReason($"{nameof(WeaverEditorUtilities)}.{nameof(WeaverEditorUtilities.ForceGenerate)} is true");
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return false;
        }

        /// <summary>Gathers the element details of this script.</summary>
        protected abstract void GatherScriptDetails();

        /************************************************************************************************************************/

        /// <summary>Appends the declaration of the elements of this script in C# code to the specified `text`.</summary>
        protected virtual void AppendScript(StringBuilder text)
        {
            try
            {
                if (!Enabled)
                {
                    text.Length = 0;
                    return;
                }

                if (Elements.Count == 0)
                {
                    text.AppendLineConst("// This script is currently empty.");
                    return;
                }

                if (DisableAllWarnings)
                {
                    text.AppendLineConst("#pragma warning disable // All.");
                    text.AppendLineConst();
                }

                if (ScriptGenerator.SaveMessage != null)
                    ScriptGenerator.SaveMessage.Append("Retain Obsolete Members: ").AppendLineConst(RetainObsoleteMembers);

                AppendScript(text, 0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.Log($"The previous exception ocurred while building" +
                    $" {ScriptGenerator.AliasAttribute.GetAlias(GeneratorMethod.Method)}:\n{text}");
                text.Length = 0;
            }
            finally
            {
                RetainObsoleteMembers = true;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the target file exists but its root type cannot be retrieved via reflection.</summary>
        public bool ScriptExistsButIsntCompiled => ExistingType == null && ProceduralAsset.Injector.Asset != null;

        /************************************************************************************************************************/

        /// <summary>Returns true if there are any obsolete members in the existing root type.</summary>
        public bool HasAnyObsoleteMembers() => ExistingType != null && ExistingType.HasAnyObsoleteMembers();

        /************************************************************************************************************************/

        /// <summary>Rebuilds the script without any obsolete members.</summary>
        public void RebuildScriptWithoutObsoleteMembers()
        {
            RetainObsoleteMembers = false;
            Generate();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Generates the <see cref="ProceduralAsset"/> which uses this script builder.
        /// </summary>
        public void Generate()
        {
            AssetGeneratorWindow.AssetsToGenerate.Add(ProceduralAsset);
            AssetGeneratorWindow.Generate(true);
        }

        /************************************************************************************************************************/

        /// <summary>Logs the `reason` that this script should be rebuilt.</summary>
        public override void LogRebuildReason(string reason)
            => ScriptGenerator.LogRebuildReason(ProceduralAsset.CurrentAssetPath, reason);

        /************************************************************************************************************************/

        private static Dictionary<Type, SimpleScriptBuilder> _TypeToBuilder;

        /// <summary>Returns an instance of the specified <see cref="SimpleScriptBuilder"/> type.</summary>
        public static SimpleScriptBuilder GetBuilderInstance(Type type)
        {
            if (type == null)
                return null;

            if (_TypeToBuilder == null)
                _TypeToBuilder = new Dictionary<Type, SimpleScriptBuilder>();

            if (!_TypeToBuilder.TryGetValue(type, out var builder))
            {
                if (type.IsSubclassOf(typeof(SimpleScriptBuilder)))
                {
                    var fields = type.GetFields(ReflectionUtilities.StaticBindings);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (field.FieldType == type)
                        {
                            builder = field.GetValue(null) as SimpleScriptBuilder;
                            break;
                        }
                    }
                }

                _TypeToBuilder.Add(type, builder);
            }

            return builder;
        }

        /************************************************************************************************************************/
    }
}

#endif

