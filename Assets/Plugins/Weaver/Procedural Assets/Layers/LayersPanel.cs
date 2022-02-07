// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural Layers script.
    /// </summary>
    public sealed class LayersPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        private CustomMaskListGUI<CustomLayerMask> _Masks;

        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Layer Constants";

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected override ProceduralScriptSettings Settings => WeaverSettings.Layers;

        /************************************************************************************************************************/

        /// <summary>Sets up the initial state of this panel.</summary>
        public override void Initialize(int index)
        {
            base.Initialize(index);

            _Masks = new CustomMaskListGUI<CustomLayerMask>(
                LayerManager.Instance,
                "Custom Layer Masks",
                new GUIContent("Project Settings", "Edit/Project Settings..."));
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Layer Constants", "/docs/project-constants/layers");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            _Masks.DoGUI();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="WeaverWindow"/>.OnDisable().
        /// </summary>
        public override void OnDisable()
        {
            _Masks.OnDisable();
        }

        /************************************************************************************************************************/
    }
}

#endif

