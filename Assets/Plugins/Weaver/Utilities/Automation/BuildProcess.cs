// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Automatically executes logic for asset injection, asset lists, and procedural assets during a build.
    /// </summary>
    internal sealed class BuildProcess : IPreprocessBuildWithReport
    {
        /************************************************************************************************************************/

        int IOrderedCallback.callbackOrder => 0;

        /************************************************************************************************************************/

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.buildScriptsOnly)
                return;

            try
            {
                WeaverEditorUtilities.IsPreprocessingBuild = true;

                WeaverSettings.Instance.ClearOldData();

                AssetListBase.RemoveMissingLists();
                foreach (var list in AssetListBase.AllLists)
                    list.GatherAssetsIfDirty();

                foreach (var injector in InjectorManager.AllInjectors)
                    injector.OnStartBuild();

                // Generate Procedural Assets.
                for (int i = 0; i < ProceduralAsset.AllProceduralAssets.Count; i++)
                {
                    var asset = ProceduralAsset.AllProceduralAssets[i];
                    var autoGenerateOnBuild = ProceduralAssetSettings.Instance.autoGenerateOnBuild;
                    if (asset.ProceduralAttribute.OptionalAutoGenerateOnBuild.ToBool(autoGenerateOnBuild))
                    {
                        AssetGeneratorWindow.AssetsToGenerate.Add(asset);
                    }
                }

                AssetGeneratorWindow.Generate(true);

                if (!ProceduralAssetSettings.Instance.autoGenerateOnBuild)
                    ProceduralAsset.CheckForMissingAssets();
            }
            finally
            {
                WeaverEditorUtilities.IsPreprocessingBuild = false;
            }

        }

        /************************************************************************************************************************/
    }
}

#endif

