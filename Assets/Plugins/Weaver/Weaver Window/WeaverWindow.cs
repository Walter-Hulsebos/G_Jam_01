// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// The main <see cref="EditorWindow"/> which provides access to <see cref="Weaver"/>'s features and settings.
    /// </summary>
    internal sealed class WeaverWindow : EditorWindow
    {
        /************************************************************************************************************************/

        /// <summary>
        /// An object that can be shown in the <see cref="WeaverWindow"/>.
        /// </summary>
        public interface IItem
        {
            /// <summary>
            /// Returns the type of the main <see cref="WeaverWindowPanel"/> that this object should be shown in and
            /// optionally a secondary panel where it will also be shown. When pinging the object, it will be
            /// highlighted in the main panel.
            /// </summary>
            Type GetPanelType(out Type secondaryPanel);
        }

        /************************************************************************************************************************/

        public static WeaverWindow Instance { get; private set; }

        [NonSerialized]
        private WeaverWindowPanel[] _Panels;

        [SerializeField]
        private Vector2 _ScrollPosition;
        public Vector2 ScrollPosition => _ScrollPosition;

        /************************************************************************************************************************/

        private void OnEnable()
        {
            Instance = this;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            // If initialisation fails, draw some things that might be helpful for fixing the issue.
            if (!InitializePanels())
            {
                DoInitialisationFailedGUI();
                return;
            }

            // Take scroll wheel events.
            if (Event.current.type == EventType.ScrollWheel)
            {
                _ScrollPosition += Event.current.delta * 6;
                Repaint();
                GUIUtility.ExitGUI();
            }

            // Draw all the panels inside a scroll view.
            _ScrollPosition = EditorGUILayout.BeginScrollView(_ScrollPosition);
            {
                EditorGUI.BeginChangeCheck();

                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = position.width * 0.5f;

                for (int i = 0; i < _Panels.Length; i++)
                {
                    _Panels[i].DoGUI();
                }

                EditorGUIUtility.labelWidth = labelWidth;

                if (EditorGUI.EndChangeCheck())
                    WeaverSettings.SetDirty();
            }
            EditorGUILayout.EndScrollView();

            // If the WeaverSettings object hasn't been saved as an asset, save it and repaint this window.
            if (WeaverSettings.EnsureInstanceIsSaved())
                Repaint();
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private bool _HasInitializedPanels;

        private bool InitializePanels()
        {
            if (_HasInitializedPanels)
                return _Panels != null;

            try
            {
                _HasInitializedPanels = true;

                _Panels = new WeaverWindowPanel[]
                {
                    new WelcomePanel(),
                    new InjectionPanel(),
                    new ProceduralPanel(),
                    new AssetListsPanel(),
                    new AnimationsPanel(),
                    new LayersPanel(),
                    new NavAreasPanel(),
                    new ScenesPanel(),
                    new ShadersPanel(),
                    new TagsPanel(),
                    new MiscPanel(),
                };

                CategoriseInjectors();

                for (int i = 0; i < _Panels.Length; i++)
                    _Panels[i].Initialize(i);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _Panels = null;
                return false;
            }
        }

        /************************************************************************************************************************/

        [MenuItem(WeaverUtilities.WeaverWindowPath)]
        public static WeaverWindow OpenWindow()
        {
            var autoDock = WeaverSettings.Window.autoDock;

            if (!string.IsNullOrEmpty(autoDock))
            {
                var dockWith = typeof(EditorWindow).Assembly.GetType(autoDock);
                if (dockWith != null)
                {
                    return GetWindow<WeaverWindow>(nameof(Weaver), dockWith);
                }
            }

            return GetWindow<WeaverWindow>(nameof(Weaver));
        }

        /************************************************************************************************************************/

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            if (_Panels != null)
            {
                for (int i = 0; i < _Panels.Length; i++)
                    _Panels[i].OnDisable();

                WeaverSettings.SetDirty();
            }
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private bool _HasCategorisedInjectors;

        private void CategoriseInjectors()
        {
            if (_HasCategorisedInjectors)
                return;

            _HasCategorisedInjectors = true;

            for (int i = 0; i < InjectorManager.AllInjectionAttributes.Count; i++)
            {
                var injector = InjectorManager.AllInjectionAttributes[i];
                var panelIndex = GetPanelIndex(injector, false, out var secondaryPanel);
                if (panelIndex >= 0)
                {
                    _Panels[panelIndex].Injectors.Add(injector);

                    panelIndex = GetPanelIndex(secondaryPanel);
                    if (panelIndex >= 0)
                        _Panels[panelIndex].Injectors.Add(injector);
                }
            }
        }

        /************************************************************************************************************************/

        private int GetPanelIndex(Type panelType)
        {
            InitializePanels();
            for (int i = 0; i < _Panels.Length; i++)
            {
                if (panelType == _Panels[i].GetType())
                    return i;
            }

            return -1;
        }

        /************************************************************************************************************************/

        private int GetPanelIndex(IItem pingable, bool isPing, out Type secondaryPanel)
        {
            var panelType = pingable.GetPanelType(out secondaryPanel);

            var index = GetPanelIndex(panelType);
            if (index >= 0)
                return index;

            Debug.LogWarning(nameof(ShowInPanelAttribute) + " specifies an invalid panel type for " + pingable);
            return -1;
        }

        /************************************************************************************************************************/

        private void DoInitialisationFailedGUI()
        {
            GUILayout.Label(WeaverUtilities.Version, WelcomePanel.HeadingStyle);

            EditorGUILayout.HelpBox(InitialisationFailedMessage, MessageType.Error);

            WelcomePanel.DoSupportGroup();

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (GUILayout.Button("Reload Assemblies"))
                EditorUtility.RequestScriptReload();

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (MiscPanel.DoDeleteWeaverButton())
            {
                if (!EditorUtility.DisplayDialog("Closing Unity",
                    "Weaver has been successfully deleted." +
                    "\n\nUnity will now be closed to ensure that Weaver is removed entirely." +
                    "\n\nYou can then restart Unity and import Weaver again. " +
                    " You might also need to fix compile errors in your own scripts as a result of API changes in Weaver.",
                    "Close Unity", "Cancel"))
                    return;

                EditorApplication.ExecuteMenuItem("Window/Asset Store");
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        /************************************************************************************************************************/

        private static string _InitialisationFailedMessage;

        private static string InitialisationFailedMessage
        {
            get
            {
                if (_InitialisationFailedMessage == null)
                {
                    _InitialisationFailedMessage = "Initialisation Failed. Check the Console for errors.";

                    // Check if this plugin has been imported over a previous version, because it will cause errors.

                    // The search filter ignores file extensions, so "Weaver.*" won't detect Weaver.dll but will still pick up
                    // all the old dlls (Weaver.AssetLinker.dll, etc).
                    var guids = AssetDatabase.FindAssets("Weaver.*", new string[] { WeaverEditorUtilities.WeaverPluginsDirectory });
                    WeaverEditorUtilities.GUIDsToAssetPaths(guids);
                    for (int i = 0; i < guids.Length; i++)
                    {
                        if (guids[i].EndsWith(".dll"))
                        {
                            _InitialisationFailedMessage = "Initialisation Failed." +
                                " Weaver has detected that it was imported over the top of a previous version which will cause errors." +
                                "\n\n 1. Use the button below to Delete Weaver." +
                                "\n 2. Restart Unity." +
                                "\n 3. Import Weaver again.";
                            break;
                        }
                    }
                }

                return _InitialisationFailedMessage;
            }
        }

        /************************************************************************************************************************/
        #region Auto Focus
        /************************************************************************************************************************/

        [NonSerialized]
        private bool _IsVisible;

        [SerializeField]
        private bool _AutoFocussedInspector;

        /************************************************************************************************************************/

        private void OnBecameVisible()
        {
            _IsVisible = true;
            _AutoFocussedInspector = false;
        }

        private void OnBecameInvisible()
        {
            _IsVisible = false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Deletage registered to <see cref="Selection.selectionChanged"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="EditorWindow"/> actually has an OnSelectionChange event they receive from Unity, but it only
        /// gets called while the window is visible and we need it to get called while hidden to return focus back to
        /// this window so we use Selection.selectionChanged instead.
        /// </remarks>
        private void OnSelectionChanged()
        {
            if (!WeaverSettings.Window.autoFocus)
                return;

            if (Selection.objects.Length > 0)
            {
                if (_IsVisible)
                {
                    ShowInspectorWindow();
                }
            }
            else if (_AutoFocussedInspector)
            {
                // When deselecting an object, return focus to this window.
                EditorApplication.delayCall += () =>
                {
                    // Don't take focus from a text field.
                    if (!EditorGUIUtility.editingTextField)
                    {
                        _AutoFocussedInspector = false;
                        Focus();
                    }
                };
            }
        }

        /************************************************************************************************************************/

        private static Type _InspectorWindowType;

        public static void ShowInspectorWindow()
        {
            if (_InspectorWindowType == null)
            {
                _InspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
                if (_InspectorWindowType == null)
                    return;
            }

            if (Instance != null)
                Instance._AutoFocussedInspector = true;

            var focusedWindow = EditorWindow.focusedWindow;
            FocusWindowIfItsOpen(_InspectorWindowType);
            if (!(focusedWindow is WeaverWindow))
                focusedWindow.Focus();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Ping Injector
        /************************************************************************************************************************/

        private static HashSet<IItem> _PingTargets;
        private static double _PingTime;
        private static GUIStyle _PingStyle;

        /************************************************************************************************************************/

        public static void Ping<T>(List<T> targets) where T : IItem
        {
            if (_PingTargets == null)
                WeaverUtilities.GetHashSet(out _PingTargets);
            else
                _PingTargets.Clear();

            for (int i = 0; i < targets.Count; i++)
                _PingTargets.Add(targets[i]);

            _PingTime = EditorApplication.timeSinceStartup;

            GUIUtility.hotControl = GUIUtility.keyboardControl = 0;

            var window = OpenWindow();
            var weaverWindowPanel = window.GetPanelIndex(targets[0], true, out _);
            if (weaverWindowPanel >= 0)
                WeaverSettings.Window.currentPanel = weaverWindowPanel;
        }

        /************************************************************************************************************************/

        public void SelectPanel(Type panelType)
        {
            var weaverWindowPanel = GetPanelIndex(panelType);
            if (weaverWindowPanel >= 0)
                WeaverSettings.Window.currentPanel = weaverWindowPanel;
        }

        /************************************************************************************************************************/

        public static void DoPingGUI(Rect rect, IItem target)
        {
            if (_PingTargets == null ||
                !_PingTargets.Contains(target))
                return;

            const double Duration = 3;
            var remainingTime = _PingTime + Duration - EditorApplication.timeSinceStartup;
            if (remainingTime < 0)
            {
                WeaverUtilities.Release(ref _PingTargets);
                return;
            }

            const double GrowTime = 0.25f;
            if (remainingTime > Duration - GrowTime)
            {
                var grow = (float)((remainingTime - (Duration - GrowTime)) / GrowTime);
                grow = Mathf.Sin(grow * Mathf.PI);
                grow *= 5;
                rect.x -= grow;
                rect.y -= grow;
                rect.width += grow * 2;
                rect.height += grow * 2;
            }

            var color = GUI.color;
            if (remainingTime < 1)
                GUI.color = new Color(color.r, color.g, color.b, (float)remainingTime);

            rect.height -= 1;
            GUI.Box(rect, "", _PingStyle ?? (_PingStyle = new GUIStyle("PR Ping")));
            WeaverWindow.Instance.Repaint();

            GUI.color = color;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

