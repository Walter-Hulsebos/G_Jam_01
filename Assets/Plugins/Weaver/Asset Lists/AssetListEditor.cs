// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only] A custom <see cref="UnityEditor.Editor"/> for <see cref="AssetListBase"/>.</summary>
    [CustomEditor(typeof(AssetListBase), true)]
    public class AssetListEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        [NonSerialized]
        private AssetListTypeChanger _TypeChanger;

        /// <summary>The list being inspected.</summary>
        public AssetListBase Target { get; private set; }

        /// <summary>The script property of the target list (used to change its type).</summary>
        public SerializedProperty ScriptProperty { get; private set; }

        /// <summary>The <see cref="AssetListBase.Directory"/> property of the target list.</summary>
        public SerializedProperty DirectoryProperty { get; private set; }

        /// <summary>The <see cref="AssetListBase.Recursive"/> property of the target list.</summary>
        public SerializedProperty RecursiveProperty { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="Target"/> has been modified and may need to be re-cast from the base
        /// <see cref="UnityEditor.Editor.target"/>.
        /// </summary>
        [NonSerialized]
        private bool _IsDirty;

        /************************************************************************************************************************/

        private void OnEnable()
        {
            ScriptProperty = serializedObject.FindProperty("m_Script");
            DirectoryProperty = serializedObject.FindProperty("_Directory");
            RecursiveProperty = serializedObject.FindProperty("_Recursive");
            _TypeChanger = AssetListTypeChanger.TryLoadFromCache(this);
            Target = target as AssetListBase;
            Target.AddToGlobalList();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by Unity to draw the target object's inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {

            if (_IsDirty)
            {
                Target = target as AssetListBase;
                Target.SetDirty();
                _IsDirty = false;
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(ScriptProperty,
                WeaverEditorUtilities.TempContent("List Type", "The script which defines the type of this list"));

            _TypeChanger.DoGUI();
            DoSettingsGUI();
            Target.DoDetailsGUI();

            if (serializedObject.ApplyModifiedProperties())
                _IsDirty = true;
        }

        /************************************************************************************************************************/

        private void DoSettingsGUI()
        {
            // Directory.
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, null, DirectoryProperty);
            var directoryAsset = EditorGUI.ObjectField(rect,
                WeaverEditorUtilities.TempContent(DirectoryProperty.displayName, DirectoryProperty.tooltip),
                DirectoryProperty.objectReferenceValue, typeof(DefaultAsset), false);
            DirectoryProperty.objectReferenceValue = directoryAsset;
            EditorGUI.EndProperty();

            // Directory Path.
            var directoryPath = Target.GetAndVerifyDirectoryPath();
            if (directoryAsset != null)
            {
                GUILayout.Label(directoryPath, EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.HelpBox("The target directory has not been assigned.", MessageType.Error);
            }

            // Recursive.
            EditorGUILayout.PropertyField(RecursiveProperty);
        }

        /************************************************************************************************************************/
    }
}

#endif

