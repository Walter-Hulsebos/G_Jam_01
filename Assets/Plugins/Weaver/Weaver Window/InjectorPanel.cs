// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="WeaverWindowPanel"/> containing the details of all asset injection attributes in the project
    /// except for procedural assets which use the <see cref="ProceduralPanel"/>.
    /// </summary>
    internal sealed class InjectionPanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/

        private string _Name;

        /// <summary>The display name of this panel.</summary>
        protected override string Name => _Name ?? (_Name = "Injectors [" + VisibleInjectorCount + "]");

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Asset Injection", "/docs/asset-injection");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            DoGroupedInjectorListGUI();
            WeaverSettings.Injection.DoGUI();

            Procedural.InjectorScriptBuilder.Instance.ProceduralAsset.DoGUI();
        }

        /************************************************************************************************************************/
    }
}

#endif

