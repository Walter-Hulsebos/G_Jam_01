// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to the <see cref="Procedural.ScenesScriptBuilder"/>.
    /// </summary>
    [Serializable]
    internal sealed class SceneSettings : ProceduralScriptSettings
    {
        /************************************************************************************************************************/

        /// <summary>
        /// If enabled: the generated script will contain a const int field holding the build settings index of each
        /// scene.
        /// </summary>
        public bool includeSceneIndices;

        /// <summary>
        /// If enabled: the generated script will contain a const string field holding the name of each scene.
        /// </summary>
        public bool includeSceneNames;

        /// <summary>
        /// If enabled: fields will be named using the scene's full asset path instead of just the file name.
        /// </summary>
        public bool useFullPathNames;

        /// <summary>
        /// If enabled: the fields for each scene will be grouped inside nested classes corresponding to the
        /// directories in their asset path. Otherwise they will all be located in the root Scenes class.
        /// </summary>
        public bool useNestedClasses;

        /************************************************************************************************************************/

        public override void DoGUI()
        {
            WeaverEditorUtilities.DoToggle(ref includeSceneIndices,
                "Include Scene Index Fields",
                "If enabled: the script will contain a const int field holding the build settings index of each scene.");

            WeaverEditorUtilities.DoToggle(ref includeSceneNames,
                "Include Scene Name Fields",
                "If enabled: the script will contain a const string field holding the name of each scene.");

            WeaverEditorUtilities.DoToggle(ref useFullPathNames,
                "Use Full Path Names",
                "If enabled: fields will be named using the scene's full asset path instead of just the file name.");

            WeaverEditorUtilities.DoToggle(ref useNestedClasses,
                "Use Nested Classes",
                "If enabled: the fields for each scene will be grouped inside nested classes" +
                " corresponding to the directories in their asset path." +
                " Otherwise they will all be located in the root Scenes class.");
        }

        /************************************************************************************************************************/
    }
}

#endif

