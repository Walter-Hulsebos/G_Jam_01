// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Manages the GUI for changing the type of an <see cref="AssetListBase"/>.
    /// </summary>
    internal sealed class AssetListTypeChanger
    {
        /************************************************************************************************************************/

        private AssetListEditor _Editor;

        private readonly AnimBool ExpansionAnimator = new AnimBool();

        private Type _SelectedAssetType;

        private int _SelectedListType;
        private readonly List<AssetListTypeInfo> ListTypes = new List<AssetListTypeInfo>();
        private GUIContent[] _ListTypeContents;

        private string _NewListTypeName;
        private Type _NewListAssetType;
        private bool _NewListTypeIsLazy;
        private Type _NewListMetaDataType;

        /************************************************************************************************************************/

        public AssetListTypeChanger(AssetListEditor editor)
        {
            _Editor = editor;
        }

        /************************************************************************************************************************/

        public void DoGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            var target = _Editor.Target;

            if (!ExpansionAnimator.target && !ExpansionAnimator.isAnimating)
            {
                DoAssetTypeButton("Asset Type", target.AssetType);
                DoLazyToggle(target.IsLazy);
                DoMetaDataTypeLabel(target.MetaDataType);
            }
            else
            {
                EditorGUILayout.LabelField(
                    WeaverEditorUtilities.TempContent("Asset Type"),
                    WeaverEditorUtilities.TempTypeContent(target.AssetType));

                EditorGUI.BeginDisabledGroup(true);
                DoLazyToggle(target.IsLazy);
                DoMetaDataTypeLabel(target.MetaDataType);
                EditorGUI.EndDisabledGroup();

                GUILayout.BeginVertical(EditorStyles.helpBox);

                const float Indent = 4;
                EditorGUIUtility.labelWidth -= Indent;

                if (EditorGUILayout.BeginFadeGroup(ExpansionAnimator.faded))
                {
                    GUILayout.Label("Change List Type");
                    DoAssetTypeButton("Asset Type", _SelectedAssetType);

                    DoListTypeGUI();
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUIUtility.labelWidth += Indent;

                GUILayout.EndVertical();
            }

            if (ExpansionAnimator.isAnimating)
                _Editor.Repaint();

            EditorGUI.EndDisabledGroup();
        }

        /************************************************************************************************************************/
        #region Asset Type
        /************************************************************************************************************************/

        private void DoAssetTypeButton(string label, Type assetType)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            if (EditorGUILayout.DropdownButton(WeaverEditorUtilities.TempTypeContent(assetType), FocusType.Passive))
                ShowAssetTypeDropdown();
            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private void ShowAssetTypeDropdown()
        {
            GetDirectory(out var directory, out var directoryPath);
            var assetTypes = GetAssetTypesInDirectory(directoryPath, _Editor.Target.Recursive);

            var assetToListType = WeaverUtilities.GetDictionary<Type, Type>();
            var listTypes = AssetListTypeInfo.ListTypes;
            for (int i = 0; i < listTypes.Count; i++)
            {
                var type = listTypes[i];
                assetToListType.Add(type.AssetType, type.ListType);
            }

            var menu = new GenericMenu();

            var currentAssetType = _SelectedAssetType ?? _Editor.Target.AssetType;
            for (int i = 0; i < assetTypes.Length; i++)
            {
                var assetType = assetTypes[i];
                var selected = assetType == currentAssetType;

                var content = new GUIContent(assetType.GetNameCS());
                if (assetToListType.TryGetValue(assetType, out var listType))
                    content.text += "  (" + listType.GetNameCS() + ")";
                else
                    content.text += "  (new)";

                menu.AddItem(content, selected, () =>
                {
                    _Editor.Target.Directory = directory;
                    SetSelectedAssetType(assetType);
                });
            }

            assetToListType.Release();
            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private void GetDirectory(out DefaultAsset directory, out string directoryPath)
        {
            directory = _Editor.Target.Directory;
            directoryPath = _Editor.Target.GetAndVerifyDirectoryPath();
            if (directoryPath == null)
            {
                directoryPath = AssetDatabase.GetAssetPath(_Editor.Target as Object);
                if (directoryPath == null)
                {
                    directory = null;
                }
                else
                {
                    directoryPath = Path.GetDirectoryName(directoryPath);
                    directory = AssetDatabase.LoadAssetAtPath<DefaultAsset>(directoryPath);
                }
            }
        }

        /************************************************************************************************************************/

        private static Type[] GetAssetTypesInDirectory(string directoryPath, bool recursive)
        {
            var types = WeaverUtilities.GetHashSet<Type>();
            GatherAssetTypesInDirectory(types, directoryPath, recursive);

            Type[] assetTypes;
            if (types.Count == 0)
            {
                assetTypes = new Type[] { typeof(GameObject) };
            }
            else
            {
                assetTypes = new Type[types.Count];
                types.CopyTo(assetTypes, 0);
                Array.Sort(assetTypes, (a, b) => a.FullName.CompareTo(b.FullName));
            }

            types.Release();
            return assetTypes;
        }

        /************************************************************************************************************************/

        private static void GatherAssetTypesInDirectory(HashSet<Type> types, string directoryPath, bool recursive)
        {
            var files = Directory.GetFiles(directoryPath);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (file.EndsWith(".meta"))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(file);
                if (asset != null)
                {
                    var type = asset.GetType();
                    GatherAssetType(types, type);

                    if (asset is GameObject gameObject)
                    {
                        var components = gameObject.GetComponents<Component>();
                        for (int j = 0; j < components.Length; j++)
                        {
                            var component = components[j];
                            if (component != null)
                                GatherAssetType(types, component.GetType());
                        }
                    }
                }
            }

            if (!recursive)
                return;

            files = Directory.GetDirectories(directoryPath);
            for (int i = 0; i < files.Length; i++)
            {
                GatherAssetTypesInDirectory(types, files[i], recursive);
            }
        }

        /************************************************************************************************************************/

        private static void GatherAssetType(HashSet<Type> types, Type assetType)
        {
            if (types.Contains(assetType))
                return;

            types.Add(assetType);

            var listTypes = AssetListTypeInfo.ListTypes;
            for (int i = 0; i < listTypes.Count; i++)
            {
                var listAssetType = listTypes[i].AssetType;
                if (listAssetType.IsAssignableFrom(assetType))
                    types.Add(listAssetType);
            }
        }

        /************************************************************************************************************************/

        private void SetSelectedAssetType(Type type)
        {
            if (!ExpansionAnimator.target && _Editor.Target != null)
            {
                var directoryPath = _Editor.Target.GetAndVerifyDirectoryPath();
                if (directoryPath != null)
                    _NewListTypeIsLazy = WeaverEditorUtilities.IsResource(directoryPath, out _);
            }

            _SelectedAssetType = type;
            if (type != null)
            {
                ExpansionAnimator.target = true;
                RefreshListTypes();
            }
            else
            {
                ExpansionAnimator.target = false;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region List Type
        /************************************************************************************************************************/

        private void RefreshListTypes()
        {
            // Gather all list types that can target the selected asset type.
            GatherListTypes(_SelectedAssetType);

            _SelectedListType = 0;

            // Initialize the GUI Content for each potential list type.
            var count = ListTypes.Count;
            WeaverUtilities.SetSize(ref _ListTypeContents, count + 1);
            for (int i = 0; i < count; i++)
            {
                _ListTypeContents[i] = new GUIContent(WeaverEditorUtilities.TempTypeContent(ListTypes[i].ListType));
            }

            ListTypes.Add(null);
            _ListTypeContents[count] = new GUIContent("Create New Type");

            _NewListAssetType = _SelectedAssetType;
            UpdateNewListTypeName();
            _Editor.Repaint();
        }

        /************************************************************************************************************************/

        private void UpdateNewListTypeName()
        {
            _NewListTypeName =
                _NewListAssetType == typeof(GameObject) ?
                "PrefabList" :
                _NewListAssetType.Name + "List";

            _NewListMetaDataType = null;
        }

        /************************************************************************************************************************/

        private void GatherListTypes(Type assetType)
        {
            ListTypes.Clear();

            var allListTypes = AssetListTypeInfo.ListTypes;

            for (int i = 0; i < allListTypes.Count; i++)
            {
                var listType = allListTypes[i];
                if (listType.AssetType == assetType)
                {
                    ListTypes.Add(listType);
                }
            }

            ListTypes.Sort((a, b) => a.ListType.FullName.CompareTo(b.ListType.FullName));
        }

        /************************************************************************************************************************/

        private void DoListTypeGUI()
        {
            // List Type.
            _SelectedListType = EditorGUILayout.Popup(WeaverEditorUtilities.TempContent("List Type"), _SelectedListType, _ListTypeContents);

            // Revert / Apply.
            var selectedListType = ListTypes[_SelectedListType];
            if (selectedListType != null)
            {
                EditorGUI.BeginDisabledGroup(true);

                // Lazy.
                DoLazyToggle(selectedListType.ListType.IsSubclassOfGenericDefinition(AssetListBase.LazyAssetListType));

                // Meta Data.
                var arguments = ReflectionUtilities.GetGenericInterfaceArguments(selectedListType.AssetType, typeof(IMetaDataProvider<>));
                DoMetaDataTypeLabel(arguments?[0]);

                EditorGUI.EndDisabledGroup();

                // Apply.
                if (DoRevertApplyGUI("Apply"))
                {
                    SetSelectedAssetType(null);
                    _CachedInstance = this;
                    _Editor.ScriptProperty.objectReferenceValue = selectedListType.Script;
                    WeaverSettings.ForceReSerialize();
                    AssetListOverlay.ClearCache();
                }
            }
            else// Create New List Type.
            {
                // Asset Type.
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(WeaverEditorUtilities.TempContent("New List Asset Type"));
                if (EditorGUILayout.DropdownButton(WeaverEditorUtilities.TempTypeContent(_NewListAssetType), FocusType.Passive))
                    ShowNewListAssetTypeDropdown();
                GUILayout.EndHorizontal();

                // Lazy.
                DoLazyToggle(_NewListTypeIsLazy);

                // Meta Data.
                DoMetaDataTypeDropdown();

                // Type Name.
                var color = GUI.color;
                {
                    if (!EditorApplication.isCompiling && AssetListTypeInfo.ScriptExists(_NewListTypeName))
                        GUI.color = WeaverEditorUtilities.WarningColor;

                    _NewListTypeName = EditorGUILayout.TextField("New List Type Name", _NewListTypeName);
                }
                GUI.color = color;

                // Generate.
                if (DoRevertApplyGUI("Generate List Type"))
                    GenerateListType();
            }
        }

        /************************************************************************************************************************/

        private void ShowNewListAssetTypeDropdown()
        {
            var menu = new GenericMenu();

            var type = _SelectedAssetType;
            while (true)
            {
                var isSelected = type == _NewListAssetType;
                var capturedType = type;
                menu.AddItem(new GUIContent(type.GetNameCS()), isSelected, () =>
                {
                    _NewListAssetType = capturedType;
                    UpdateNewListTypeName();
                });

                if (type != typeof(Object))
                    type = type.BaseType;
                else
                    break;
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private void DoLazyToggle(bool value)
        {
            var label = WeaverEditorUtilities.TempContent("Is Lazy",
                "If true: this list will only load its assets as they are needed rather than on startup.");

            EditorGUI.BeginChangeCheck();
            var lazy = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                if (!ExpansionAnimator.target)
                {
                    SetSelectedAssetType(_Editor.Target.AssetType);
                    _SelectedListType = ListTypes.Count - 1;
                }

                _NewListTypeIsLazy = lazy;
                UpdateNewListTypeName();
            }
        }

        /************************************************************************************************************************/

        private static void DoMetaDataTypeLabel(Type metaDataType)
        {
            EditorGUILayout.LabelField("Meta Data Type", metaDataType != null ? metaDataType.GetNameCS() : "None");
        }

        /************************************************************************************************************************/

        private void DoMetaDataTypeDropdown()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(WeaverEditorUtilities.TempContent("Meta Data Type"));

            var metaDataTypes = MetaDataUtils.GetMetaDataTypes(_NewListAssetType);
            if (metaDataTypes == null)
            {
                GUILayout.Label("None");
            }
            else
            {
                if (EditorGUILayout.DropdownButton(WeaverEditorUtilities.TempTypeContent(_NewListMetaDataType), FocusType.Passive))
                {
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), _NewListMetaDataType == null, () =>
                    {
                        _NewListMetaDataType = null;
                    });

                    for (int i = 0; i < metaDataTypes.Count; i++)
                    {
                        var metaDataType = metaDataTypes[i];
                        menu.AddItem(new GUIContent(metaDataType.GetNameCS()), _NewListMetaDataType == metaDataType, () =>
                        {
                            _NewListMetaDataType = metaDataType;
                        });
                    }

                    menu.ShowAsContext();
                }
            }

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private bool DoRevertApplyGUI(string applyLabel)
        {
            var enabled = GUI.enabled;
            if (EditorApplication.isCompiling)
                GUI.enabled = false;

            GUILayout.BeginHorizontal();

            EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            if (GUILayout.Button("Revert", WeaverEditorUtilities.DontExpandWidth))
            {
                SetSelectedAssetType(null);
            }

            var applyClicked = GUILayout.Button(applyLabel, WeaverEditorUtilities.DontExpandWidth);

            GUILayout.EndHorizontal();

            GUI.enabled = enabled;

            return applyClicked;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region List Type Generation
        /************************************************************************************************************************/

        private static AssetListTypeChanger _CachedInstance;

        private const string CachePath = "Temp/Weaver.NewList.txt";

        /************************************************************************************************************************/

        private void GenerateListType()
        {
            // Generate a script containing the new list type and save its path to retain the settings once assemblies are reloaded.

            Type baseType;
            if (_NewListMetaDataType == null)
            {
                baseType = _NewListTypeIsLazy ? AssetListBase.LazyAssetListType : AssetListBase.GenericAssetListType;
                baseType = baseType.MakeGenericType(ReflectionUtilities.OneType(_NewListAssetType));
            }
            else
            {
                baseType = AssetListBase.MetaAssetListType;
                baseType = baseType.MakeGenericType(ReflectionUtilities.TwoTypes(_NewListAssetType, _NewListMetaDataType));
            }

            var editorOnly = WeaverEditorUtilities.IsEditorOnly(_NewListAssetType);

            var scriptPath = AssetListTypeInfo.GenerateScript(_NewListTypeName, editorOnly, baseType);

            File.WriteAllText(CachePath, scriptPath);
        }

        /************************************************************************************************************************/

        public static AssetListTypeChanger TryLoadFromCache(AssetListEditor editor)
        {
            if (_CachedInstance != null)
            {
                _CachedInstance._Editor = editor;
                return WeaverUtilities.Nullify(ref _CachedInstance);
            }

            var instance = new AssetListTypeChanger(editor);
            if (!File.Exists(CachePath))
                return instance;

            var scriptPath = File.ReadAllText(CachePath);
            File.Delete(CachePath);

            var allListTypes = AssetListTypeInfo.ListTypes;
            for (int i = 0; i < allListTypes.Count; i++)
            {
                var listType = allListTypes[i];
                if (scriptPath == AssetDatabase.GetAssetPath(listType.Script))
                {
                    // The directory gets lost if we don't re-assign it for some reason.
                    editor.ScriptProperty.objectReferenceValue = listType.Script;
                    editor.serializedObject.ApplyModifiedProperties();
                    AssetListOverlay.ClearCache();
                    break;
                }
            }

            return instance;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

