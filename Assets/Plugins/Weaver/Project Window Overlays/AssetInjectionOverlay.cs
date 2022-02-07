// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Weaver.Editor.Window;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A visual indicator in the project window for any asset linked to an <see cref="AssetInjectionAttribute"/>.
    /// </summary>
    internal class AssetInjectionOverlay
    {
        /************************************************************************************************************************/

        /// <summary>Assets mapped to their injection attribute.</summary>
        private static readonly Dictionary<Object, AssetInjectionAttribute>
            AssetToAttribute = new Dictionary<Object, AssetInjectionAttribute>();

        /// <summary>Injection attributes mapped to the GUID of their target asset.</summary>
        private static readonly Dictionary<AssetInjectionAttribute, string>
            AttributeToGUID = new Dictionary<AssetInjectionAttribute, string>();

        /// <summary>A pool of spare overlays which can be reused.</summary>
        private static readonly ObjectPool<AssetInjectionOverlay>
            OverlayPool = new ObjectPool<AssetInjectionOverlay>(() => new AssetInjectionOverlay(new List<AssetInjectionAttribute>()));

        /// <summary>Asset GUIDs mapped to the <see cref="AssetInjectionOverlay"/> which wraps their <see cref="AssetInjectionAttribute"/>.</summary>
        private static readonly Dictionary<string, AssetInjectionOverlay>
            GuidToOverlay = new Dictionary<string, AssetInjectionOverlay>();

        /************************************************************************************************************************/

        /// <summary>
        /// The attributes which are currently targeting the asset GUID this overlay is responsible for.
        /// Usually there will only be one attribute targeting a particular asset, but there's no reason why more
        /// couldn't target the same one.
        /// </summary>
        private readonly List<AssetInjectionAttribute> Attributes = new List<AssetInjectionAttribute>();

        /************************************************************************************************************************/

        protected AssetInjectionOverlay(List<AssetInjectionAttribute> attributes)
        {
            Attributes = attributes;
        }

        /************************************************************************************************************************/

        public static bool TryDoGUI(string guid, Rect selectionRect)
        {
            if (GuidToOverlay.TryGetValue(guid, out var overlay))
            {
                overlay.DoGUI(selectionRect);
                return true;
            }
            else
            {
                return false;
            }
        }

        /************************************************************************************************************************/

        protected void DoGUI(Rect selectionRect)
        {
            var icon = Icon;
            if (icon == null)
                return;

            if (ProjectWindowOverlays.DoGUI(selectionRect, GetTooltip(), icon))
                OpenContextMenu();
        }

        /************************************************************************************************************************/

        protected virtual Texture Icon => Attributes[0].Icon;

        /************************************************************************************************************************/

        private string _Tooltip;

        private string GetTooltip()
        {
            if (_Tooltip == null)
            {
                var tooltip = WeaverUtilities.GetStringBuilder();
                AppendTooltipPrefix(tooltip);

                for (int i = 0; i < Attributes.Count; i++)
                {
                    if (i > 0)
                        tooltip.AppendLineConst().AppendLineConst();

                    var attribute = Attributes[i];
                    if (attribute != null)
                    {
                        tooltip.Append(attribute);

                        if (attribute.Tooltip != null)
                            tooltip.AppendLineConst().AppendLineConst().Append(attribute.Tooltip);
                    }
                }

                _Tooltip = tooltip.ReleaseToString();
            }

            return _Tooltip;
        }

        /************************************************************************************************************************/

        protected virtual void AppendTooltipPrefix(StringBuilder tooltip) { }

        /************************************************************************************************************************/

        private void OpenContextMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Open Weaver Window"), false, () => WeaverWindow.Ping(Attributes));

            for (int i = 0; i < Attributes.Count; i++)
            {
                menu.AddSeparator("");
                Attributes[i].AddItemsToMenu(menu);
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        public static void OnBeforeAssetChanged(AssetInjectionAttribute attribute)
        {
            if (!(attribute.Asset is null))
                AssetToAttribute.Remove(attribute.Asset);
        }

        /************************************************************************************************************************/

        public static void OnAfterAssetChanged(AssetInjectionAttribute attribute)
        {
            var wasRegistered = AttributeToGUID.TryGetValue(attribute, out var guid);

            // Remove the previous asset from its overlay (if it had one).
            if (guid != null)
            {
                if (GuidToOverlay.TryGetValue(guid, out var overlay))
                {
                    overlay.Attributes.Remove(attribute);
                    overlay._Tooltip = null;

                    // And return the overlay to the spare pool if it is now empty.
                    if (overlay.Attributes.Count == 0)
                    {
                        GuidToOverlay.Remove(guid);
                        OverlayPool.Release(overlay);
                    }
                }
            }

            if (attribute.Asset == null)
            {
                if (!wasRegistered || guid != null)
                    PotentialAssetInjectionOverlay.AddAttributeWithoutTarget(attribute);

                guid = null;
            }
            else
            {
                if (wasRegistered && guid == null)
                    PotentialAssetInjectionOverlay.RemoveAttributeWithoutTarget(attribute);

                AssetToAttribute[attribute.Asset] = attribute;

                // Get the asset GUID.
                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(attribute.Asset));

                // Get or create an overlay.
                if (!GuidToOverlay.TryGetValue(guid, out var overlay))
                {
                    overlay = OverlayPool.Acquire();
                    GuidToOverlay.Add(guid, overlay);
                }

                // Add the asset to it.
                overlay._Tooltip = null;
                overlay.Attributes.Add(attribute);
            }

            AttributeToGUID[attribute] = guid;

            EditorApplication.RepaintProjectWindow();
        }

        /************************************************************************************************************************/

        public static AssetInjectionAttribute GetAttribute(Object asset)
        {
            AssetToAttribute.TryGetValue(asset, out var attribute);
            return attribute;
        }

        /************************************************************************************************************************/
    }
}

#endif

