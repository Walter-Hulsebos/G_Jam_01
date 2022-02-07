// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A system which draws a visual indicator in the project window for any asset linked to an
    /// <see cref="AssetInjectionAttribute"/>.
    /// </summary>
    internal static class ProjectWindowOverlays
    {
        /************************************************************************************************************************/

        public static void OnShowSettingChanged()
        {
            EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemOnGUI;

            if (WeaverSettings.Injection.showProjectWindowOverlays)
                EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;

            EditorApplication.RepaintProjectWindow();
        }

        /************************************************************************************************************************/

        private static string _PreviousGUID;

        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            // If the GUID is the same as the previous one, the current item must be a sub-asset so we can skip it.
            if (_PreviousGUID == guid)
                return;

            _PreviousGUID = guid;

            if (AssetInjectionOverlay.TryDoGUI(guid, selectionRect))
                return;

            if (AssetListOverlay.TryDoGUI(guid, selectionRect))
                return;

            if (PotentialAssetInjectionOverlay.TryDoGUI(guid, selectionRect))
                return;
        }

        /************************************************************************************************************************/

        public static bool DoGUI(Rect selectionRect, string tooltip, Texture icon)
        {
            if (selectionRect.height > EditorGUIUtility.singleLineHeight)
            {
                selectionRect.x -= selectionRect.width * 0.05f + 5;
                selectionRect.y -= selectionRect.width * -0.01f + 5;
                selectionRect.width *= 0.5f;
                selectionRect.height = selectionRect.width;
            }
            else
            {
                selectionRect.y += 1;
                selectionRect.height -= 2;
                selectionRect.xMin = selectionRect.xMax - selectionRect.height;
                //selectionRect.x -= 1;
            }

            var content = WeaverEditorUtilities.TempContent("", tooltip, icon);

            // If the user is Right Clicking, use a Button in case it should open a Context Menu.
            if (Event.current.button == 1)
            {
                if (GUI.Button(selectionRect, content, GUIStyle.none))
                {
                    return true;
                }
            }
            else// Otherwise just draw the overlay (because we only want the button to intercept Right Clicks, not Left Clicks).
            {
                GUI.Label(selectionRect, content, GUIStyle.none);
            }

            return false;
        }

        /************************************************************************************************************************/
    }
}

#endif

