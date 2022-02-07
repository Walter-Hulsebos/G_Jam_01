// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Exposes an event that is called whenever an asset is imported, deleted, or moved.
    /// </summary>
    public sealed class AssetPostprocessor : UnityEditor.AssetPostprocessor
    {
        /************************************************************************************************************************/

        /// <summary>A delegate corresponding to <see cref="OnPostprocessAll"/>.</summary>
        public delegate void PostprocessorMethod(
            string[] imported, string[] deleted, string[] movedTo, string[] movedFrom);

        /// <summary>An event triggered by <see cref="OnPostprocessAll"/>.</summary>
        public static event PostprocessorMethod OnPostprocessAssets;

        /************************************************************************************************************************/

        private static void OnPostprocessAll(
            string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
            => OnPostprocessAssets?.Invoke(imported, deleted, movedTo, movedFrom);

        /************************************************************************************************************************/
    }
}

#endif

