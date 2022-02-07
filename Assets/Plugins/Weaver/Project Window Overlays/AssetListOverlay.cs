// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A visual indicator for assets that are included in an asset list.
    /// </summary>
    internal sealed class AssetListOverlay
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<string, AssetListOverlay>
            GuidToOverlay = new Dictionary<string, AssetListOverlay>();

        private readonly List<AssetListBase> Lists;
        private readonly string Tooltip;

        /************************************************************************************************************************/

        public static bool TryDoGUI(string guid, Rect selectionRect)
        {
            var overlay = GetOverlay(guid);
            if (overlay == null)
                return false;

            var icon = overlay.GetIcon();
            if (icon == null)
                return false;

            if (ProjectWindowOverlays.DoGUI(selectionRect, overlay.Tooltip, icon))
                overlay.OpenContextMenu();

            return true;
        }

        /************************************************************************************************************************/

        private static AssetListOverlay GetOverlay(string guid)
        {
            if (!GuidToOverlay.TryGetValue(guid, out var overlay))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset != null)
                {
                    var lists = new List<AssetListBase>();
                    GatherContainingLists(assetPath, asset, lists);

                    if (lists.Count > 0)
                        overlay = new AssetListOverlay(lists);
                }

                GuidToOverlay.Add(guid, overlay);
            }

            return overlay;
        }

        /************************************************************************************************************************/

        private static void GatherContainingLists(string assetPath, Object asset, List<AssetListBase> lists)
        {
            AssetListBase.RemoveMissingLists();

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                GatherContainingLists(lists, assetPath, null);
            }

            assetPath = Path.GetDirectoryName(assetPath);

            if (asset is GameObject prefab)
            {
                GatherContainingLists(lists, assetPath, (list) =>
                {
                    if (list.AssetType == typeof(GameObject))
                        return true;

                    if (typeof(Component).IsAssignableFrom(list.AssetType) &&
                        prefab.GetComponent(list.AssetType) != null)
                        return true;

                    return false;
                });
            }
            else
            {
                var assetType = asset.GetType();
                GatherContainingLists(lists, assetPath, (list) => list.AssetType.IsAssignableFrom(assetType));
            }
        }

        /************************************************************************************************************************/

        private static void GatherContainingLists(List<AssetListBase> lists, string assetPath, Func<AssetListBase, bool> validateType)
        {
            for (int i = 0; i < AssetListBase.AllLists.Count; i++)
            {
                var list = AssetListBase.AllLists[i];
                if (!AssetDatabase.Contains(list as Object))
                    continue;

                var targetPath = list.GetAndVerifyDirectoryPath();
                if (targetPath == null || (validateType != null && !validateType(list)))
                    continue;

                GatherIfContaining(lists, list, assetPath, targetPath);
            }
        }

        /************************************************************************************************************************/

        private static void GatherIfContaining(List<AssetListBase> lists, AssetListBase list, string assetPath, string targetPath)
        {
            if (list.Recursive)
            {
                if (assetPath.StartsWith(targetPath))
                {
                    lists.Add(list);
                }
            }
            else
            {
                if (assetPath == targetPath)
                {
                    lists.Add(list);
                }
            }
        }

        /************************************************************************************************************************/

        private AssetListOverlay(List<AssetListBase> lists)
        {
            Lists = lists;

            var tooltip = WeaverUtilities.GetStringBuilder();
            tooltip.Append("This asset is included in the following Asset Lists:");

            for (int i = 0; i < lists.Count; i++)
            {
                tooltip.AppendLineConst()
                    .AppendLineConst()
                    .Append("- ")
                    .Append(AssetDatabase.GetAssetPath(lists[i] as Object));
            }

            Tooltip = tooltip.ReleaseToString();
        }

        /************************************************************************************************************************/

        private void OpenContextMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Open Weaver Window"), false, () => Window.WeaverWindow.Ping(Lists));

            for (int i = 0; i < Lists.Count; i++)
            {
                var list = Lists[i] as Object;
                var content = new GUIContent("Select Asset List:    " + AssetDatabase.GetAssetPath(list));
                content.text = content.text.Replace("/", " -> ");

                menu.AddItem(content, false, () => Selection.activeObject = list);
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        static AssetListOverlay()
        {
            AssetPostprocessor.OnPostprocessAssets += (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths) =>
            {
                if (importedAssets.Length >= 0 ||
                    deletedAssets.Length >= 0 ||
                    movedAssets.Length >= 0)
                {
                    ClearCache();
                }
            };

            EditorApplication.playModeStateChanged += (change) => ClearCache();
        }

        /************************************************************************************************************************/

        public static void ClearCache()
        {
            GuidToOverlay.Clear();
            EditorApplication.RepaintProjectWindow();
        }

        /************************************************************************************************************************/

        private Texture GetIcon()
        {
            for (int i = 0; i < Lists.Count; i++)
            {
                var list = Lists[i] as Object;
                if (list == null)
                    continue;

                return Icons.AssetList;
            }

            return null;
        }

        /************************************************************************************************************************/
    }
}

#endif

