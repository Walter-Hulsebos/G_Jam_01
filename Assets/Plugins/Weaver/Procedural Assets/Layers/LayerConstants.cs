// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;
using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the physics layers in your project.
    /// </summary>
    internal static class LayerConstants
    {
        /************************************************************************************************************************/

        [AssetReference(FileName = "Layers", DisableAutoFind = true, Optional = true,
            Tooltip = "This script can be customised via the Layer Constants panel in the Weaver Window")]
        [Window.ShowInPanel(typeof(Window.LayersPanel), ShowInMain = true)]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldShow = nameof(ShouldShowScript),
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        private static readonly LayersScriptBuilder
            ScriptBuilder = new LayersScriptBuilder(GenerateScript);

        /************************************************************************************************************************/

        private static bool ShouldShowScript => ScriptBuilder.Enabled;

        private static bool ShouldGenerateScript => ScriptBuilder.ShouldBuild();

        [ScriptGenerator.Alias(nameof(Weaver))]
        private static void GenerateScript(StringBuilder text) => ScriptBuilder.BuildScript(text);

        /************************************************************************************************************************/
    }
}

#endif

