// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ProceduralScriptPanel"/> containing the details of the procedural Tags script.
    /// </summary>
    public sealed class TagsPanel : ProceduralScriptPanel
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override string Name => "Tag Constants";

        /// <inheritdoc/>
        protected override ProceduralScriptSettings Settings => WeaverSettings.Tags;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void PopulateHeaderContextMenu(GenericMenu menu)
        {
            WeaverEditorUtilities.AddLinkToURL(menu, "Help: Tag Constants", "/docs/project-constants/tags");
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
            GUILayout.Label("Tags can be edited by selecting a GameObject" +
                " then opening its Tag dropdown menu at the top of the Inspector" +
                " and using the 'Add Tag...' function.", EditorStyles.wordWrappedLabel);
            DoInjectorListGUI();
        }

        /************************************************************************************************************************/
    }
}

#endif

