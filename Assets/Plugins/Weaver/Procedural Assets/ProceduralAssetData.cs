// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Details about a <see cref="ProceduralAsset"/> and the asset is is linked to.
    /// </summary>
    [Serializable]
    internal sealed class ProceduralAssetData : IReferenceIndex
    {
        /************************************************************************************************************************/

        [SerializeField]
        private int _ReferencedAssetInjectionDataIndex;
        public int ReferencedIndex
        {
            get => _ReferencedAssetInjectionDataIndex;
            set => _ReferencedAssetInjectionDataIndex = value;
        }

        [SerializeField]
        public bool hasSubAssets;

        [SerializeField]
        private int[] _DependancyIndices;

        [NonSerialized]
        private List<ProceduralAsset> _Dependancies;

        [NonSerialized]
        private bool _HasInitializedDependancies;

        /************************************************************************************************************************/

        public List<ProceduralAsset> Dependancies
        {
            get
            {
                InitializeDependancies();
                return _Dependancies;
            }
        }

        /************************************************************************************************************************/

        public ProceduralAssetData(int referencedAssetInjectionDataIndex)
        {
            _ReferencedAssetInjectionDataIndex = referencedAssetInjectionDataIndex;
        }

        /************************************************************************************************************************/

        private void InitializeDependancies()
        {
            if (_HasInitializedDependancies)
                return;

            _HasInitializedDependancies = true;

            if (_DependancyIndices == null ||
                _DependancyIndices.Length == 0)
                return;

            var assetInjectionData = InjectionSettings.AssetInjectionData;

            _Dependancies = new List<ProceduralAsset>(_DependancyIndices.Length);
            for (int i = 0; i < _DependancyIndices.Length; i++)
            {
                var index = _DependancyIndices[i];
                if (index >= 0)
                {
                    var attribute = assetInjectionData[index].Attribute;
                    if (attribute == null)
                        continue;

                    var proceduralAsset = attribute.ProceduralAsset;
                    if (proceduralAsset != null)
                        _Dependancies.Add(proceduralAsset);
                }
            }
        }

        /************************************************************************************************************************/

        public bool IsDependantOn(ProceduralAsset asset)
        {
            if (Dependancies == null)
                return false;

            for (int i = 0; i < _Dependancies.Count; i++)
            {
                if (asset == _Dependancies[i])
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        public bool HasDependancy(ProceduralAsset asset)
        {
            return Dependancies != null && _Dependancies.Contains(asset);
        }

        /************************************************************************************************************************/

        public void AddDependancy(ProceduralAsset asset)
        {
            if (Dependancies == null)
                _Dependancies = new List<ProceduralAsset>();

            _Dependancies.Add(asset);
            WeaverSettings.SetDirty();
        }

        /************************************************************************************************************************/

        public void RemoveDependancy(int index)
        {
            if (Dependancies == null)
                return;

            if (_Dependancies.Count > 1)
                _Dependancies.RemoveAt(index);
            else
                _Dependancies = null;

            WeaverSettings.SetDirty();
        }

        /************************************************************************************************************************/

        public void SerializeDependancies()
        {
            if (_Dependancies == null)
                return;

            if (Dependancies != null)
            {
                WeaverUtilities.SetSize(ref _DependancyIndices, _Dependancies.Count);

                for (int i = 0; i < _DependancyIndices.Length; i++)
                    _DependancyIndices[i] = _Dependancies[i].Injector.SavedData.Index;

                WeaverSettings.SetDirty();
            }
            else
            {
                _DependancyIndices = null;
            }
        }

        /************************************************************************************************************************/

        public override string ToString()
        {
            return string.Concat(base.ToString(), ": hasSubAsset=", hasSubAssets);
        }

        /************************************************************************************************************************/
    }
}

#endif

