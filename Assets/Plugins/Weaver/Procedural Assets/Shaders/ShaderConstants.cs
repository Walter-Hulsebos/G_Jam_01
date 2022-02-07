// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;
using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the properties and symbols in a set of
    /// shaders chosen in the <see cref="Window.ShadersPanel"/>.
    /// </summary>
    internal static class ShaderConstants
    {
        /************************************************************************************************************************/

        [AssetReference(FileName = "Shaders", DisableAutoFind = true, Optional = true,
            Tooltip = "This script can be customised via the Shader Constants panel in the Weaver Window")]
        [Window.ShowInPanel(typeof(Window.ShadersPanel), ShowInMain = true)]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldShow = nameof(ShouldShowScript),
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        public static readonly ShadersScriptBuilder
            ScriptBuilder = new ShadersScriptBuilder(GenerateScript);

        /************************************************************************************************************************/

        private static bool ShouldShowScript => ScriptBuilder.Enabled;

        private static bool ShouldGenerateScript => ScriptBuilder.ShouldBuild();

        [ScriptGenerator.Alias(nameof(Weaver))]
        private static void GenerateScript(StringBuilder text) => ScriptBuilder.BuildScript(text);

        /************************************************************************************************************************/
    }
}

#endif

