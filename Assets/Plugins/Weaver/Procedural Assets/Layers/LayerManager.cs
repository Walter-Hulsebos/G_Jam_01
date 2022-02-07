// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditorInternal;
using UnityEngine;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// The central point for the <see cref="LayersScriptBuilder"/> to access the details it needs.
    /// </summary>
    internal class LayerManager
    {
        /************************************************************************************************************************/

        public static readonly LayerManager Instance = new LayerManager();

        /************************************************************************************************************************/

        public int[] LayerValues { get; private set; }

        public event Action<string[], string[]> OnLayersChanged;

        /************************************************************************************************************************/

        public virtual BaseLayerSettings Settings => WeaverSettings.Layers;

        public virtual LayersScriptBuilder ScriptBuilder => LayersScriptBuilder.Instance;

        public virtual int DefaultLayerCount => 5;

        public virtual string[] NewLayerNames => InternalEditorUtility.layers;

        public virtual int NameToValue(string name) => LayerMask.NameToLayer(name);

        /************************************************************************************************************************/

        public string[] OldLayerNames => Settings.layerNames;

        /************************************************************************************************************************/

        public bool UpdateLayerNames()
        {
            var oldLayers = OldLayerNames;
            var newLayers = NewLayerNames;

            if (LayerValues == null ||
                oldLayers == null ||
                oldLayers.Length != newLayers.Length)
                goto OnLayersChanged;

            for (int i = DefaultLayerCount; i < newLayers.Length; i++)
            {
                if (oldLayers[i] != newLayers[i])
                    goto OnLayersChanged;
            }

            return false;

            OnLayersChanged:

            Settings.layerNames = newLayers;

            GatherLayerValues();

            if (oldLayers != null && Settings.CustomMasks != null)
            {
                var count = Settings.CustomMaskCount;
                for (int i = 0; i < count; i++)
                    Settings.GetMask(i).OnLayersChanged(oldLayers, newLayers);
            }

            OnLayersChanged?.Invoke(oldLayers, newLayers);

            return true;
        }

        /************************************************************************************************************************/

        public void GatherLayerValues()
        {
            var oldNames = OldLayerNames;

            LayerValues = WeaverUtilities.SetSize(LayerValues, oldNames.Length);

            for (int i = 0; i < LayerValues.Length; i++)
            {
                LayerValues[i] = NameToValue(oldNames[i]);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

