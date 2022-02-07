// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural Scenes script.
    /// </summary>
    public sealed class ScenesPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Scene Constants";

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected override ProceduralScriptSettings Settings => WeaverSettings.Scenes;

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Scene Constants", "/docs/project-constants/scenes");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            const string BuildSettingsPath = "File/Build Settings...";
            if (GUILayout.Button(
                WeaverEditorUtilities.TempContent("Build Settings [Ctrl + Shift + B]", BuildSettingsPath),
                WeaverEditorUtilities.DontExpandWidth))
            {
                EditorApplication.ExecuteMenuItem(BuildSettingsPath);
            }

            WeaverSettings.Scenes.DoGUI();

            DoInjectorListGUI();
        }

        /************************************************************************************************************************/
    }
}

#endif

