// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Details about an <see cref="AssetInjectionAttribute"/> and the asset is is linked to.
    /// </summary>
    [Serializable]
    internal sealed class AssetInjectionData
    {
        /************************************************************************************************************************/

        public static event Action<int> OnDataRemoved;

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Type.FullName"/> and <see cref="AssemblyName.Name"/> of the type in which the attributed
        /// property is declared.
        /// </summary>
        [SerializeField]
        private string _DeclaringType;

        /// <summary>The name of the attributed member.</summary>
        [SerializeField]
        private string _Member;

        [SerializeField]
        private Object _Asset;

        /// <summary>The asset linked to the attribute.</summary>
        public Object Asset
        {
            get
            {
                if (_Asset == null && !string.IsNullOrEmpty(_GUID))
                {
                    var path = AssetDatabase.GUIDToAssetPath(_GUID);
                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    for (int i = 0; i < subAssets.Length; i++)
                    {
                        var subAsset = subAssets[i];
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subAsset, out _, out long id) &&
                            id == _LocalID)
                        {
                            _Asset = subAsset;
                            break;
                        }
                    }
                }

                return _Asset;
            }

            set
            {
                if (ReferenceEquals(_Asset, value))
                    return;

                _Asset = value;

                if (value != null)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _GUID, out _LocalID);
                }
                else
                {
                    _GUID = null;
                    _LocalID = 0;
                }

                WeaverSettings.SetDirty();
            }
        }

        /// <summary>The GUID of the <see cref="Asset"/>.</summary>
        [SerializeField]
        private string _GUID;

        /// <summary>The local ID of the <see cref="Asset"/> inside its file.</summary>
        [SerializeField]
        private long _LocalID;

        /************************************************************************************************************************/

        /// <summary>
        /// When an <see cref="AssetInjectionAttribute"/> initializes, it claimes its saved data so that other
        /// assets can skip over it and so old data from attributes which no longer exist can be removed.
        /// </summary>
        public AssetInjectionAttribute Attribute { get; private set; }

        /// <summary>
        /// The index of this data in the list containing it.
        /// </summary>
        public int Index { get; private set; }

        /************************************************************************************************************************/

        internal AssetInjectionData(AssetInjectionAttribute attribute, int index)
        {
            _DeclaringType = ReflectionUtilities.GetQualifiedName(attribute.Member.DeclaringType);
            _Member = attribute.Member.Name;
            Attribute = attribute;
            Index = index;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if this <see cref="AssetInjectionData"/> hasn't yet been claimed and references the `member`.
        /// </summary>
        public bool IsTarget(MemberInfo member)
        {
            return
                Attribute == null &&
                _DeclaringType == ReflectionUtilities.GetQualifiedName(member.DeclaringType) &&
                _Member == member.Name;
        }

        /************************************************************************************************************************/

        public override string ToString()
        {
            return $"{GetType().GetNameCS()}: DeclaringType={_DeclaringType}, Member={_Member}," +
                $" {nameof(Asset)}={(_Asset != null ? _Asset.ToString() : "null")}," +
                $" GUID='{_GUID}'";
        }

        /************************************************************************************************************************/

        public bool TryFindTargetAsset()
        {
            var foundNewTarget = false;

            var pathMatcher = AssetPathMatcher.Get(Attribute);
            var assetPaths = WeaverEditorUtilities.FindAllAssetPaths(Attribute.AssetType);
            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (pathMatcher.TryMatchFile(assetPaths[i]))
                    foundNewTarget = true;
            }

            if (Asset != null)
            {
                if (foundNewTarget && WeaverSettings.Injection.logAssetFound && WeaverSettings.Instance.IsSaved)
                {
                    var context = Asset;
                    if (context is Component component)
                        context = component.gameObject;

                    Debug.Log($"Found Target Asset for {Attribute.Member.GetNameCS()} at {AssetDatabase.GetAssetPath(context)}", context);
                }

                return true;
            }

            return false;
        }

        /************************************************************************************************************************/
        #region Static Access
        /************************************************************************************************************************/

        static AssetInjectionData()
        {
            WeaverSettings.OnAfterDeserialize += () =>
            {
                var dataList = InjectionSettings.AssetInjectionData;
                if (dataList == null)
                    return;

                // Initialize the indices of each item in the list.
                for (int i = 0; i < dataList.Count; i++)
                {
                    dataList[i].Index = i;
                }
            };

            WeaverSettings.OnClearOldData += ClearUnclaimedData;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the <see cref="AssetInjectionData"/> associated with the specified `attribute` or creates a new one.
        /// </summary>
        public static AssetInjectionData GetOrCreateData(ref List<AssetInjectionData> dataList, AssetInjectionAttribute attribute, out bool isNew)
        {
            var data = GetData(dataList, attribute);
            if (data != null)
            {
                isNew = false;
                return data;
            }
            else
            {
                isNew = true;
                return NewData(ref dataList, attribute);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the <see cref="AssetInjectionData"/> associated with the specified `attribute` or creates a new one.
        /// </summary>
        public static AssetInjectionData GetData(List<AssetInjectionData> dataList, AssetInjectionAttribute attribute)
        {
            if (dataList != null)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    var data = dataList[i];
                    if (data.IsTarget(attribute.Member))
                    {
                        if (data.Asset != null && !attribute.AssetType.IsAssignableFrom(data.Asset.GetType()))
                            data.Asset = null;

                        data.Attribute = attribute;
                        return data;
                    }
                }
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Stores a reference to the property (by name) and the asset so it can be found next time without searching
        /// the whole project. If there is no settings file, this method does nothing.
        /// </summary>
        private static AssetInjectionData NewData(ref List<AssetInjectionData> dataList, AssetInjectionAttribute attribute)
        {
            if (dataList == null)
                dataList = new List<AssetInjectionData>();

            var data = new AssetInjectionData(attribute, dataList.Count);
            dataList.Add(data);

            WeaverSettings.SetDirty();

            return data;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes any data for attributes which no longer exist and returns true if the list was changed.
        /// </summary>
        public static void ClearUnclaimedData(ref bool isDirty)
        {
            var dataList = InjectionSettings.AssetInjectionData;
            if (dataList == null)
                return;

            for (int i = dataList.Count - 1; i >= 0; i--)
            {
                if (dataList[i].Attribute == null)
                {
#if WEAVER_DEBUG
                    Debug.Log("Removing Unclaimed " + dataList[i]);
#endif

                    dataList.RemoveAt(i);
                    isDirty = true;

                    // Adjust the indices of any later data.
                    for (int j = i; j < dataList.Count; j++)
                    {
                        dataList[j].Index--;
                    }

                    OnDataRemoved?.Invoke(i);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

