// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to procedural assets.
    /// </summary>
    [Serializable]
    internal sealed class ProceduralAssetSettings : IOnCreate
    {
        /************************************************************************************************************************/

        public bool includeNamespaceInName = false;

        public bool checkForMissingAssets = true;

        public bool autoGenerateOnBuild = true;

        public bool notifyWhenDeletingOldAssets = true;

        /************************************************************************************************************************/
        // Procedural Scripts
        /************************************************************************************************************************/

        public bool scriptsUseWeaverNamespace = true;

        public bool autoGenerateScriptsOnSave = true;

        public bool notifyWhenGeneratingScripts = true;

        public bool scriptsKeepObsoleteMembers = true;

        /************************************************************************************************************************/

        [SerializeField]
        private DefaultAsset _OutputDirectory;

        [SerializeField]
        private List<ProceduralAssetData> _ProceduralAssetData;

        [SerializeField]
        private List<ProceduralAssetListData> _ProceduralAssetListData;

        /************************************************************************************************************************/

        public static ProceduralAssetSettings Instance => WeaverSettings.ProceduralAssets;

        /************************************************************************************************************************/

        void IOnCreate.OnCreate()
        {
            Instance._ProceduralAssetData = new List<ProceduralAssetData>();
            Instance._ProceduralAssetListData = new List<ProceduralAssetListData>();
        }

        /************************************************************************************************************************/

        public static ProceduralAssetData GetProceduralAssetData(ProceduralAsset asset)
        {
            var data = WeaverUtilities.GetReferenceTo(Instance._ProceduralAssetData, asset.Injector.SavedData.Index);

            if (data == null)
            {
                data = new ProceduralAssetData(asset.Injector.SavedData.Index);
                Instance._ProceduralAssetData.Add(data);
                WeaverSettings.SetDirty();
            }

            return data;
        }

        /************************************************************************************************************************/

        public static ProceduralAssetListData GetProceduralAssetListData(ProceduralAsset asset)
        {
            var data = WeaverUtilities.GetReferenceTo(Instance._ProceduralAssetListData, asset.Injector.SavedData.Index);

            if (data == null)
            {
                data = new ProceduralAssetListData(asset.Injector.SavedData.Index);
                Instance._ProceduralAssetListData.Add(data);
                WeaverSettings.SetDirty();
            }

            return data;
        }

        /************************************************************************************************************************/

        /// <summary>The directory to save new procedural assets in. Ends with a '/'.</summary>
        public static string OutputDirectory
        {
            get
            {
                // Custom.
                var asset = Instance._OutputDirectory;
                if (asset != null)
                {
                    var path = AssetDatabase.GetAssetPath(asset);
                    if (path != null && path.StartsWith("Assets/") && AssetDatabase.IsValidFolder(path))
                    {
                        if (path[path.Length - 1] != '/')
                            path += "/";

                        return path;
                    }

                    Instance._OutputDirectory = null;
                }

                // Default.
                return "Assets/Procedural Assets/";
            }
        }

        /************************************************************************************************************************/

        public static string EnsureOutputDirectoryExists()
        {
            var directory = OutputDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Created Weaver Procedural Asset Output Directory at '{directory}'." +
                    $" This directory can be moved anywhere you like" +
                    $" or you can set a different one via the Procedural Assets tab in '{WeaverUtilities.WeaverWindowPath}'.");

                AssetDatabase.ImportAsset(directory);

                EditorApplication.delayCall += () =>
                    Instance._OutputDirectory = AssetDatabase.LoadAssetAtPath<DefaultAsset>(directory.Substring(0, directory.Length - 1));
            }

            return directory;
        }

        /************************************************************************************************************************/

        public static Object CreateNewAssetInOutputDirectory(Type type, string name, out string assetPath)
        {
            assetPath = WeaverEditorUtilities.GetDefaultFileExtension(type);
            if (assetPath == null)
            {
                Debug.LogWarning("Unable to determine default file extension for " + type.GetNameCS());
                return null;
            }

            var directory = EnsureOutputDirectoryExists();
            assetPath = directory + name + "." + assetPath;

            return WeaverEditorUtilities.CreateNewAsset(type, assetPath);
        }

        /************************************************************************************************************************/

        public void DoGUI()
        {
            Instance._OutputDirectory = (DefaultAsset)EditorGUILayout.ObjectField(WeaverEditorUtilities.TempContent(
                "Output Directory",
                "The directory in which new procedural assets are initially saved." +
                " After being saved the first time, regenerating an asset will continue to" +
                " overwrite the existing one, even if you move it somewhere else in the project."),
                Instance._OutputDirectory, typeof(DefaultAsset), false);

            var outputDirectory = OutputDirectory;
            EditorGUILayout.HelpBox(outputDirectory, MessageType.None);

            if (outputDirectory.StartsWith("Assets/Plugins/"))
            {
                EditorGUILayout.HelpBox(
                    "Using a directory inside 'Assets/Plugins' as the Working Directory may prevent the Injector script from working",
                    MessageType.Warning);
            }

            WeaverEditorUtilities.DoToggle(ref includeNamespaceInName,
                "Include Namespace in Name",
                "If enabled: newly created procedural assets will be named according to their Namespace.DeclaringType.Member. Otherwise the Namespace will be omitted.");

            WeaverEditorUtilities.DoToggle(ref checkForMissingAssets,
                "Check For Missing Assets",
                "If enabled: Weaver will notify you when entering Play Mode or starting a build if there are any procedural assets that haven't yet been generated.");

            WeaverEditorUtilities.DoToggle(ref autoGenerateOnBuild,
                "Auto Generate On Build",
                "If enabled: all procedural assets will automatically be generated when starting a build.");

            WeaverEditorUtilities.DoToggle(ref notifyWhenDeletingOldAssets,
                "Notify When Deleting Old Assets",
                "If enabled: the system will notify you whenever regenerating a procedural asset list deletes an old asset.");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Procedural Scripts");

                WeaverEditorUtilities.DoToggle(ref scriptsUseWeaverNamespace,
                    "Use Weaver Namespace",
                    "If enabled: procedural scripts will be put inside the Weaver namespace by default");

                WeaverEditorUtilities.DoToggle(ref autoGenerateScriptsOnSave,
                    "Auto Generate On Save",
                    "If enabled: saving the scene or project will automatically regenerate any procedural scripts that need to change.");

                WeaverEditorUtilities.DoToggle(ref notifyWhenGeneratingScripts,
                    "Notify When Generating",
                    "If enabled: a message will be logged whenever a procedural script is generated.");

                WeaverEditorUtilities.DoToggle(ref scriptsKeepObsoleteMembers,
                    "Keep Obsolete Members",
                    "If enabled: procedural scripts will keep their old members when regenerating" +
                    " and mark them as [Obsolete] until you build instead of simply removing them.");
            }
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        static ProceduralAssetSettings()
        {
            AssetInjectionData.OnDataRemoved += (index) =>
            {
                WeaverUtilities.OnReferenceRemoved(Instance._ProceduralAssetData, index);
                WeaverUtilities.OnReferenceRemoved(Instance._ProceduralAssetListData, index);
            };
        }

        /************************************************************************************************************************/
    }
}

#endif

