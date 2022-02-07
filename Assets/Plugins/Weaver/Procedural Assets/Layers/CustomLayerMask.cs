// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Text;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// A custom bit mask corresponding to the project's physics layers.
    /// </summary>
    [Serializable]
    internal class CustomLayerMask
    {
        /************************************************************************************************************************/

        public string name = "";

        /// <summary>
        /// Bit mask corresponding to <see cref="UnityEditorInternal.InternalEditorUtility.layers"/> (not the actual
        /// value of this <see cref="CustomLayerMask"/>).
        /// </summary>
        public int layers;

        public string comment = "";

        /// <summary>The actual bit mask value of this <see cref="CustomLayerMask"/>.</summary>
        public int Value { get; private set; }

        /************************************************************************************************************************/

        public virtual LayerManager LayerManager => LayerManager.Instance;

        /************************************************************************************************************************/

        public bool IsUnusedDummy
        {
            get
            {
                return
                    string.IsNullOrEmpty(name) &&
                    string.IsNullOrEmpty(comment) &&
                    layers == 0;
            }
        }

        /************************************************************************************************************************/

        public bool HasBit(int bit)
        {
            return HasBit(layers, bit);
        }

        public static bool HasBit(int mask, int bit)
        {
            return (mask & (1 << bit)) != 0;
        }

        /************************************************************************************************************************/

        public void AddBit(int bit)
        {
            AddBit(ref layers, bit);
        }

        public void AddBit(ref int value, int bit)
        {
            value |= 1 << bit;
            Value |= 1 << LayerManager.LayerValues[bit];
        }

        /************************************************************************************************************************/

        public void RemoveBit(int bit)
        {
            RemoveBit(ref layers, bit);
        }

        public void RemoveBit(ref int value, int bit)
        {
            value &= ~(1 << bit);
            Value &= ~(1 << LayerManager.LayerValues[bit]);
        }

        /************************************************************************************************************************/

        public void OnLayersChanged(string[] oldLayers, string[] newLayers)
        {
            var layerNames = WeaverUtilities.GetList<string>();

            for (int i = 0; i < oldLayers.Length; i++)
                if (HasBit(layers, i))
                    layerNames.Add(oldLayers[i]);

            layers = 0;
            Value = 0;
            for (int i = 0; i < newLayers.Length; i++)
                if (layerNames.Contains(newLayers[i]))
                    AddBit(i);

            layerNames.Release();
        }

        /************************************************************************************************************************/

        public void CalculateValue()
        {
            Value = 0;
            for (int i = 0; i < LayerManager.LayerValues.Length; i++)
                if (HasBit(layers, i))
                    Value |= 1 << LayerManager.LayerValues[i];
        }

        /************************************************************************************************************************/

        public bool ValidateName()
        {
            if (string.IsNullOrEmpty(name) || char.IsDigit(name[0]))
                return false;

            // Remove spaces.
            name = name.Replace(" ", "");

            // If including layer masks, no custom mask can have the same name as a layer.
            if (LayerManager.Settings.includeLayerMasks)
            {
                var layers = LayerManager.NewLayerNames;
                if (Array.IndexOf(layers, name) >= 0)
                    return false;
            }

            // If anything earlier in the mask list has the same name, this one is invalid.
            var settings = LayerManager.Settings;
            var count = settings.CustomMaskCount;
            for (int i = 0; i < count; i++)
            {
                var otherMask = settings.GetMask(i);
                if (otherMask == this)
                    break;
                else if (otherMask.name == name)
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        public bool HasAllLayers()
        {
            return HasAllLayers(layers);
        }

        public bool HasAllLayers(int layers)
        {
            for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
                if (!HasBit(layers, i))
                    return false;

            return true;
        }

        /************************************************************************************************************************/

        public void SetAllLayers(bool on)
        {
            layers = 0;
            Value = 0;

            if (on)
                for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
                    AddBit(i);
        }

        /************************************************************************************************************************/

        public void AppendInitializer(StringBuilder text, int indent, object mask)
        {
            AppendInitializer(LayerManager, LayerManager.ScriptBuilder, text, indent, mask);
        }

        public static void AppendInitializer(LayerManager layerManager, LayersScriptBuilder scriptBuilder, StringBuilder text, int indent, object mask)
        {
            text.Append(" = ");

            var first = true;

            var maskValue = (int)mask;
            if (maskValue == 0)
            {
                text.Append('0');
                return;
            }

            var value = 0;
            for (int i = 0; i < layerManager.OldLayerNames.Length; i++)
            {
                if (HasBit(maskValue, layerManager.LayerValues[i]))
                {
                    if (first)
                        first = false;
                    else
                        text.Append(" | ");

                    text.Append(scriptBuilder.GetMaskValueName(i));

                    value |= 1 << layerManager.LayerValues[i];
                }
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

