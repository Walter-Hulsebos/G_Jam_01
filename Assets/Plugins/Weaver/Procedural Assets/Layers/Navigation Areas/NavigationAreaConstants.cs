// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;
using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the navigation areas in your project.
    /// </summary>
    internal sealed class NavigationAreaConstants
    {
        /************************************************************************************************************************/

        [AssetReference(FileName = "NavAreas", DisableAutoFind = true, Optional = true,
            Tooltip = "This script can be customised via the Navigation Area Constants panel in the Weaver Window")]
        [Window.ShowInPanel(typeof(Window.NavAreasPanel), ShowInMain = true)]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldShow = nameof(ShouldShowScript),
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        private static readonly NavigationAreasScriptBuilder
            ScriptBuilder = new NavigationAreasScriptBuilder(GenerateScript);

        /************************************************************************************************************************/

        private static bool ShouldShowScript => ScriptBuilder.Enabled;

        private static bool ShouldGenerateScript => ScriptBuilder.ShouldBuild();

        [ScriptGenerator.Alias(nameof(Weaver))]
        private static void GenerateScript(StringBuilder text) => ScriptBuilder.BuildScript(text);

        /************************************************************************************************************************/
    }
}

#endif

