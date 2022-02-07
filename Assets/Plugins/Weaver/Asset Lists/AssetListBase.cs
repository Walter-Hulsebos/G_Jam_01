// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using Weaver.Editor;
using Weaver.Editor.Window;
#endif

namespace Weaver
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> containing a list which is automatically populated with all assets of a given
    /// type in a specific folder. Gathering takes place in the Unity Editor so the list can be loaded efficiently at
    /// runtime and the target assets do not need to be in a Resources folder.
    /// </summary>
    /// <remarks>
    /// Unfortunately, Editor-Only fields (such as the target <see cref="Directory"/> and <see cref="Recursive"/> flag)
    /// can't be declared in a pre-compiled assembly or generic type. This complicates the structure of Weaver's source
    /// code since <see cref="AssetListBase"/> needs to be inside Weaver.dll to integrate with other systems while the
    /// other classes (<c>AssetList</c>, <c>AssetList&lt;T&gt;</c>, <c>LazyAssetList&lt;T&gt;</c>, and
    /// <c>MetaAssetList&lt;TAsset, TMeta&gt;</c>) need to be out in the Unity project for Unity to compile them itself.
    /// </remarks>
    public abstract class AssetListBase : ScriptableObject
#if UNITY_EDITOR
        , WeaverWindow.IItem
