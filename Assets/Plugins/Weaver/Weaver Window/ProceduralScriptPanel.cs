// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A base <see cref="WeaverWindowPanel"/> class for panels specific to a certain procedural script.
    /// </summary>
    public abstract class ProceduralScriptPanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected abstract ProceduralScriptSettings Settings { get; }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Header GUI for this panel which is displayed regardless of whether it is expanded or not.
        /// </summary>
        public override void DoHeaderGUI()
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.boldLabel);
            var width = rect.width;

            rect.width = rect.height;
            Settings.enabled = GUI.Toggle(rect, Settings.enabled,
                WeaverEditorUtilities.TempContent("", "Determines whether this procedural script should be generated or not"));

            rect.x += rect.width;
            rect.width = width - rect.height;

            if (GUI.Button(rect, Name, EditorStyles.boldLabel))
            {
                if (CheckHeaderContextMenu())
                    return;

                IsExpanded = !IsExpanded;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

