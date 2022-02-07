// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal] References to various textures that are used as icons.</summary>
    internal sealed class Icons : ScriptableObject
    {
        /************************************************************************************************************************/

        /// <summary>Singleton instance assigned using Asset Injection.</summary>
        [AssetReference(EditorOnly = true, Optional = true, Tooltip =
            "These icons are used to indicate that an asset in the Project Window is referenced by one of the Weaver systems")]
        [Window.ShowInPanel(typeof(Window.MiscPanel))]
        private static Icons Instance { get; set; }

        /************************************************************************************************************************/
#pragma warning disable UNT0008 // Null propagation on Unity objects (not a problem in this case).
        /************************************************************************************************************************/

        [SerializeField]
        private Texture2D _AssetList = null;
        public static Texture2D AssetList => Instance?._AssetList;

        [SerializeField]
        private Texture2D _AssetInstance = null;
        public static Texture2D AssetInstance => Instance?._AssetInstance;

        [SerializeField]
        private Texture2D _AssetPool = null;
        public static Texture2D AssetPool => Instance?._AssetPool;

        [SerializeField]
        private Texture2D _AssetReference = null;
        public static Texture2D AssetReference => Instance?._AssetReference;

        [SerializeField]
        private Texture2D _LazyReference = null;
        public static Texture2D LazyReference => Instance?._LazyReference;

        [SerializeField]
        private Texture2D _Pref = null;
        public static Texture2D Pref => Instance?._Pref;

        [SerializeField]
        private Texture2D _PotentialTarget = null;
        public static Texture2D PotentialTarget => Instance?._PotentialTarget;

        /************************************************************************************************************************/
#pragma warning restore UNT0008 // Null propagation on Unity objects (not a problem in this case).
        /************************************************************************************************************************/
    }
}

#endif
