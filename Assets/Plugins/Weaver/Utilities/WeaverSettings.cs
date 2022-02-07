// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ScriptableObject"/> which stores various data used by <see cref="Weaver"/>.
    /// </summary>
    internal sealed class WeaverSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        /************************************************************************************************************************/

        [SerializeField]
        private InjectionSettings _Injection;
        internal static InjectionSettings Injection => Instance._Injection;

        [SerializeField]
        private ProceduralAssetSettings _ProceduralAssets;
        internal static ProceduralAssetSettings ProceduralAssets => Instance._ProceduralAssets;

        [SerializeField]
        private AnimationSettings _Animations;
        internal static AnimationSettings Animations => Instance._Animations;

        [SerializeField]
        private LayerSettings _Layers;
        internal static LayerSettings Layers => Instance._Layers;

        [SerializeField]
        private NavAreaSettings _NavAreas;
        internal static NavAreaSettings NavAreas => Instance._NavAreas;

        [SerializeField]
        private SceneSettings _Scenes;
        internal static SceneSettings Scenes => Instance._Scenes;

        [SerializeField]
        private ShaderSettings _Shaders;
        internal static ShaderSettings Shaders => Instance._Shaders;

        [SerializeField]
        private TagSettings _Tags;
        internal static TagSettings Tags => Instance._Tags;

        [SerializeField]
        private WeaverWindowSettings _Window;
        internal static WeaverWindowSettings Window => Instance._Window;

        /************************************************************************************************************************/

        private static WeaverSettings _Instance;

        internal bool IsSaved { get; private set; }

        /************************************************************************************************************************/

        private const string Name = "Weaver Settings";

        internal static string DefaultPath => $"{WeaverEditorUtilities.WeaverPluginsDirectory}/{Name}.asset";

        /************************************************************************************************************************/

        /// <summary>
        /// Searches for a <see cref="WeaverSettings"/> asset in the project, caches it, and returns it.
        /// </summary>
        internal static WeaverSettings Instance
        {
            get
            {
                if (_Instance != null)
                    return _Instance;

                _Instance = GetExistingInstance();
                if (_Instance != null)
                    return _Instance;

                // Otherwise create a new one.
                _Instance = CreateInstance<WeaverSettings>();
                _Instance.name = Name;
                _Instance.hideFlags = HideFlags.DontSave;

                // OnEnable will open the WeaverWindow to ensure the instance is saved when it first draws its GUI to
                // ensure that it doesn't happen during the startup process. Otherwise it would fail to load the
                // settings during startup, create another one, and save it immediately even though the old one still
                // exists and would be loaded perfectly fine at the end of the startup process.

                return _Instance;
            }
        }

        /************************************************************************************************************************/

        internal static WeaverSettings GetExistingInstance()
        {
            if (_Instance != null)
                return _Instance;

            // Try to find an existing settings asset.
            // Prioritise ones that aren't the default.
            _Instance = WeaverEditorUtilities.FindAsset<WeaverSettings>();
            if (_Instance != null)
            {
                _Instance.IsSaved = true;
                return _Instance;
            }

            // Try to find an existing un-saved settings object.
            _Instance = FindObjectOfType<WeaverSettings>();

            return _Instance;
        }

        /************************************************************************************************************************/

        internal static bool EnsureInstanceIsSaved()
        {
            if (_Instance == null ||
                _Instance.IsSaved ||
                EditorApplication.isCompiling)
                return false;

            _Instance.IsSaved = true;

            if (AssetDatabase.Contains(_Instance))
                return false;

            var path = DefaultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            _Instance.hideFlags = HideFlags.None;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(_Instance, path);
            Debug.Log($"Created {Name} at {path}", _Instance);

            return true;
        }

        /************************************************************************************************************************/

        private void OnEnable()
        {
            if (_Instance == null)
                _Instance = this;

            WeaverUtilities.EnsureExists(ref _Injection);
            WeaverUtilities.EnsureExists(ref _ProceduralAssets);
            if (_Animations == null) _Animations = new AnimationSettings();
            WeaverUtilities.EnsureExists(ref _Layers);
            WeaverUtilities.EnsureExists(ref _NavAreas);
            if (_Scenes == null) _Scenes = new SceneSettings();
            WeaverUtilities.EnsureExists(ref _Shaders);
            if (_Tags == null) _Tags = new TagSettings();
            if (_Window == null) _Window = new WeaverWindowSettings();

            _Layers.Initialize();
            _NavAreas.Initialize();

            if (_Instance == this && !AssetDatabase.Contains(this))
                EditorApplication.delayCall += () => Editor.Window.WeaverWindow.OpenWindow();
        }

        /************************************************************************************************************************/

        internal static event Action OnAfterDeserialize;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_Instance == null)
                _Instance = this;

            OnAfterDeserialize?.Invoke();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        /************************************************************************************************************************/

        internal delegate void ClearOldDataFunction(ref bool isDirty);

        internal static event ClearOldDataFunction OnClearOldData;

        internal void ClearOldData()
        {
            if (EditorUtility.scriptCompilationFailed)
                return;

            var isDirty = false;
            OnClearOldData?.Invoke(ref isDirty);

            if (isDirty)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        /************************************************************************************************************************/

        internal static new void SetDirty()
        {
            EditorUtility.SetDirty(Instance);
        }

        /************************************************************************************************************************/

        private SerializedObject _SerializedObject;

        internal static SerializedObject SerializedObject
        {
            get
            {
                var instance = Instance;
                if (instance == null)
                    return null;

                if (instance._SerializedObject == null || instance._SerializedObject.targetObject != instance)
                    instance._SerializedObject = new SerializedObject(instance);

                return instance._SerializedObject;
            }
        }

        /************************************************************************************************************************/

        [SerializeField, HideInInspector]
        private bool _Dummy = false;

        /// <summary>
        /// When the type of an <see cref="AssetListBase"/> is changed Unity needs to serialize all references to it and
        /// then deserialize them with the new type. Unfortunately it the type change isn't an intentional feature of
        /// the engine so it doesn't have a clean way to do so. This method modifies a <see cref="SerializedProperty"/>
        /// for a dummy field to force Unity to recognise that the data has changed. This only works for references to
        /// the list in question inside <see cref="WeaverSettings"/> itself; unfortunately, other references will still
        /// hold their reference to the list object with the old type until assemblies are reloaded.
        /// </summary>
        internal static void ForceReSerialize()
        {
            EditorApplication.delayCall += () =>
            {
                var serializedObject = SerializedObject;
                var dummy = serializedObject.FindProperty(nameof(_Dummy));
                dummy.boolValue = !dummy.boolValue;
                serializedObject.ApplyModifiedProperties();
            };
        }

        /************************************************************************************************************************/
    }
}

#endif
