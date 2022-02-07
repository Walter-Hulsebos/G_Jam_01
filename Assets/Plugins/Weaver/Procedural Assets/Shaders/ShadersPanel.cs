// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural Shaders script.
    /// </summary>
    public sealed class ShadersPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        private ReorderableList _Shaders;

        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Shader Constants";

        /// <summary>The base settings for the procedural script this panel manages.</summary>
        protected override ProceduralScriptSettings Settings => WeaverSettings.Shaders;

        /************************************************************************************************************************/

        private void InitializeList()
        {
            if (_Shaders != null && _Shaders.serializedProperty.serializedObject.targetObject != null)
                return;

            var property = WeaverSettings.Shaders.GetShadersListProperty();

            _Shaders = new ReorderableList(property.serializedObject, property)
            {
                footerHeight = EditorGUIUtility.singleLineHeight,
                drawHeaderCallback = DoListHeaderGUI,
                drawElementCallback = DoListElementGUI,
                drawFooterCallback = DoListFooterGUI,
                displayAdd = false,
                displayRemove = false,
                elementHeight = EditorGUIUtility.singleLineHeight,
            };
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Shader Constants", "/docs/project-constants/shaders");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {
            InitializeList();

            EditorGUI.BeginChangeCheck();

            _Shaders.serializedProperty.serializedObject.Update();
            _Shaders.DoLayoutListFixed();
            _Shaders.serializedProperty.serializedObject.ApplyModifiedProperties();

            DoInjectorListGUI();

            if (EditorGUI.EndChangeCheck())
                WeaverSettings.Shaders.CleanList();
        }

        /************************************************************************************************************************/

        private void DoListHeaderGUI(Rect rect)
        {
            GUI.Label(rect, "Target Shaders");
        }

        /************************************************************************************************************************/

        private void DoListElementGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            var shaderProperty = _Shaders.serializedProperty.GetArrayElementAtIndex(index);

            var right = rect.xMax;

            EditorGUI.BeginChangeCheck();

            var shader = shaderProperty.objectReferenceValue;

            rect.y -= 1;
            rect.width = GUIStyles.RemoveButtonWidth;

            var content = GUIStyles.GetTempRemoveButton("Remove this Shader");
            if (GUI.Button(rect, content, GUIStyles.RemoveButtonStyle))
                shader = null;

            rect.y += 1;
            rect.x += rect.width;
            rect.xMax = right;

            shader = EditorGUI.ObjectField(rect, shader, typeof(Shader), false);

            if (EditorGUI.EndChangeCheck())
            {
                shaderProperty.objectReferenceValue = shader;
            }
        }

        /************************************************************************************************************************/

        private void DoListFooterGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyles.FooterBackgroundStyle.Draw(rect, false, false, false, false);
            }

            rect.y -= 2;

            rect.xMin += 5;
            rect.width -= 6;
            var right = rect.xMax;

            rect.width = EditorGUIUtility.labelWidth - 5;
            GUI.Label(rect, "Add Shader");

            rect.x += rect.width;
            rect.xMax = right;
            var shader = EditorGUI.ObjectField(rect, null, typeof(Shader), false);

            if (shader != null)
            {
                WeaverSettings.Shaders.shaders.Add((Shader)shader);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

