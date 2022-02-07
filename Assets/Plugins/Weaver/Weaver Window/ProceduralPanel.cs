// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="WeaverWindowPanel"/> containing the details of all procedural assets in the project.
    /// </summary>
    internal sealed class ProceduralPanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/

        private string _Name;

        /// <summary>The display name of this panel.</summary>
        protected override string Name => _Name ?? (_Name = "Procedural Assets [" + VisibleInjectorCount + "]");

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Procedural Assets", "/docs/procedural-assets");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            if (Injectors.Count == 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("There are no custom procedural assets in the project.");
                GUILayout.EndVertical();
            }
            else
            {
                DoGroupedInjectorListGUI();
            }

            ProceduralAssetSettings.Instance.DoGUI();
            AssetGenerators.DoGUI();
        }

        /************************************************************************************************************************/
    }
}

#endif

