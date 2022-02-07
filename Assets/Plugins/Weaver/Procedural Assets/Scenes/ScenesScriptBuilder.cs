// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the scenes in your build settings.
    /// </summary>
    public sealed class ScenesScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ScenesScriptBuilder"/>.</summary>
        public ScenesScriptBuilder(Action<StringBuilder> generatorMethod) : base(generatorMethod) { }

        /************************************************************************************************************************/

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => WeaverSettings.Scenes.enabled;

        /************************************************************************************************************************/

        /// <summary>Gathers the element details of this script.</summary>
        protected override void GatherScriptDetails()
        {
            var scenes = EditorBuildSettings.scenes;

            var index = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (!scene.enabled ||
                    scene.path == "")
                    continue;

                // Field Name.
                var name = GetFieldName(scene.path);

                // Declaring Type.
                var declaringType = GetOrCreateDeclaringType(scene.path);

                // Scene Index.
                if (WeaverSettings.Scenes.includeSceneIndices)
                {
                    var fieldName = name;
                    if (WeaverSettings.Scenes.includeSceneNames)
                        fieldName += " Index";

                    var field = declaringType.AddField(fieldName, index);
                    field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                    field.CommentBuilder = (text) => text.Append(scene.path);
                }

                // Scene Name.
                if (WeaverSettings.Scenes.includeSceneNames)
                {
                    var fieldName = name;
                    if (WeaverSettings.Scenes.includeSceneIndices)
                        fieldName += " Name";

                    var field = declaringType.AddField(fieldName, name);
                    field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                    field.CommentBuilder = (text) => text.Append(scene.path);
                }

                index++;
            }

            AddIndexToNameMethod(scenes);
        }

        /************************************************************************************************************************/

        public override string GetPrimaryMemberName(string nameSource)
        {
            return CSharpProcedural.ValidateMemberName(nameSource, WeaverSettings.Scenes.useFullPathNames);
        }

        /************************************************************************************************************************/

        private static string GetFieldName(string path)
        {
            if (WeaverSettings.Scenes.useFullPathNames)
            {
                var dot = WeaverUtilities.GetFileExtensionIndex(path);
                if (dot >= 0)
                    return path.Substring(0, dot);
                else
                    return path;
            }
            else
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        }

        /************************************************************************************************************************/

        private TypeBuilder GetOrCreateDeclaringType(string path)
        {
            if (WeaverSettings.Scenes.useNestedClasses)
            {
                // Skip the "Assets/" prefix for determining the type path, but still include it in the type comments.
                var subPath = new Substring(path, 7);
                return RootType.GetOrAddNestedTypesForDirectories(subPath, 0);
            }
            else
            {
                return RootType;
            }
        }

        /************************************************************************************************************************/

        private static readonly ParameterBuilder[]
            IndexToNameParameters = { new ParameterBuilder(typeof(int), "index") };

        private MethodBuilder AddIndexToNameMethod(EditorBuildSettingsScene[] scenes)
        {
            var method = RootType.AddMethod("IndexToName", typeof(string), IndexToNameParameters, (text, indent) =>
             {
                 text.Indent(indent).AppendLineConst("switch (index)");
                 text.OpenScope(ref indent);

                 var sceneIndex = 0;
                 for (int i = 0; i < scenes.Length; i++)
                 {
                     var scene = scenes[i];
                     if (!scene.enabled)
                         continue;

                     var nameSource = Path.GetFileNameWithoutExtension(scene.path);

                     text.Indent(indent).Append("case ").Append(sceneIndex).Append(": return \"").Append(nameSource).AppendLineConst("\";");

                     sceneIndex++;
                 }

                 text.Indent(indent).AppendLineConst("default: return null;");
                 text.CloseScope(ref indent);
             });

            method.CommentBuilder =
                (text) => text.Append("Returns the name of the scene associated with the specified scene 'index' in the build settings.");
            return method;
        }

        /************************************************************************************************************************/
    }
}

#endif

