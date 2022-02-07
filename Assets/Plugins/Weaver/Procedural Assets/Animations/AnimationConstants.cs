// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;
using UnityEditor;
using UnityEditor.Animations;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Scans your project for all <see cref="AnimatorController"/> assets to gather the hashes of all states and
    /// parameters then procedurally generates a script containing constants wrapper methods for each of them.
    /// </summary>
    internal static class AnimationConstants
    {
        /************************************************************************************************************************/

        [AssetReference(FileName = "Animations", DisableAutoFind = true, Optional = true,
            Tooltip = "This script can be customised via the Animation Constants panel in the Weaver Window")]
        [Window.ShowInPanel(typeof(Window.AnimationsPanel), ShowInMain = true)]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldShow = nameof(ShouldShowScript),
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        private static readonly AnimationsScriptBuilder
            ScriptBuilder = new AnimationsScriptBuilder(GenerateScript);

        /************************************************************************************************************************/

        private static bool ShouldShowScript => ScriptBuilder.Enabled;

        private static bool ShouldGenerateScript => ScriptBuilder.ShouldBuild();

        [ScriptGenerator.Alias(nameof(Weaver))]
        private static void GenerateScript(StringBuilder text) => ScriptBuilder.BuildScript(text);

        /************************************************************************************************************************/
    }
}

#endif

