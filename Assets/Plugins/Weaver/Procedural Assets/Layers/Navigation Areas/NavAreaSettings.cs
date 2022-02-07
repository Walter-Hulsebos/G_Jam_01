// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to the <see cref="NavigationAreasScriptBuilder"/>.
    /// </summary>
    [Serializable]
    internal sealed class NavAreaSettings : BaseLayerSettings
    {
        /************************************************************************************************************************/

        public List<CustomNavAreaMask> customMasks;

        protected override void OnCreate() => customMasks = new List<CustomNavAreaMask>();

        public override IList CustomMasks => customMasks;

        public override CustomLayerMask GetMask(int index) => customMasks[index];

        /************************************************************************************************************************/

        public override string EnableLayersLabel => "Include Areas";
        public override string EnableLayersTooltip => "If enabled, the script will include constants corresponding to the index of each area";

        public override string EnableLayerMasksLabel => "Include Area Masks";
        public override string EnableLayerMasksTooltip => "If enabled, the script will include constants corresponding to the bit mask of each area (1 << AreaValue)";

        public override LayerManager LayerManager => NavAreaManager.Instance;

        /************************************************************************************************************************/
    }
}

#endif

