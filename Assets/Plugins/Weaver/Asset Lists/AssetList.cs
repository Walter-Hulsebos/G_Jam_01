// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Weaver
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> containing a list automatically populated with all assets of a given type in a
    /// specific folder. Gathering takes place in the Unity Editor so the list can be loaded efficiently at runtime.
    /// </summary>
    public abstract class AssetList : AssetListBase
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        [SerializeField]
        private DefaultAsset _Directory;

        /// <summary>[Editor-Only] The directory from which this list will gather assets.</summary>
        public override DefaultAsset Directory
        {
            get => _Directory;
            set
            {
                _Directory = value;
                SetDirty();
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private bool _Recursive;

        /// <summary>[Editor-Only]
        /// If true: this list will gather assets in any sub-directories as well as the target directory.
        /// </summary>
        public override bool Recursive
        {
            get => _Recursive;
            set
            {
                _Recursive = value;
                SetDirty();
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Specifies the asset list types for Weaver.dll to use since these types aren't declared in that assembly.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void SetListTypes()
        {
#pragma warning disable CS0618 // Type or member is obsolete.
            SetListTypes(typeof(AssetList<>), typeof(LazyAssetList<>), typeof(MetaAssetList<,>));
#pragma warning restore CS0618 // Type or member is obsolete.
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

