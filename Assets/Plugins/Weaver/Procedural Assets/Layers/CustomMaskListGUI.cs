// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A GUI for a list of <see cref="CustomLayerMask"/>.
    /// </summary>
    internal sealed class CustomMaskListGUI<T> : ReorderableList where T : CustomLayerMask, new()
    {
        /************************************************************************************************************************/

        private const float
            LineHeight = 16,
            NameAreaRight = 142,
            MinCommentSize = 60;

        private static readonly GUILayoutOption[]
            MinWidth = new GUILayoutOption[1];

        private readonly LayerManager LayerManager;
        private readonly string Label;
        private readonly GUIContent[] ShortcutButtonContents;

        private float[] _ShortcutButtonWidths;
        private float _HeaderHeight;
        private T _DummyMask;

        /************************************************************************************************************************/

        public CustomMaskListGUI(LayerManager manager, string label, params GUIContent[] shortcutButtonContents)
            : base(manager.Settings.CustomMasks, typeof(T))
        {
            LayerManager = manager;
            Label = label;
            ShortcutButtonContents = shortcutButtonContents;

            headerHeight = 0;
            elementHeight = LineHeight;
            footerHeight = LineHeight;
            drawElementCallback = DoLayerMask;
            drawFooterCallback = DoFooter;

            _DummyMask = new T();

            manager.OnLayersChanged += OnLayersChanged;
            manager.UpdateLayerNames();
        }

        /************************************************************************************************************************/

        public void DoGUI()
        {

            LayerManager.UpdateLayerNames();

            // Header.
            DrawHeader();

            // Mask List.
            this.DoLayoutListFixed(MinWidth);

            // Settings.
            LayerManager.Settings.DoGUI();

            // Script.
            LayerManager.ScriptBuilder.ProceduralAsset.DoGUI();
        }

        /************************************************************************************************************************/

        private void DrawHeader()
        {
            // Background.
            var headerRect = DrawHeaderBackground();

            // Label.
            var area = headerRect;
            area.height = LineHeight;
            GUI.Label(area, Label);

            // Shortcut Buttons.
            area.y += area.height;
            DoShortcutButtons(area);

            // Mask Name Headding.
            area.x = headerRect.x + 40;
            area.y = headerRect.y + _HeaderHeight - 17;
            area.xMax = NameAreaRight;
            area.height = LineHeight;
            GUI.Label(area, "Mask Name");

            // Comment Headding.
            var toggleAreaWidth = (1 + LayerManager.OldLayerNames.Length) * LineHeight;
            area.x = headerRect.x + NameAreaRight + toggleAreaWidth;
            GUI.Label(area, "Comment");

            // Rotate the GUI matrix.

            var roundedScroll = new Vector2(
                Mathf.Round(WeaverWindow.Instance.ScrollPosition.x),
                Mathf.Round(WeaverWindow.Instance.ScrollPosition.y));

            area.x = headerRect.x + NameAreaRight;
            area.y = headerRect.y;
            area.height = headerRect.y + _HeaderHeight;
            area.width = toggleAreaWidth;
            GUI.BeginClip(area);

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-90, Vector2.zero);
            GUI.matrix *= Matrix4x4.Translate(new Vector3(-_HeaderHeight, -roundedScroll.y, 0));

            area.x = 2;
            area.y = roundedScroll.y;
            area.width = _HeaderHeight;
            area.height = LineHeight;

            // Layer Headings.

            GUI.Label(area, "All");

            for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
            {
                GUI.matrix *= Matrix4x4.Translate(new Vector3(0, LineHeight, 0));
                GUI.Label(area, LayerManager.OldLayerNames[i]);
            }

            GUI.matrix = matrix;

            GUI.EndClip();
        }

        /************************************************************************************************************************/

        private Rect DrawHeaderBackground()
        {
            RecalculateHeaderHeight();

            const float Padding = 2;
            var headerRect = EditorGUILayout.GetControlRect(false, _HeaderHeight - Padding);
            headerRect.height += Padding;

            if (Event.current.type == EventType.Repaint)
            {
                var width = 143 + (1 + LayerManager.OldLayerNames.Length) * LineHeight + MinCommentSize;
                if (width < headerRect.width)
                    width = headerRect.width;

                var rect = headerRect;
                rect.width = width;

                GUIStyles.HeaderBackgroundStyle.Draw(rect, false, false, false, false);
            }

            return headerRect;
        }

        /************************************************************************************************************************/

        private void OnLayersChanged(string[] oldLayers, string[] newLayers)
        {
            // Recalculate header height next time it draws.
            _HeaderHeight = 0;
        }

        private void RecalculateHeaderHeight()
        {
            if (_HeaderHeight > 0)
                return;

            // Width of the longest default layer (Ignore Raycast).
            var label = WeaverEditorUtilities.TempContent("Ignore Raycast");
            GUI.skin.label.CalcMinMaxWidth(label, out var _, out _HeaderHeight);

            for (int i = LayerManager.DefaultLayerCount; i < LayerManager.OldLayerNames.Length; i++)
            {
                label.text = LayerManager.OldLayerNames[i];

                GUI.skin.label.CalcMinMaxWidth(label, out _, out var width);

                if (_HeaderHeight < width)
                    _HeaderHeight = width;
            }

            _HeaderHeight += 2;

            MinWidth[0] = GUILayout.MinWidth(NameAreaRight + (1 + LayerManager.OldLayerNames.Length) * LineHeight + MinCommentSize);
        }

        /************************************************************************************************************************/

        private void DoShortcutButtons(Rect rect)
        {
            var enabled = GUI.enabled;
            GUI.enabled = true;

            if (_ShortcutButtonWidths == null)
            {
                _ShortcutButtonWidths = new float[ShortcutButtonContents.Length];
                for (int i = 0; i < _ShortcutButtonWidths.Length; i++)
                {
                    GUI.skin.button.CalcMinMaxWidth(ShortcutButtonContents[i], out var minWidth, out _);
                    _ShortcutButtonWidths[i] = minWidth;
                }
            }

            rect.x += 3;
            rect.y += 1;
            rect.height = 18;

            for (int i = 0; i < ShortcutButtonContents.Length; i++)
            {
                var content = ShortcutButtonContents[i];
                rect.width = _ShortcutButtonWidths[i];

                if (GUI.Button(rect, content))
                {
                    EditorApplication.ExecuteMenuItem(content.tooltip);
                }

                rect.y += rect.height + 2;
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DoLayerMask(Rect rect, int index, bool isActive, bool isFocused)
        {
            var mask = LayerManager.Settings.GetMask(index);
            DoLayerMask(mask, rect, index, isActive);
        }

        private void DoLayerMask(CustomLayerMask mask, Rect rect, int index, bool isActive)
        {
            var xMax = rect.xMax;

            // Remove.
            rect.y -= 1; rect.width = GUIStyles.RemoveButtonWidth;
            if (index >= 0)
                DoRemoveLayerButton(rect, index);
            rect.y += 1;

            // Name.
            var color = GUI.color;
            if (!mask.ValidateName() && !mask.IsUnusedDummy)
                GUI.color = WeaverEditorUtilities.ErrorColor;

            rect.x = rect.xMax;
            rect.width = 100;
            mask.name = EditorGUI.TextField(rect, mask.name);

            rect.x += rect.width + 2;
            rect.width = LineHeight;

            EditorGUI.BeginChangeCheck();

            // Toggle All Layers.
            var wasActive = mask.HasAllLayers();
            isActive = GUI.Toggle(rect, wasActive, "");
            if (isActive != wasActive)
            {
                mask.SetAllLayers(isActive);
            }
            rect.x += rect.width;

            // Individual Layer Toggles.
            for (int i = 0; i < LayerManager.OldLayerNames.Length; i++)
            {
                wasActive = mask.HasBit(i);
                isActive = GUI.Toggle(rect, wasActive, "");
                if (isActive != wasActive)
                {
                    if (isActive)
                        mask.AddBit(i);
                    else
                        mask.RemoveBit(i);
                }

                rect.x += rect.width;
            }

            if (EditorGUI.EndChangeCheck())
                GUIUtility.keyboardControl = 0;

            // Comment.
            var right = xMax + 2;
            if (rect.x < right)
            {
                rect.width = MinCommentSize;
                if (rect.xMax < right)
                    rect.xMax = right;

                mask.comment = EditorGUI.TextField(rect, mask.comment);
            }

            GUI.color = color;
        }

        /************************************************************************************************************************/

        private void DoRemoveLayerButton(Rect position, int index)
        {
            var content = GUIStyles.GetTempRemoveButton("Remove this Mask");
            if (GUI.Button(position, content, GUIStyles.RemoveButtonStyle))
            {
                GUIUtility.keyboardControl = 0;
                EditorApplication.delayCall += delegate
                {
                    LayerManager.Settings.CustomMasks.RemoveAt(index);
                    WeaverWindow.Instance.Repaint();
                };
                return;
            }
        }

        /************************************************************************************************************************/

        private void DoFooter(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyles.FooterBackgroundStyle.Draw(rect, false, false, false, false);
            }

            rect.y -= 2;
            rect.xMin += 20;
            rect.width -= 6;

            DoLayerMask(_DummyMask, rect, -1, false);

            if (string.IsNullOrEmpty(_DummyMask.name))
            {
                var enabled = GUI.enabled;
                GUI.enabled = false;

                rect.x += 21;
                GUI.Label(rect, "New Mask");

                GUI.enabled = enabled;
            }

            if ((!string.IsNullOrEmpty(_DummyMask.name) ||
                !string.IsNullOrEmpty(_DummyMask.comment)) &&
                !EditorGUIUtility.editingTextField)
            {
                LayerManager.Settings.CustomMasks.Add(_DummyMask);
                _DummyMask = new T();
            }
        }

        /************************************************************************************************************************/

        public void OnDisable()
        {
            LayerManager.OnLayersChanged -= OnLayersChanged;
        }

        /************************************************************************************************************************/
    }
}

#endif