#endif
    {
        /************************************************************************************************************************/

        /// <summary>The number of assets in this list.</summary>
        public abstract int Count { get; }

        /// <summary>Clears all the assets from this list.</summary>
        public abstract void Clear();

        /// <summary>The type of assets in this list.</summary>
        public abstract Type AssetType { get; }

        /// <summary>If true: this list will only load its assets as they are needed rather than on startup.</summary>
        public abstract bool IsLazy { get; }

        /// <summary>The type of meta data in this list.</summary>
        public virtual Type MetaDataType => null;

        /************************************************************************************************************************/

        /// <summary>Returns a random index in this list.</summary>
        public int GetRandomIndex() => UnityEngine.Random.Range(0, Count);

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// The directory from which this list will gather assets.
        /// </summary>
        public abstract DefaultAsset Directory { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns the path of the target <see cref="Directory"/> relative to the project root or null if a valid
        /// folder isn't assigned.
        /// </summary>
        public string GetAndVerifyDirectoryPath()
        {
            if (Directory != null)
            {
                var directoryPath = AssetDatabase.GetAssetPath(Directory);
                if (directoryPath != null &&
                    AssetDatabase.IsValidFolder(directoryPath))
                    return directoryPath;
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// If true: this list will gather assets in any sub-directories as well as the target directory.
        /// </summary>
        public abstract bool Recursive { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Assigns the currently selected object as the <see cref="Directory"/> if possible.
        /// </summary>
        protected virtual void Reset()
        {
            if (Selection.activeObject is DefaultAsset directory)
                Directory = directory;
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private bool _HasGatheredAssets;

        /// <summary>[Editor-Only]
        /// Indicates that this list has been modified and should re-gather its assets next time it is accessed or
        /// serialized.
        /// </summary>
        protected internal new void SetDirty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            _HasGatheredAssets = false;
            _Tooltip = null;
            AssetListOverlay.ClearCache();

            if (this != null)
                EditorUtility.SetDirty(this);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Called by Unity when this list is unloaded.
        /// Ensures that all assets in the target <see cref="Directory"/> are gathered so they can be serialized.
        /// </summary>
        protected virtual void OnDisable()
        {
            GatherAssetsIfDirty();
        }

        /************************************************************************************************************************/
        #region Asset Gathering
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers the assets in the target <see cref="Directory"/> if they might have changed since this method was
        /// last called.
        /// </summary>
        public void GatherAssetsIfDirty()
        {
            if (_HasGatheredAssets)
                return;

            _HasGatheredAssets = true;

            GatherAssets();
        }

        /// <summary>[Editor-Only]
        /// Override to gather the assets in the target <see cref="Directory"/>.
        /// </summary>
        protected abstract void GatherAssets();

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers all assets in the target <see cref="Directory"/> into the `assets` list.
        /// </summary>
        protected void GatherAssets<T>(ref List<T> assets) where T : Object
        {
            if (assets == null)
                assets = new List<T>();
            else
                assets.Clear();

            var directoryPath = GetAndVerifyDirectoryPath();
            if (directoryPath == null)
                return;

            GatherAssets(assets, directoryPath, Recursive);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers all assets in the target <see cref="Directory"/> into the `assets` list.
        /// </summary>
        protected static void GatherAssets<T>(List<T> assets, string directoryPath, bool recursive) where T : Object
        {

            var files = System.IO.Directory.GetFiles(directoryPath);
            for (int i = 0; i < files.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(files[i]);
                if (asset != null)
                    assets.Add(asset);
            }

            if (recursive)
            {
                files = System.IO.Directory.GetDirectories(directoryPath);
                for (int i = 0; i < files.Length; i++)
                {
                    GatherAssets(assets, files[i], recursive);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers all assets and their resource paths in the target <see cref="Directory"/>.
        /// </summary>
        protected void GatherResources<T>(ref List<string> paths, List<T> assets) where T : Object
        {
            if (paths == null)
                paths = new List<string>();
            else
                paths.Clear();

            assets.Clear();

            //Utils.LogTemp("GatherResourcePaths " + name);

            var directoryPath = GetAndVerifyDirectoryPath();
            if (directoryPath == null)
                return;

            if (!WeaverEditorUtilities.IsResource(directoryPath, out var resourcePathStart))
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode ||
                    WeaverEditorUtilities.IsBuilding)
                    Debug.LogWarning("Asset List target directory isn't inside a Resources folder: " + directoryPath + ".", Directory);

                return;
            }

            GatherAssets(assets, directoryPath, Recursive);

            for (int i = 0; i < assets.Count; i++)
            {
                var path = AssetDatabase.GetAssetPath(assets[i]);
                path = WeaverEditorUtilities.AssetToResourcePath(path, resourcePathStart);
                paths.Add(path);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Auto Dirty
        /************************************************************************************************************************/

        static AssetListBase()
        {
            Editor.AssetPostprocessor.OnPostprocessAssets += (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths) =>
            {
                RemoveMissingLists();

                for (int i = 0; i < AllLists.Count; i++)
                    AllLists[i].OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            };
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Calls <see cref="SetDirty"/> if any of the changed assets are (or should now be) included in this list.
        /// </summary>
        internal void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!_HasGatheredAssets)
                return;

            var directoryPath = GetAndVerifyDirectoryPath();
            if (directoryPath == null)
            {
                Clear();
                return;
            }

            if (ContainsAssets(directoryPath, importedAssets) ||
                ContainsAssets(directoryPath, deletedAssets) ||
                ContainsAssets(directoryPath, movedAssets) ||
                ContainsAssets(directoryPath, movedFromAssetPaths))
            {
                SetDirty();
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether any of the `assetPaths` starts with the `directoryPath`.
        /// </summary>
        private static bool ContainsAssets(string directoryPath, string[] assetPaths)
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (assetPaths[i].StartsWith(directoryPath))
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        private string _Tooltip;

        /// <summary>[Editor-Only]
        /// The text to use to describe this list when showing a tooltip in the <see cref="WeaverWindow"/>.
        /// </summary>
        public string Tooltip
        {
            get
            {
                if (_Tooltip == null)
                {
                    _Tooltip = WeaverUtilities.GetStringBuilder()
                        .Append("AssetList: ")
                        .Append(name)
                        .AppendLineConst()
                        .AppendLineConst()
                        .Append("Type: ")
                        .Append(GetType().GetNameCS())
                        .AppendLineConst()
                        .AppendLineConst()
                        .Append("Directory: ")
                        .Append(GetAndVerifyDirectoryPath())
                        .ReleaseToString();
                }

                return _Tooltip;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws the details of this list in the inspector.
        /// </summary>
        public abstract void DoDetailsGUI();

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// All asset lists are shown in the 'Asset Lists' panel in the Weaver Window.
        /// </summary>
        Type WeaverWindow.IItem.GetPanelType(out Type secondaryPanel)
        {
            secondaryPanel = null;
            return typeof(AssetListsPanel);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region All Lists
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Ensures that this list is in the global collection of all lists so it can be displayed in the Weaver Window
        /// and can show project window overlays on its target assets.
        /// </summary>
        protected virtual void OnEnable()
        {
            AddToGlobalList();
        }

        /// <summary>[Editor-Only]
        /// Adds the `list` to a global collection of all lists.
        /// </summary>
        internal void AddToGlobalList()
        {
            if (!_IsInGlobalList)
            {
                _IsInGlobalList = true;
                AllLists.Add(this);
            }
        }

        [NonSerialized]
        private bool _IsInGlobalList;

        /************************************************************************************************************************/

        internal static readonly List<AssetListBase> AllLists = new List<AssetListBase>();

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Removes anything from <see cref="AllLists"/> that has been destroyed or failed to load.
        /// </summary>
        internal static void RemoveMissingLists()
        {
            for (int i = AllLists.Count - 1; i >= 0; i--)
            {
                if (AllLists[i] == null)
                {
                    AllLists.RemoveAt(i);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region List Types
        /************************************************************************************************************************/

        internal static Type GenericAssetListType { get; private set; }
        internal static Type LazyAssetListType { get; private set; }
        internal static Type MetaAssetListType { get; private set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Assigns the list types that need to be defined outside this assembly so they c an be used internally.
        /// </summary>
        /// <remarks>
        /// This method should only be called by Weaver.AssetList.
        /// </remarks>
        [Obsolete("This method should only be called by Weaver.AssetList to specify the list types defined outside Weaver.dll")]
        public static void SetListTypes(Type genericAssetList, Type lazyAssetList, Type metaAssetList)
        {
            GenericAssetListType = genericAssetList;
            LazyAssetListType = lazyAssetList;
            MetaAssetListType = metaAssetList;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

