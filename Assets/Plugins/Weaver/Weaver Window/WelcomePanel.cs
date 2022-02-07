// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="WeaverWindowPanel"/> containing links to support and the examples for Weaver.
    /// </summary>
    internal sealed class WelcomePanel : WeaverWindowPanel
    {
        /************************************************************************************************************************/

        private static GUIStyle _HeadingStyle;

        public static GUIStyle HeadingStyle
        {
            get
            {
                if (_HeadingStyle == null)
                {
                    var height = EditorGUIUtility.singleLineHeight * 1.5f;

                    _HeadingStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = (int)height,
                        fontStyle = FontStyle.Bold,
                    };

                    _HeadingStyle.fixedHeight = height + _HeadingStyle.padding.vertical + 2;
                }

                return _HeadingStyle;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The display name of this panel.</summary>
        protected override string Name => "Welcome";

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the Header GUI for this panel which is displayed regardless of whether it is expanded or not.
        /// </summary>
        public override void DoHeaderGUI()
        {
            if (GUILayout.Button(WeaverUtilities.Version, HeadingStyle))
            {
                if (CheckHeaderContextMenu())
                    return;

                IsExpanded = !IsExpanded;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the Body GUI for this panel which is only displayed while it is expanded.</summary>
        public override void DoBodyGUI()
        {
            DoWelcomeGroup();
            DoExamplesGroup();
            DoSupportGroup();
            DoFeedbackGroup();
        }

        /************************************************************************************************************************/

        public static void DoWelcomeGroup()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Welcome to Weaver", EditorStyles.largeLabel);
                GUILayout.Label(
                    $"This window can be opened via '{WeaverUtilities.WeaverWindowPath}'" +
                    " and is used to manage Weaver's features and settings.",
                    EditorStyles.wordWrappedLabel);
            }
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        private static DefaultAsset _ExamplesFolder;

        private static void DoExamplesGroup()
        {
            if (_ExamplesFolder == null)
            {
                _ExamplesFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    WeaverEditorUtilities.WeaverPluginsDirectory + "/Examples");
                if (_ExamplesFolder == null)
                    return;
            }

            var examplesFolderPath = AssetDatabase.GetAssetPath(_ExamplesFolder);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Example Scenes", EditorStyles.largeLabel);
            GUILayout.Label(examplesFolderPath);
            EditorGUILayout.ObjectField(_ExamplesFolder, typeof(DefaultAsset), false);

            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        public static void DoSupportGroup()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Support", EditorStyles.largeLabel);

                var label = WeaverEditorUtilities.TempContent("Documentation", WeaverUtilities.DocumentationURL);
                if (GUILayout.Button(label, WeaverEditorUtilities.DontExpandWidth))
                    Application.OpenURL(WeaverUtilities.DocumentationURL);

                label = WeaverEditorUtilities.TempContent("Forum", WeaverUtilities.ForumURL);
                if (GUILayout.Button(label, WeaverEditorUtilities.DontExpandWidth))
                    Application.OpenURL(WeaverUtilities.DocumentationURL);

                if (GUILayout.Button("Email: " + WeaverUtilities.DeveloperEmail, WeaverEditorUtilities.DontExpandWidth))
                {
                    EditorGUIUtility.systemCopyBuffer = WeaverUtilities.DeveloperEmail;
                    Debug.Log($"Copied '{WeaverUtilities.DeveloperEmail}' to the clipboard.");
                }

                GUILayout.Label("You can also right click on the header of any panel to go directly to its documentation.", EditorStyles.wordWrappedLabel);
            }
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        public static void DoFeedbackGroup()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Feedback", EditorStyles.largeLabel);

                GUILayout.Label("Honest reviews on the Asset Store are appreciated, but please read the documentation and" +
                    " post in the forum or contact the support email address shown above" +
                    " before posting feature requests, issues, or questions in a review.", EditorStyles.wordWrappedLabel);

                if (GUILayout.Button("Asset Store: Weaver " + (WeaverUtilities.IsWeaverPro ? "Pro" : "Lite"), WeaverEditorUtilities.DontExpandWidth))
                    WeaverUtilities.OpenCurrentVersionInAssetStore();
            }
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/
    }
}

#endif

