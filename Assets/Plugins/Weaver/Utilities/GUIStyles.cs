// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal] Various common GUI utilities.</summary>
    internal static class GUIStyles
    {
        /************************************************************************************************************************/

        public const float
            RemoveButtonWidth = 20;

        public static readonly GUIStyle
            HeaderBackgroundStyle = new GUIStyle("RL Header") { fixedHeight = 0 },
            FooterBackgroundStyle = new GUIStyle("RL Footer") { fixedHeight = 0 },
            BackgroundStyle = new GUIStyle("RL Background") { stretchHeight = false },
            RemoveButtonStyle = "RL FooterButton",
            FakeObjectFieldStyle = new GUIStyle(EditorStyles.textField) { imagePosition = ImagePosition.ImageLeft };

        public static readonly GUIStyle SmallButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            padding = new RectOffset(2, 3, 2, 2),
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = EditorGUIUtility.singleLineHeight,
            fixedWidth = EditorGUIUtility.singleLineHeight - 1,
        };

        private static readonly Texture
            MinusIcon = EditorGUIUtility.IconContent("Toolbar Minus").image;

        public static GUIContent GetTempRemoveButton(string tooltip)
        {
            return WeaverEditorUtilities.TempContent(null, tooltip, MinusIcon);
        }

        /************************************************************************************************************************/
    }
}

#endif

