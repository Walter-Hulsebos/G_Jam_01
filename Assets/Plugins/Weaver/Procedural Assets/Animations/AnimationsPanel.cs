// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural Animations script.
    /// </summary>
    public sealed class AnimationsPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Animation Constants";

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected override ProceduralScriptSettings Settings => WeaverSettings.Animations;

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Animation Constants", "/docs/project-constants/animations");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            WeaverSettings.Animations.DoGUI();

            var enabled = GUI.enabled;
            GUI.enabled = true;
            if (GUILayout.Button("If you're interested in an animation system that doesn't require these constants," +
                " click here to check out Animancer on the Asset Store.", EditorStyles.helpBox))
                AssetStore.Open("content/116516");
            GUI.enabled = enabled;

            DoInjectorListGUI();
        }

        /************************************************************************************************************************/
    }
}

#endif

