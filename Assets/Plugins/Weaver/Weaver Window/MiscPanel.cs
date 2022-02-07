// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="WeaverWindowPanel"/> containing things that don't fit in other panels.
    /// </summary>
    public sealed class MiscPanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Miscellaneous";

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            DoGroupedInjectorListGUI();
            WeaverSettings.Window.DoGUI();

            DoDeleteWeaverButton();
        }

        /************************************************************************************************************************/

        /// <summary>Draws a button to delete Weaver and returns true if the user clicks and confirms it.</summary>
        public static bool DoDeleteWeaverButton()
        {
            if (GUILayout.Button("Delete Weaver"))
                return WeaverEditorUtilities.AskAndDeleteWeaver();

            return false;
        }

        /************************************************************************************************************************/
    }
}

#endif

