// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="WeaverWindowPanel"/> which manages asset lists.
    /// </summary>
    internal sealed class AssetListsPanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/
        #region Panel
        /************************************************************************************************************************/

        /// <summary>
        /// All types derived from <see cref="AssetListBase"/> which have at least one instance in the project.
        /// </summary>
        private readonly HashSet<Type> UsedListTypes = new HashSet<Type>();

        /// <summary>
        /// A subset of <see cref="AssetListBase.AllLists"/> containing lists that are currently saved as assets.
        /// </summary>
        private readonly List<AssetListBase> SavedAssetLists = new List<AssetListBase>();

        private ReorderableList _AssetLists;
        private ReorderableList _AssetListTypes;
        private bool _HasUnusedListType;

        private int _ListCount = -1;
        private string _Name;

        /// <summary>The display name of this panel.</summary>
        protected override string Name => _Name;

        /************************************************************************************************************************/

        /// <summary>Sets up the initial state of this panel.</summary>
        public override void Initialize(int index)
        {
            base.Initialize(index);
            InitializeAssetLists();
            InitializeListTypes();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Header GUI for this panel which is displayed regardless of whether it is expanded or not.
        /// </summary>
        public override void DoHeaderGUI()
        {
            if (Event.current.type == EventType.Layout)
                RefreshGatheredLists();

            if (_ListCount != SavedAssetLists.Count)
            {
                _ListCount = SavedAssetLists.Count;
                _Name = "Asset Lists [" + _ListCount + "]";
            }

            base.DoHeaderGUI();
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions to the header context menu.</summary>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Asset Lists", "/docs/asset-lists");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Body GUI for this panel which is only displayed while it is expanded.
        /// </summary>
        public override void DoBodyGUI()
        {

            DoAssetListsGUI();
            DoListTypesGUI();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Asset Lists
        /************************************************************************************************************************/

        private void InitializeAssetLists()
        {
            _AssetLists = new ReorderableList(SavedAssetLists, typeof(AssetListBase))
            {
                drawHeaderCallback = DoAssetListsHeaderGUI,
                drawElementCallback = DoAssetListsElementGUI,
                elementHeight = EditorGUIUtility.singleLineHeight,
                footerHeight = 0,
                displayAdd = false,
                displayRemove = false,
            };
        }

        /************************************************************************************************************************/

        private void DoAssetListsGUI()
        {
            _AssetLists.DoLayoutListFixed();
        }

        /************************************************************************************************************************/

        private bool _AreAllListsGathered;

        private void RefreshGatheredLists()
        {
            if (!_AreAllListsGathered)
            {
                _AreAllListsGathered = true;

                var paths = WeaverEditorUtilities.FindAllAssetPaths(typeof(AssetListBase));
                for (int i = 0; i < paths.Length; i++)
                {
                    var list = AssetDatabase.LoadAssetAtPath<AssetListBase>(paths[i]);
                    if (list != null)
                        list.AddToGlobalList();
                }
            }
            else
            {
                AssetListBase.RemoveMissingLists();
            }

            SavedAssetLists.Clear();
            for (int i = 0; i < AssetListBase.AllLists.Count; i++)
            {
                var list = AssetListBase.AllLists[i];
                if (AssetDatabase.Contains(list))
                    SavedAssetLists.Add(list);
            }

            WeaverUtilities.StableInsertionSort(SavedAssetLists, (x, y) => string.Compare(x.name, y.name));
        }

        /************************************************************************************************************************/

        private void DoAssetListsHeaderGUI(Rect rect)
        {
            rect.xMin -= 2;
            GUI.Label(rect, "Asset Lists");
        }

        /************************************************************************************************************************/

        private void DoAssetListsElementGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            var list = SavedAssetLists[index];

            if (list == null)
            {
                GUI.Label(rect, "Missing Asset List");
                return;
            }

            WeaverWindow.DoPingGUI(rect, list);

            var content = WeaverEditorUtilities.TempContent(
                list.name,
                list.Tooltip,
                AssetPreview.GetMiniTypeThumbnail(list.AssetType));

            if (GUI.Button(rect, content, EditorStyles.textField))
            {
                Selection.activeObject = list;
                WeaverWindow.ShowInspectorWindow();
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region List Types
        /************************************************************************************************************************/

        private void InitializeListTypes()
        {
            _AssetListTypes = new ReorderableList(AssetListTypeInfo.ListTypes, typeof(Type))
            {
                drawHeaderCallback = DoListTypesHeaderGUI,
                drawElementCallback = DoListTypesElementGUI,
                elementHeight = EditorGUIUtility.singleLineHeight,
                footerHeight = 0,
                displayAdd = false,
                displayRemove = false,
                draggable = false,
            };
        }

        /************************************************************************************************************************/

        private void DoListTypesGUI()
        {
            _HasUnusedListType = false;

            RefreshListTypes();
            _AssetListTypes.DoLayoutListFixed();

            if (_HasUnusedListType)
                DoDeleteUnusedTypesGUI();
        }

        /************************************************************************************************************************/

        private static bool _AreListTypesSorted;

        private void RefreshListTypes()
        {
            if (Event.current.type != EventType.Layout)
                return;

            if (!_AreListTypesSorted)
            {
                _AreListTypesSorted = true;
                AssetListTypeInfo.ListTypes.Sort((x, y) => string.Compare(x.ListType.Name, y.ListType.Name));
            }

            UsedListTypes.Clear();

            for (int i = 0; i < SavedAssetLists.Count; i++)
            {
                var list = SavedAssetLists[i];
                if (list == null)
                    continue;

                var type = list.GetType();

                do
                {
                    UsedListTypes.Add(type);
                    type = type.BaseType;
                } while (type != null);
            }
        }

        /************************************************************************************************************************/

        private void DoListTypesHeaderGUI(Rect rect)
        {
            var right = rect.xMax;

            rect.xMin -= 2;
            rect.width = EditorGUIUtility.labelWidth - 4;
            GUI.Label(rect, "List Type");

            rect.x += rect.width;
            rect.xMax = right;
            GUI.Label(rect, "Asset Type");
        }

        /************************************************************************************************************************/

        private void DoListTypesElementGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            var info = AssetListTypeInfo.ListTypes[index];

            var isUsed = UsedListTypes.Contains(info.ListType);

            var right = rect.xMax;

            rect.xMin -= 2;
            rect.width = EditorGUIUtility.labelWidth - 4;
            info.DoScriptGUI(rect, ref isUsed);

            if (!isUsed)
                _HasUnusedListType = true;

            rect.x += rect.width;
            rect.xMax = right;

            GUIContent content;
            if (info.AssetType != null)
            {
                content = WeaverEditorUtilities.TempTypeContent(info.AssetType);
            }
            else
            {
                content = WeaverEditorUtilities.TempContent("Unknown");
            }

            GUI.Label(rect, content);
        }

        /************************************************************************************************************************/

        private void DoDeleteUnusedTypesGUI()
        {
            if (!GUILayout.Button("Delete Unused Asset List Types", EditorStyles.miniButton))
                return;

            var unusedTypes = WeaverUtilities.GetList<AssetListTypeInfo>();
            for (int i = 0; i < AssetListTypeInfo.ListTypes.Count; i++)
            {
                var typeInfo = AssetListTypeInfo.ListTypes[i];
                var listType = typeInfo.ListType;

                if (!UsedListTypes.Contains(listType) &&
                    AssetListTypeInfo.ShouldAllowDestroyScript(listType))
                {
                    unusedTypes.Add(typeInfo);
                }
            }

            var message = WeaverUtilities.GetStringBuilder();
            for (int i = 0; i < unusedTypes.Count; i++)
            {
                message.Append("- ")
                    .AppendLineConst(AssetDatabase.GetAssetPath(unusedTypes[i].Script));
            }

            message.AppendLineConst()
                .Append("You cannot undo this action, but you can just generate the scripts again if needed.");

            if (EditorUtility.DisplayDialog("Delete Unused Asset List Types?", message.ReleaseToString(), "Delete", "Cancel"))
            {
                for (int i = 0; i < unusedTypes.Count; i++)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(unusedTypes[i].Script));
                }
            }

            unusedTypes.Release();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

