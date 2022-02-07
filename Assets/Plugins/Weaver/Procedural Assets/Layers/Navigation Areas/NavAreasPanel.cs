// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural NavAreas script.
    /// </summary>
    public sealed class NavAreasPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        private CustomMaskListGUI<CustomNavAreaMask> _Masks;

        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Navigation Area Constants";

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected override ProceduralScriptSettings Settings => WeaverSettings.NavAreas;

        /************************************************************************************************************************/

        /// <summary>Sets up the initial state of this panel.</summary>
        public override void Initialize(int index)
        {
            base.Initialize(index);

            _Masks = new CustomMaskListGUI<CustomNavAreaMask>(
                NavAreaManager.Instance,
                "Custom Navigation Area Masks",
                new GUIContent("Navigation", "Window/Navigation"));
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Navigation Area Constants", "/docs/project-constants/navigation-areas");
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

