// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// An object that can be used for dependency injection.
    /// </summary>
    internal interface IInjector
    {
        /************************************************************************************************************************/

        /// <summary>Indicates whether this injector should be excluded from runtime builds.</summary>
        bool EditorOnly { get; }

        /// <summary>
        /// Indicates whether injection should be performed in Edit Mode, otherwise it will only be performed in play
        /// mode and on startup in a build.
        /// </summary>
        bool InEditMode { get; }

        /// <summary>Executes the dependency injection.</summary>
        void Inject();

        /// <summary>[Pro-Only]
        /// Called during a build by <see cref="UnityEditor.Build.IPreprocessBuildWithReport.OnPreprocessBuild"/>.
        /// </summary>
        void OnStartBuild();

        /// <summary>[Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        void GatherInjectorDetails(InjectorScriptBuilder builder);

        /// <summary>[Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the values this attribute
        /// wants assigned it.
        /// </summary>
        void SetupInjectorValues(InjectorScriptBuilder builder);

        /************************************************************************************************************************/
    }

    public partial class WeaverEditorUtilities
    {
        /************************************************************************************************************************/

        /// <summary>[Editor-Only, Internal]
        /// Indicates whether injection is allowed for this injector based on the current state of the Unity Editor.
        /// </summary>
        internal static bool CanInject(this IInjector injector)
        {
            if (WeaverEditorUtilities.IsBuilding)
                return !injector.EditorOnly;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return true;

            if (injector.InEditMode)
                return true;

            return false;
        }

        /************************************************************************************************************************/
    }
}

#endif

