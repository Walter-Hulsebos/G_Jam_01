// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Automatically generates all procedural assets with <see cref="ProceduralAssetAttribute.AutoGenerateOnSave"/>
    /// when assets are saved (such as when the user presses Ctrl + S to save the scene).
    /// </summary>
    internal sealed class AutoGenerateOnSave : UnityEditor.AssetModificationProcessor
    {
        /************************************************************************************************************************/

        private AutoGenerateOnSave() { }

        /************************************************************************************************************************/

        private static bool _IsSavingAssets;

        private static void OnWillSaveAssets(string[] paths)
        {
            if (!ProceduralAssetSettings.Instance.autoGenerateScriptsOnSave)
                return;

            // Auto-Generate on Build is handled by the BuildProcess class.
            if (WeaverEditorUtilities.IsPreprocessingBuild)
                return;

            // Don't auto generate while Unity is compiling.
            if (EditorApplication.isCompiling)
                return;

            // Don't call this method recursively.
            if (_IsSavingAssets)
                return;

            _IsSavingAssets = true;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        if (!_WillGenerateAfterPlayMode)
                        {
                            _WillGenerateAfterPlayMode = true;
                            EditorApplication.playModeStateChanged += GenerateAfterPlayMode;
                        }
                    }
                    else
                    {
                        if (EditorGUIUtility.editingTextField)
                            return;

                        AutoGenerateAll();
                    }
                }
                finally
                {
                    _IsSavingAssets = false;
                }
            };
        }

        /************************************************************************************************************************/

        private static bool _WillGenerateAfterPlayMode;

        private static void GenerateAfterPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= GenerateAfterPlayMode;
                _WillGenerateAfterPlayMode = false;
                AutoGenerateAll();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Generates all procedural assets which use <see cref="SimpleScriptBuilder"/>s if their
        /// <see cref="ProceduralAssetAttribute.AutoGenerateOnSave"/> property is true.
        /// </summary>
        public static void AutoGenerateAll(bool retainObsoleteMembers = true)
        {
            for (int i = 0; i < ProceduralAsset.AllProceduralAssets.Count; i++)
            {
                var asset = ProceduralAsset.AllProceduralAssets[i];
                if (!asset.ProceduralAttribute.AutoGenerateOnSave)
                    continue;

                AssetGeneratorWindow.AssetsToGenerate.Add(asset);
            }

            AssetGeneratorWindow.Generate(true);
        }

        /************************************************************************************************************************/
    }
}

#endif

