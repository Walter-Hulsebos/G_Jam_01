// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Text;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the navigation areas in your project.
    /// </summary>
    public sealed class NavigationAreasScriptBuilder : LayersScriptBuilder
    {
        /************************************************************************************************************************/

        internal override LayerManager LayerManager => NavAreaManager.Instance;

        /************************************************************************************************************************/

        /// <summary>Appends the details of the current script to the <see cref="ScriptGenerator.SaveMessage"/>.</summary>
        protected override void AppendSaveMessage()
        {
            ScriptGenerator.SaveMessage.Append("Area Names: ").AppendLineConst(LayerManager.OldLayerNames.Length);
            ScriptGenerator.SaveMessage.Append("Areas: ").AppendLineConst(LayerManager.Settings.includeLayers);
            ScriptGenerator.SaveMessage.Append("Area Masks: ").AppendLineConst(LayerManager.Settings.includeLayerMasks);
            ScriptGenerator.SaveMessage.Append("Custom Masks: ").AppendLineConst(LayerManager.Settings.CountValidMasks());
        }

        /************************************************************************************************************************/

        internal static new NavigationAreasScriptBuilder Instance { get; private set; }

        /// <summary>Creates a new <see cref="NavigationAreasScriptBuilder"/>.</summary>
        public NavigationAreasScriptBuilder(Action<StringBuilder> generatorMethod) : base(generatorMethod,
            "Nav Areas -> Window/Navigation",
            "Custom Nav Area Masks -> " + WeaverUtilities.WeaverWindowPath)
        {
            Instance = this;
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the details of members in addition to the main layers.</summary>
        protected override void GatherExtraMembers() { }

        /************************************************************************************************************************/
    }
}

#endif

