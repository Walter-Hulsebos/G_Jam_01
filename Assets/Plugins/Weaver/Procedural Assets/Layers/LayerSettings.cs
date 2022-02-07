// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Base class for settings relating to a procedural script based on a set of layers.
    /// </summary>
    [Serializable]
    internal abstract class BaseLayerSettings : ProceduralScriptSettings, IOnCreate
    {
        /************************************************************************************************************************/

        public string[]
            layerNames;

        public bool
            includeLayers = true,
            includeLayerMasks = false;

        /************************************************************************************************************************/

        public virtual string EnableLayersLabel => "Include Layers";
        public virtual string EnableLayersTooltip => "If enabled, the script will include constants corresponding to the index of each layer";

        public virtual string EnableLayerMasksLabel => "Include Layer Masks";
        public virtual string EnableLayerMasksTooltip => "If enabled, the script will include constants corresponding to the bit mask of each layer (1 << LayerValue)";

        public abstract LayerManager LayerManager { get; }

        /************************************************************************************************************************/

        void IOnCreate.OnCreate() => OnCreate();

        protected virtual void OnCreate() { }

        public abstract IList CustomMasks { get; }

        public int CustomMaskCount => CustomMasks.Count;

        public abstract CustomLayerMask GetMask(int index);

        /************************************************************************************************************************/

        public void Initialize()
        {
            if (!LayerManager.UpdateLayerNames())
            {
                LayerManager.GatherLayerValues();

                var count = CustomMaskCount;
                for (int i = 0; i < count; i++)
                    GetMask(i).CalculateValue();
            }
        }

        /************************************************************************************************************************/

        public int CountValidMasks()
        {
            var validCount = 0;
            var count = CustomMaskCount;
            for (int i = 0; i < count; i++)
            {
                if (GetMask(i).ValidateName())
                    validCount++;
            }
            return validCount;
        }

        /************************************************************************************************************************/

        public override void DoGUI()
        {
            WeaverEditorUtilities.DoToggle(ref includeLayers, EnableLayersLabel, EnableLayersTooltip);
            WeaverEditorUtilities.DoToggle(ref includeLayerMasks, EnableLayerMasksLabel, EnableLayerMasksTooltip);
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/

    /// <summary>[Editor-Only, Internal]
    /// Settings relating to the <see cref="Procedural.LayersScriptBuilder"/>.
    /// </summary>
    [Serializable]
    internal sealed class LayerSettings : BaseLayerSettings
    {
        /************************************************************************************************************************/

        public bool
            includeCollisionMatrix2D = false,
            includeCollisionMatrix3D = false;

        /************************************************************************************************************************/

        public override LayerManager LayerManager => LayerManager.Instance;

        /************************************************************************************************************************/

        public List<CustomLayerMask> customMasks;

        protected override void OnCreate() => customMasks = new List<CustomLayerMask>();

        public override IList CustomMasks => customMasks;

        public override CustomLayerMask GetMask(int index) => customMasks[index];

        /************************************************************************************************************************/

        public override void DoGUI()
        {
            base.DoGUI();

            WeaverEditorUtilities.DoToggle(ref includeCollisionMatrix2D,
                "Include Collision Matrix 2D",
                "If enabled, the script will include constants corresponding to the 2D collision matrix for each layer");

            WeaverEditorUtilities.DoToggle(ref includeCollisionMatrix3D,
                "Include Collision Matrix 3D",
                "If enabled, the script will include constants corresponding to the 3D collision matrix for each layer");
        }

        /************************************************************************************************************************/
    }
}

#endif

