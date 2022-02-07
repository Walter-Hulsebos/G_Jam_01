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
    /// An <see cref="AssetList"/> that serializes direct references to the target assets.
    /// </summary>
    public class AssetList<T> : AssetList, IAssetList<T>
        where T : Object
    {
        /************************************************************************************************************************/

        /// <summary>The assets in this list.</summary>
        [SerializeField]
        private List<T> _Assets;

        /************************************************************************************************************************/

        /// <summary>The number of assets in this list.</summary>
        public override int Count
        {
            get
            {
#if UNITY_EDITOR
                GatherAssetsIfDirty();
#endif
                return _Assets.Count;
            }
        }

        /// <inheritdoc/>
        public override void Clear() => _Assets.Clear();

        /// <inheritdoc/>
        public override Type AssetType => typeof(T);

        /// <summary>False. This list will load all its assets immediately on startup.</summary>
        public override bool IsLazy => false;

        /************************************************************************************************************************/

        /// <summary>Gets the asset at the specified index in this list.</summary>
        public T this[int index] => _Assets[index];

        /************************************************************************************************************************/

        /// <summary>Returns an enumerator that iterates through all assets in this list.</summary>
        public IEnumerator<T> GetEnumerator() { return _Assets.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _Assets.GetEnumerator(); }

        /************************************************************************************************************************/

        /// <summary>Returns a description of the contents of this list.</summary>
        public override string ToString()
        {
            if (this == null)
                return "Null";

            var text = WeaverUtilities.GetStringBuilder();

            text.Append(name)
                .Append(": ")
                .Append(GetType().FullName)
                .Append(" [")
                .Append(Count)
                .Append("]");

            for (int i = 0; i < Count; i++)
            {
                var asset = _Assets[i];

                text.AppendLine()
                    .Append('[')
                    .Append(i)
                    .Append("] ")
                    .Append(asset != null ? asset.GetType().FullName : "Null");
            }

            return WeaverUtilities.ReleaseToString(text);
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Gathers all assets in the target <see cref="AssetList.Directory"/> and stores them in the serialized
        /// <see cref="_Assets"/> list.
        /// </summary>
        protected override void GatherAssets()
        {
            GatherAssets(ref _Assets);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws GUI controls for the assets in this list.</summary>
        public override void DoDetailsGUI()
        {
            GUI.enabled = false;

            var count = Count;
            EditorGUILayout.LabelField("Assets", count.ToString());

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.ObjectField(this[i], typeof(T), false);
            }
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

