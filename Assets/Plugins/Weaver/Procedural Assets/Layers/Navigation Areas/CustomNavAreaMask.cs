// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// A custom bit mask corresponding to the project's navigation areas.
    /// </summary>
    [Serializable]
    internal sealed class CustomNavAreaMask : CustomLayerMask
    {
        /************************************************************************************************************************/

        public override LayerManager LayerManager => NavAreaManager.Instance;

        /************************************************************************************************************************/
    }
}

#endif

