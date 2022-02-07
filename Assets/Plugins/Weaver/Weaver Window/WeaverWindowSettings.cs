// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to the <see cref="Window.WeaverWindow"/>.
    /// </summary>
    [Serializable]
    internal sealed class WeaverWindowSettings
    {
        /************************************************************************************************************************/

        public int currentPanel;

        public string autoDock = "UnityEditor.InspectorWindow";

        public bool autoFocus = true;

        /************************************************************************************************************************/

        public void DoGUI()
        {
            autoDock = EditorGUILayout.TextField(WeaverEditorUtilities.TempContent(
                "Auto Dock",
                "The name of the EditorWindow type which the Weaver Window should dock with when opened (default is 'UnityEditor.InspectorWindow')"),
                autoDock);

            WeaverEditorUtilities.DoToggle(ref autoFocus, "Auto Focus",
                "If enabled: selecting anything in the Project or Hierarchy window will automatically show the Inspector");
        }

        /************************************************************************************************************************/
    }
}

#endif

