// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>
    /// A <see cref="LazyAssetList{T}"/> that serializes a specific type of meta-data about the listed assets so that
    /// the data can be accessed and evaluated without actually loading the assets.
    /// </summary>
    public abstract class MetaAssetList<TAsset, TMeta> : LazyAssetList<TAsset>
        where TAsset : Object
    {
        /************************************************************************************************************************/

        /// <summary>The meta data of all assets in this list.</summary>
        [SerializeField]
        private List<TMeta> _MetaData;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Type MetaDataType => typeof(TMeta);

        /************************************************************************************************************************/

        /// <summary>Returns the meta-data of the asset at the specified index.</summary>
        public TMeta GetMetaData(int index)
        {
#if ! UNITY_EDITOR
            return _MetaData[index];
#else
            var count = Count;
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException("index");

            if (_MetaData == null)
                _MetaData = new List<TMeta>();

            WeaverUtilities.SetCount(_MetaData, count);

            if (!_SearchedForMetaConstructor)
            {
                _SearchedForMetaConstructor = true;
                _MetaConstructor = Editor.MetaDataUtils.GetSingleParameterConstructor(typeof(TMeta), typeof(TAsset));
            }

            return _MetaData[index] = Editor.MetaDataUtils.GetMetaData<TAsset, TMeta>(this[index], _MetaConstructor);
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an array of indicies indicating the sorted order of items in this list based on the `comparer`.
        /// </summary>
        public int[] GetSortedIndices(IComparer<TMeta> comparer)
        {
            int[] indices = null;
            GetSortedIndices(ref indices, comparer);
            return indices;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Outputs an array of indicies indicating the sorted order of items in this list based on the `comparer`.
        /// </summary>
        public void GetSortedIndices(ref int[] indices, IComparer<TMeta> comparer)
        {
            if (indices == null || indices.Length != _MetaData.Count)
                indices = new int[_MetaData.Count];

            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;

#if UNITY_EDITOR
                GetMetaData(i);
#endif
            }

            Array.Sort(indices, (a, b) => comparer.Compare(_MetaData[a], _MetaData[b]));
        }

        /************************************************************************************************************************/
        #region Meta Gathering
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The constructor of <see cref="TMeta"/>.</summary>
        private ConstructorInfo _MetaConstructor;

        /// <summary>[Editor-Only] Indicates whether this list has looked for the <see cref="_MetaConstructor"/>.</summary>
        private bool _SearchedForMetaConstructor;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void GatherAssets()
        {
            base.GatherAssets();

            for (int i = 0; i < Count; i++)
            {
                GetMetaData(i);
            }
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}

