// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System;
using System.Text;
using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// A procedural script builder which automatically numbers and timestamps each build.
    /// <para></para>
    /// The documentation for this example can be found at
    /// https://kybernetik.com.au/weaver/docs/examples/build-details
    /// </summary>
    internal sealed class BuildDetailsScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        /// <summary>The procedurally generated script asset.</summary>
        [AssetReference(FileName = "BuildDetails", DisableAutoFind = true, Optional = true)]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="ProceduralAssetAttribute.CheckShouldGenerate"/> value set on the <see cref="Script"/>
        /// property directs it to use this property to determine whether to allow the asset to be regenerated.
        /// <para></para>
        /// Most scripts can just use <see cref="SimpleScriptBuilder.ShouldBuild"/> to determine whether any members
        /// have changed. But this script doesn't actually care about the members so it just checks
        /// <see cref="WeaverEditorUtilities.ForceGenerate"/> and
        /// <see cref="WeaverEditorUtilities.IsPreprocessingBuild"/>.
        /// </summary>
        private static bool ShouldGenerateScript
        {
            get
            {
                return
                    WeaverEditorUtilities.ForceGenerate ||
                    WeaverEditorUtilities.IsPreprocessingBuild ||
                    Script == null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="ScriptBuilder"/> instance used to generate this procedural script.</summary>
        private static readonly BuildDetailsScriptBuilder Instance = new BuildDetailsScriptBuilder();

        /// <summary>
        /// Creates a new <see cref="BuildDetailsScriptBuilder"/> using <see cref="GenerateScript"/> as the
        /// generator method.
        /// </summary>
        private BuildDetailsScriptBuilder()
            : base(GenerateScript)
        { }

        /// <summary>Appends the contents of this procedural script to the `text`.</summary>
        private static void GenerateScript(StringBuilder text)
        {
            Instance.RetainObsoleteMembers = false;
            Instance.BuildScript(text, WeaverEditorUtilities.IsBuilding);
        }

        /************************************************************************************************************************/

        private FieldBuilder _BuildNumber;
        private FieldBuilder _BuildDate;

        /// <summary>
        /// Partially initializes a <see cref="FieldBuilder"/> for the <see cref="_BuildNumber"/> and
        /// <see cref="_BuildDate"/>.
        /// </summary>
        protected override void GatherScriptDetails()
        {
            // Normally this method would configure all the member builders here so the ScriptBuilder can compare them
            // to existing members to determine whether the script actually needs to be regenerated.

            // But we need to react based on the existing field values and the builders aren't matched with their
            // existing members until ScriptBuilder.PrepareToBuild.

            // So we create the member builders here, then finish configuring them in BuildScript.

            _BuildNumber = RootType.AddField("BuildNumber", typeof(int));
            _BuildNumber.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
            _BuildNumber.ValueEquals = null;

            _BuildDate = RootType.AddField("BuildDate", typeof(DateTime));
            _BuildDate.Modifiers = AccessModifiers.Public | AccessModifiers.Static | AccessModifiers.Readonly;
            _BuildDate.ValueEquals = null;
        }

        /// <summary>
        /// Finishes initialising the member builders that were gathered in <see cref="GatherScriptDetails"/> and
        /// Appends the declaration of the elements of this script in C# code to the specified `text`.
        /// </summary>
        protected override void AppendScript(StringBuilder text)
        {
            if (WeaverEditorUtilities.IsBuilding)
                ScriptGenerator.DisableSaveMessage();

            RootType.CommentBuilder = (comment) =>
            {
                comment.AppendLineConst("A utility class which tracks the number of times this project has been built" +
                    " and the date it was last built.")
                    .Append("This class is automatically regenerated each time the project is built.");
            };

            // Build Number.
            _BuildNumber.CommentBuilder = (comment) => comment.Append("The number of times this project has been built.");
            _BuildNumber.AppendInitializer = (initializer, indent, value) =>
            {
                var buildNumber = -1;

                if (_BuildNumber.ExistingField != null &&
                    _BuildNumber.ExistingField.FieldType == typeof(int) &&
                    _BuildNumber.ExistingField.IsStatic)
                {
                    buildNumber = (int)_BuildNumber.ExistingField.GetValue(null);
                }

                if (WeaverEditorUtilities.IsBuilding)
                    buildNumber++;

                initializer.Append(" = ").Append(buildNumber);
            };

            // Build Date.
            var buildDate = DateTime.Now;

            if (!WeaverEditorUtilities.IsBuilding &&
                _BuildDate.ExistingField != null &&
                _BuildDate.ExistingField.FieldType == typeof(DateTime) &&
                _BuildDate.ExistingField.IsStatic)
            {
                buildDate = (DateTime)_BuildDate.ExistingField.GetValue(null);
            }

            _BuildDate.CommentBuilder = (comment) => comment.Append(buildDate);
            _BuildDate.AppendInitializer = (initializer, indent, value) =>
            {
                initializer.Append(" = new System.DateTime(").Append(buildDate.Ticks).Append("L)");
            };

            base.AppendScript(text);
        }

        /************************************************************************************************************************/
    }
}

#endif

