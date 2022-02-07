// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only]
    /// A collapsible panel in the <see cref="WeaverWindow"/>.
    /// </summary>
    public abstract class WeaverWindowPanel
    {
        /************************************************************************************************************************/

        /// <summary>
        /// All the <see cref="InjectionAttribute"/>s have been classified for this panel.
        /// </summary>
        public readonly List<InjectionAttribute> Injectors = new List<InjectionAttribute>();

        /************************************************************************************************************************/

        private readonly AnimBool ExpansionAnimator = new AnimBool();

        private int _Index;

        /// <summary>The display name of this panel.</summary>
        protected abstract string Name { get; }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines whether this panel is currently expanded by comparing the index specified in
        /// <see cref="Initialize"/> with <see cref="WeaverWindowSettings.currentPanel"/>.
        /// </summary>
        public bool IsExpanded
        {
            get { return WeaverSettings.Window.currentPanel == _Index; }
            set
            {
                if (value)
                    WeaverSettings.Window.currentPanel = _Index;
                else if (IsExpanded)
                    WeaverSettings.Window.currentPanel = -1;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sets up the initial state of this panel.</summary>
        public virtual void Initialize(int index)
        {
            _Index = index;
            ExpansionAnimator.value = ExpansionAnimator.target = IsExpanded;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the GUI for this panel.
        /// </summary>
        public virtual void DoGUI()
        {
            var enabled = GUI.enabled;

            GUILayout.BeginVertical(EditorStyles.helpBox);

            DoHeaderGUI();

            ExpansionAnimator.target = IsExpanded;

            if (EditorGUILayout.BeginFadeGroup(ExpansionAnimator.faded))
            {
                DoBodyGUI();
            }
            EditorGUILayout.EndFadeGroup();

            GUILayout.EndVertical();

            if (ExpansionAnimator.isAnimating)
                WeaverWindow.Instance.Repaint();

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Header GUI for this panel which is displayed regardless of whether it is expanded or not.
        /// </summary>
        public virtual void DoHeaderGUI()
        {
            if (GUILayout.Button(Name, EditorStyles.boldLabel))
            {
                if (CheckHeaderContextMenu())
                    return;

                IsExpanded = !IsExpanded;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Opens a context menu if the current event is a Right Click.</summary>
        protected bool CheckHeaderContextMenu()
        {
            if (Event.current.button != 1)
                return false;

            var menu = new GenericMenu();
            PopulateHeaderContextMenu(menu);
            menu.ShowAsContext();

            return true;
        }

        /// <summary>Adds functions to the header context menu.</summary>
        protected virtual void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help", "/");
        }

        /************************************************************************************************************************/

        /// <summary>Draws the Body GUI for this panel which is only displayed while it is expanded.</summary>
        public abstract void DoBodyGUI();

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="WeaverWindow"/>.OnDisable().
        /// </summary>
        public virtual void OnDisable() { }

        /************************************************************************************************************************/

        /// <summary>The number of <see cref="InjectionAttribute"/>s that are shown in this panel.</summary>
        public int VisibleInjectorCount
        {
            get
            {
                var count = 0;
                for (int i = 0; i < Injectors.Count; i++)
                {
                    var injector = Injectors[i];
                    if (injector.ShouldShow)
                        count++;
                }

                return count;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws each element in the <see cref="Injectors"/> list.</summary>
        public void DoInjectorListGUI()
        {
            for (int i = 0; i < Injectors.Count; i++)
                Injectors[i].DoInspectorGUI();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the <see cref="Injectors"/> list, grouped by namespaces.
        /// </summary>
        public void DoGroupedInjectorListGUI()
        {
            const float LabelWidthOffset = 4;
            EditorGUIUtility.labelWidth -= LabelWidthOffset;

            var previousGroup = ".";
            var isInsideGroup = false;

            for (int i = 0; i < Injectors.Count; i++)
            {
                var injector = Injectors[i];
                if (!injector.ShouldShow)
                    continue;

                var currentGroup = injector.Member.DeclaringType.Namespace;
                if (previousGroup != currentGroup)
                {
                    if (isInsideGroup)
                        GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUI.skin.box);
                    isInsideGroup = true;

                    previousGroup = currentGroup;
                    if (currentGroup != null)
                        EditorGUILayout.LabelField(currentGroup);
                }

                injector.DoInspectorGUI();
            }

            if (isInsideGroup)
                GUILayout.EndVertical();

            EditorGUIUtility.labelWidth += LabelWidthOffset;
        }

        /************************************************************************************************************************/
    }
}

#endif

