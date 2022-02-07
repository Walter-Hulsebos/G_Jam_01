// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="AssetList"/> that serializes resource paths so that assets don't need to be loaded until they are
    /// first accessed.
    /// </summary>
    public class LazyAssetList<T> : AssetList, IAssetList<T>
        where T : Object
    {
        /************************************************************************************************************************/

        /// <summary>The paths of all assets in this list.</summary>
        [SerializeField]
        private List<string> _Paths;

        /************************************************************************************************************************/

        /// <summary>The assets in this list corresponding to the <see cref="_Paths"/>.</summary>
        private readonly List<T> Assets = new List<T>();

        /************************************************************************************************************************/

        /// <summary>The number of assets in this list.</summary>
        public override int Count
        {
            get
            {
#if UNITY_EDITOR
                GatherAssetsIfDirty();
#endif

                return _Paths.Count;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Clear() => _Paths.Clear();

        /// <inheritdoc/>
        public override Type AssetType => typeof(T);

        /// <summary>True. This list will only load its assets as they are needed rather than on startup.</summary>
        public override bool IsLazy => true;

        /************************************************************************************************************************/

        /// <summary>Gets the path of the asset at the specified `index`.</summary>
        public string GetPath(int index)
        {
#if UNITY_EDITOR
            GatherAssetsIfDirty();
#endif
            return _Paths[index];
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the asset at the specified `index` has already been loaded.</summary>
        public bool GetIsLoaded(int index)
        {
#if UNITY_EDITOR
            GatherAssetsIfDirty();
#endif
            return Assets[index] != null;
        }

        /************************************************************************************************************************/
        #region Path Changes
        /************************************************************************************************************************/

#if ! UNITY_EDITOR
        /// <summary>
        /// [Runtime-Only] Called by Unity when this list is loaded.
        /// </summary>
        /// <remarks>
        /// Note that the base class contains an [Editor-Only] version of this method as well.
        /// </remarks>
        protected virtual void OnEnable()
        {
            OnCountChanged();
        }
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// Called when the number of assets in this list changes to ensure that all internal aspects are kept in sync.
        /// </summary>
        protected virtual void OnCountChanged()
        {
            // Could try to support removing asset paths selectively.

            for (int i = 0; i < Assets.Count; i++)
                Assets[i] = null;

            WeaverUtilities.SetCount(Assets, _Paths.Count);
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `path` to this list.</summary>
        public void AddPath(string path)
        {
            _Paths.Add(path);
            OnCountChanged();
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `paths` to this list.</summary>
        public void AddPaths(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                _Paths.Add(path);
            OnCountChanged();
        }

        /************************************************************************************************************************/

        /// <summary>Adds the paths in the specified `list` to this list.</summary>
        public void AddPaths(LazyAssetList<T> list)
        {
            var count = list.Count;

            var minCapacity = _Paths.Count + count;
            if (_Paths.Capacity < minCapacity)
                _Paths.Capacity = minCapacity;

            AddPaths(list._Paths);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Ensures that the asset at the specified index in this list is loaded and returns it.</summary>
        public T this[int index] => GetAsset(index);

        /// <summary>Ensures that the asset at the specified index in this list is loaded and returns it.</summary>
        public T GetAsset(int index)
        {
            var asset = Assets[index];
            if (asset == null)
            {
                asset = Resources.Load<T>(GetPath(index));
                Assets[index] = asset;
            }
            return asset;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the asset at the specified index in this list is not loaded, this method loads it asynchronously,
        /// returns the <see cref="ResourceRequest"/> used to do so, and passes it into `onAssetLoaded` when complete.
        /// Otherwise this method immediately passes the already loaded asset into `onAssetLoaded` and returns null.
        /// </summary>
        public ResourceRequest GetAssetAsync(int index, Action<T> onAssetLoaded)
        {
            var asset = Assets[index];
            if (asset != null)
            {
                onAssetLoaded?.Invoke(asset);
                return null;
            }
            else
            {
                var request = Resources.LoadAsync<T>(GetPath(index));
                request.completed += (operation) =>
                {
                    var newAsset = (T)((ResourceRequest)operation).asset;
                    Assets[index] = newAsset;
                    onAssetLoaded?.Invoke(newAsset);
                };
                return request;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that all assets in this list are loaded.</summary>
        public void LoadAll()
        {
            for (int i = 0; i < Count; i++)
            {
                GetAsset(i);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Resources.UnloadAsset"/> on every asset in this list.</summary>
        public void UnloadAll()
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                Resources.UnloadAsset(Assets[i]);
                Assets[i] = null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns an enumerator that iterates through all assets in this list.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            LoadAll();
            return Assets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /************************************************************************************************************************/

        /// <summary>Returns a description of the contents of this list.</summary>
        public override string ToString()
        {
            if (this == null)
                return "Null";

            var text = WeaverUtilities.GetStringBuilder()
                .Append(name)
                .Append(": ")
                .Append(GetType().FullName)
                .Append(" [")
                .Append(Count)
                .Append("]");

            for (int i = 0; i < Count; i++)
            {
                var asset = Assets[i];

                text.AppendLine()
                    .Append('[')
                    .Append(i)
                    .Append("] ")
                    .Append(GetPath(i))
                    .Append(" (")
                    .Append(asset != null ? asset.GetType().FullName : "Not Loaded")
                    .Append(')');
            }

            return WeaverUtilities.ReleaseToString(text);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a dictionary that maps the resource path of each asset in this list to its index.
        /// </summary>
        public Dictionary<string, int> MapPathToIndex()
        {
#if UNITY_EDITOR
            GatherAssetsIfDirty();
#endif

            var pathToIndex = new Dictionary<string, int>(_Paths.Count);

            for (int i = 0; i < _Paths.Count; i++)
            {
                pathToIndex.Add(_Paths[i], i);
            }

            return pathToIndex;
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers all assets in the target <see cref="AssetList.Directory"/> and stores their resource paths in the
        /// serialized <see cref="_Paths"/> list.
        /// </summary>
        protected override void GatherAssets()
        {
            GatherResources(ref _Paths, Assets);
            OnCountChanged();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the details of this list in the inspector.</summary>
        public override void DoDetailsGUI()
        {
            GUI.enabled = false;

            var directoryPath = GetAndVerifyDirectoryPath();
            if (directoryPath == null)
                return;

            if (!Editor.WeaverEditorUtilities.IsResource(directoryPath, out var _))
            {
                EditorGUILayout.HelpBox("Target directory is not inside a Resources folder."
                    + " This list will not be able to load any assets since it is a LazyAssetList.", MessageType.Error);
                return;
            }

            var count = Count;
            EditorGUILayout.LabelField("Assets", count.ToString());

            for (int i = 0; i < count; i++)
            {
                var label = $"[{i}] {GetPath(i)}";
                EditorGUILayout.ObjectField(label, this[i], typeof(T), false);
            }
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

