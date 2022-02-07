// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Text;
using UnityEngine;
using Weaver.Editor.Procedural.Scripting;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the physics layers in your project.
    /// </summary>
    public class LayersScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        internal virtual LayerManager LayerManager => LayerManager.Instance;

        /************************************************************************************************************************/

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => LayerManager.Settings.enabled;

        /************************************************************************************************************************/

        /// <summary>Appends the declaration of the elements of this script in C# code to the specified `text`.</summary>
        protected override void AppendScript(StringBuilder text)
        {
            if (ScriptGenerator.SaveMessage != null)
            {
                LayerManager.UpdateLayerNames();
                AppendSaveMessage();
            }

            base.AppendScript(text);
        }

        /************************************************************************************************************************/

        /// <summary>Appends the details of the current script to the <see cref="ScriptGenerator.SaveMessage"/>.</summary>
        protected virtual void AppendSaveMessage()
        {
            ScriptGenerator.SaveMessage.Append("Layer Names: ").AppendLineConst(LayerManager.OldLayerNames.Length);
            ScriptGenerator.SaveMessage.Append("Layers: ").AppendLineConst(LayerManager.Settings.includeLayers);
            ScriptGenerator.SaveMessage.Append("Layer Masks: ").AppendLineConst(LayerManager.Settings.includeLayerMasks);
            ScriptGenerator.SaveMessage.Append("Custom Masks: ").AppendLineConst(LayerManager.Settings.CountValidMasks());
            ScriptGenerator.SaveMessage.Append("Collision Matrix 2D: ").AppendLineConst(WeaverSettings.Layers.includeCollisionMatrix2D);
            ScriptGenerator.SaveMessage.Append("Collision Matrix 3D: ").AppendLineConst(WeaverSettings.Layers.includeCollisionMatrix3D);
        }

        /************************************************************************************************************************/

        private string GetFieldValueName(int index)
        {
            if (LayerManager.Settings.includeLayers)
            {
                if (LayerManager.Settings.includeLayerMasks)
                    index *= 2;

                return RootType.Elements[index].Name;
            }
            else
            {
                return LayerManager.LayerValues[index].ToString();
            }
        }

        /************************************************************************************************************************/

        internal string GetMaskValueName(int index)
        {
            if (LayerManager.Settings.includeLayerMasks)
            {
                if (LayerManager.Settings.includeLayers)
                    index = index * 2 + 1;

                return RootType.Elements[index].Name;
            }
            else
            {
                return $"(1 << {GetFieldValueName(index)})";
            }
        }

        /************************************************************************************************************************/
        #region Gather Layer Details
        /************************************************************************************************************************/

        private const int
            LayersRegion = 0,
            CustomMasksRegion = 1,
            CollisionMatrix2DRegion = 2,
            CollisionMatrix3DRegion = 3;

        internal static LayersScriptBuilder Instance { get; private set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="LayersScriptBuilder"/>.</summary>
        protected LayersScriptBuilder(Action<StringBuilder> generatorMethod, params string[] regions)
            : base(generatorMethod)
        {
            if (GetType() == typeof(LayersScriptBuilder))
                Instance = this;

            Regions = regions;
        }

        /// <summary>Creates a new <see cref="LayersScriptBuilder"/>.</summary>
        public LayersScriptBuilder(Action<StringBuilder> generatorMethod) : this(generatorMethod,
            "Layers -> Edit/Project Settings/Tags and Layers",
            "Custom Layer Masks -> " + WeaverUtilities.WeaverWindowPath,
            "2D Collision Matrix -> Edit/Project Settings/Physics 2D",
            "3D Collision Matrix -> Edit/Project Settings/Physics")
        { }

        /************************************************************************************************************************/

        /// <summary>Gathers the element details of this script.</summary>
        protected override void GatherScriptDetails()
        {
            LayerManager.UpdateLayerNames();

            for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
            {
                var value = LayerManager.LayerValues[i];
                var maskValue = 1 << value;

                var fieldNameSource = LayerManager.OldLayerNames[i];
                Scripting.FieldBuilder field;

                if (LayerManager.Settings.includeLayers)
                {
                    field = RootType.AddField(LayerManager.OldLayerNames[i], value);
                    field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                    field.RegionIndex = LayersRegion;
                    field.CommentBuilder = (text) => text.Append(field.NameSource).Append(" (Mask Value = ").Append(maskValue).Append(")");
                }
                else
                {
                    field = null;
                }

                if (LayerManager.Settings.includeLayerMasks)
                {
                    var maskField = RootType.AddField(LayerManager.OldLayerNames[i] + "Mask", maskValue);
                    maskField.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                    maskField.AppendInitializer = (text, indent, val) =>
                        text.Append(" = 1 << ").Append(field != null ? field.Name : value.ToString());
                    maskField.RegionIndex = LayersRegion;
                    maskField.CommentBuilder = (text) =>
                        text.Append("Layer Mask for ").Append(fieldNameSource).Append(" = ").Append(maskValue);
                }
            }

            GatherExtraMembers();
            GatherCustomMasks();
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the details of members in addition to the main layers.</summary>
        protected virtual void GatherExtraMembers()
        {
            if (WeaverSettings.Layers.includeCollisionMatrix2D)
                GatherCollisionMatrix("CollisionMask2D", "2D Collision Mask for ", CollisionMatrix2DRegion, Physics2D.GetIgnoreLayerCollision);

            if (WeaverSettings.Layers.includeCollisionMatrix3D)
                GatherCollisionMatrix("CollisionMask3D", "3D Collision Mask for ", CollisionMatrix3DRegion, Physics.GetIgnoreLayerCollision);
        }

        /************************************************************************************************************************/

        private void GatherCollisionMatrix(string nameSuffix, string commentPrefix, int regionIndex,
            Func<int, int, bool> getIgnoreLayerCollision)
        {
            for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
            {
                var name = LayerManager.OldLayerNames[i];

                var layerValue = LayerManager.LayerValues[i];

                var mask = 0;
                for (int j = 0; j < LayerManager.LayerValues.Length; j++)
                {
                    var otherLayerValue = LayerManager.LayerValues[j];
                    if (!getIgnoreLayerCollision(layerValue, otherLayerValue))
                    {
                        mask |= 1 << otherLayerValue;
                    }
                }

                var field = RootType.AddField(name + nameSuffix, mask);
                field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                field.RegionIndex = regionIndex;
                field.CommentBuilder = (text) => text.Append(commentPrefix).Append(name).Append(" (Mask Value: ").Append(mask).Append(")");
                field.AppendInitializer = (text, indent, value) => CustomLayerMask.AppendInitializer(LayerManager, this, text, indent, value);
            }
        }

        /************************************************************************************************************************/

        private void GatherCustomMasks()
        {
            var settings = LayerManager.Settings;
            var count = settings.CustomMaskCount;
            for (int i = 0; i < count; i++)
            {
                var mask = settings.GetMask(i);

                if (mask.ValidateName())
                {
                    var field = RootType.AddField(mask.name + "Mask", mask.Value);
                    field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                    field.RegionIndex = CustomMasksRegion;
                    field.CommentBuilder = (text) =>
                    {
                        text.Append(mask.name).Append(" (Mask Value: ").Append(mask.Value).Append(")");
                        if (!string.IsNullOrEmpty(mask.comment))
                            text.Append(": ").Append(mask.comment);
                    };
                    field.AppendInitializer = mask.AppendInitializer;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

