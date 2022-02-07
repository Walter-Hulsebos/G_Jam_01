// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// In Edit Mode we inject everything using an [<see cref="InitializeOnLoadMethodAttribute"/>].
    /// <para></para>
    /// In Play Mode we inject everything using [<see cref="RuntimeInitializeOnLoadMethodAttribute"/>] so it can be applied
    /// before the scene is loaded.
    /// <para></para>
    /// During the build process we generate a procedural script and attach it to an object in the first scene so that
    /// it can inject everything efficiently on startup.
    /// </summary>
    internal static class Startup
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Called automatically by Unity whenever it reloads assemblies. Executes all
        /// <see cref="InjectionAttribute"/>s to apply their values to their attributed members.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnLoadInEditor()
        {
            // If there is no existing settings asset, then we won't have anything to inject immediately.
            // But we still need to initialize it, so just wait a frame.

            // This is generally caused when first importing Weaver. For whatever reason Unity invokes this method
            // before the AssetDatabase is actually ready to load the settings asset.

            if (WeaverSettings.GetExistingInstance() == null)
            {
                EditorApplication.delayCall += () =>
                {
                    Initialize();
                    Window.WeaverWindow.OpenWindow();
                };

                return;
            }

            // Otherwise initialize now.

            Initialize();

            void Initialize()
            {
                EditorApplication.playModeStateChanged += (change) =>
                {
                    if (change == PlayModeStateChange.ExitingEditMode)
                        ProceduralAsset.CheckForMissingAssets();
                };

                // When first entering Play Mode, the [RuntimeInitializeOnLoadMethod] will call InjectAll.
                // But in Edit Mode or if the assemblies are reloaded during Play Mode, we need to call it here.
                if (!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                {
                    InjectAll();
                }

                ProjectWindowOverlays.OnShowSettingChanged();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called automatically by Unity when entering Play Mode. Executes all <see cref="InjectionAttribute"/>s to
        /// apply their values to their attributed members.
        /// <para></para>
        /// This would normally be called on startup in a runtime build as well, but runtime initialisation is handled
        /// by the procedural <see cref="InjectorScriptBuilder.Script"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InjectAll()
        {
            foreach (var injector in InjectorManager.AllInjectors)
                injector.Inject();
        }

        /************************************************************************************************************************/
    }
}

#endif

