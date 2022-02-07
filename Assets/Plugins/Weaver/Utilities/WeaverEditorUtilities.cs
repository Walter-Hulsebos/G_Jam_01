// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Weaver.Editor.Window;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only] A variety of miscellaneous utility methods.</summary>
    public static partial class WeaverEditorUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Sets <c>transform.hideFlags = flags</c> and does the same for all the children of `transform`.
        /// </summary>
        public static void SetHideFlagsRecursive(Transform transform, HideFlags flags)
        {
            transform.gameObject.hideFlags = flags;

            for (int i = 0; i < transform.childCount; i++)
            {
                SetHideFlagsRecursive(transform.GetChild(i), flags);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Starts a coroutine to run in the editor update loop.
        /// The coroutine can be cancelled by removing the returned delegate from
        /// <see cref=" EditorApplication.update"/>.
        /// </summary>
        public static EditorApplication.CallbackFunction EditorStartCoroutine(IEnumerator coroutine)
        {
            EditorApplication.CallbackFunction update = null;

            update = () =>
            {
                if (!coroutine.MoveNext())
                    EditorApplication.update -= update;
            };

            EditorApplication.update += update;
            return update;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// <see cref="BuildPipeline.isBuildingPlayer"/> isn't true during
        /// <see cref="UnityEditor.Build.IPreprocessBuildWithReport.OnPreprocessBuild"/> so we use this value instead.
        /// </summary>
        public static bool IsPreprocessingBuild { get; internal set; }

        /// <summary>[Editor-Only]
        /// Indicates whether a runtime build is currently being compiled.
        /// </summary>
        public static bool IsBuilding => IsPreprocessingBuild || BuildPipeline.isBuildingPlayer;

        /// <summary>[Editor-Only]
        /// Some assets (such as scripts) don't always need to regenerate.
        /// This property is set to true when the user generates a single asset rather than a group.
        /// </summary>
        public static bool ForceGenerate { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Tries to determine whether the `type` is only available in the Unity Editor.</summary>
        public static bool IsEditorOnly(Type type)
        {
            // Check the type.
            if (type.Namespace != null && type.Namespace.StartsWith(nameof(UnityEditor)))
                return true;

            // Check its generic arguments (if any).
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (IsEditorOnly(arguments[i]))
                        return true;
                }
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// If `obj` is a <see cref="GameObject"/> this method casts and returns it.
        /// Or if it is a <see cref="Component"/> this method returns its <see cref="Component.gameObject"/> and
        /// outputs the `component`.
        /// Otherwise it returns null.
        /// </summary>
        public static GameObject GetGameObject(Object obj, out Component component)
        {
            if (obj is GameObject gameObject)
            {
                component = null;
                return gameObject;
            }

            component = obj as Component;
            if (component != null)
            {
                return component.gameObject;
            }

            return null;
        }

        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="Color"/> used to indicate a warning.</summary>
        public static readonly Color WarningColor = new Color(1, 0.9f, 0.6f);

        /// <summary>[Editor-Only] The <see cref="Color"/> used to indicate an error.</summary>
        public static readonly Color ErrorColor = new Color(1, 0.75f, 0.75f);

        /************************************************************************************************************************/

        private static GUILayoutOption[] _DontExpandWidth;

        /// <summary>[Editor-Only]
        /// The cached result of <see cref="GUILayout.ExpandWidth"/> with a false parameter.
        /// </summary>
        public static GUILayoutOption[] DontExpandWidth
        {
            get
            {
                if (_DontExpandWidth == null)
                    _DontExpandWidth = new GUILayoutOption[] { GUILayout.ExpandWidth(false) };
                return _DontExpandWidth;
            }
        }

        /************************************************************************************************************************/

        private static GUIContent _TempContent;

        /// <summary>
        /// Returns a <see cref="GUIContent"/> containing the specified parameters. The same object is returned every
        /// time so it can be reused without causing garbage collection.
        /// </summary>
        public static GUIContent TempContent(string text, string tooltip = null, Texture image = null)
        {
            if (_TempContent == null)
                _TempContent = new GUIContent();
            _TempContent.text = text;
            _TempContent.tooltip = tooltip;
            _TempContent.image = image;
            return _TempContent;
        }

        /************************************************************************************************************************/

        private static GUIContent _TempTypeContent;

        /// <summary>[Editor-Only]
        /// Returns a <see cref="GUIContent"/> using the type name as the text, full name as the tooltip, and
        /// <see cref="AssetPreview"/> icon as the image (if it has one). The same object is returned every time so it
        /// can be reused without causing garbage collection.
        /// </summary>
        public static GUIContent TempTypeContent(Type type)
        {
            if (_TempTypeContent == null)
                _TempTypeContent = new GUIContent();
            _TempTypeContent.text = type.GetNameCS(CSharp.NameVerbosity.Basic);
            _TempTypeContent.tooltip = type.GetNameCS();
            _TempTypeContent.image = AssetPreview.GetMiniTypeThumbnail(type);
            return _TempTypeContent;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws a <see cref="GUILayout.Toggle(bool, GUIContent, GUILayoutOption[])"/> and returns true if the value
        /// is changed.
        /// </summary>
        public static bool DoToggle(ref bool value, string label, string tooltip)
        {
            var newValue = EditorGUILayout.Toggle(WeaverEditorUtilities.TempContent(label, tooltip), value);
            if (value != newValue)
            {
                value = newValue;
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Works like <see cref="ReorderableList.DoLayoutList"/> but doesn't screw up the padding while a fade group
        /// is animating.
        /// </summary>
        public static void DoLayoutListFixed(this ReorderableList list, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(false, list.GetHeight(), options);
            list.DoList(rect);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Assets
        /************************************************************************************************************************/

        /// <summary>
        /// Replaces each GUID in an array with the corresponding asset path using
        /// <see cref="AssetDatabase.GUIDToAssetPath"/>
        /// </summary>
        public static void GUIDsToAssetPaths(string[] guids)
        {
            for (int i = 0; i < guids.Length; i++)
                guids[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an asset of the specified type from anywhere in the project.
        /// </summary>
        public static T FindAsset<T>() where T : Object
        {
            var filter = typeof(Component).IsAssignableFrom(typeof(T)) ?
                "t:GameObject" :
                "t:" + typeof(T).Name;

            var guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an asset of the specified type from anywhere in the project.
        /// Checks the `assetPathHint` before searching the rest of the project.
        /// </summary>
        public static T FindAsset<T>(string assetPathHint) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPathHint);
            if (asset != null)
                return asset;

            return FindAsset<T>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified path includes a "Resources" folder, along with the character index at which
        /// the resource path starts (the character after the "Resources" folder).
        /// </summary>
        public static bool IsResource(string assetPath, out int resourcePathStart)
        {
            const int CheckLength = 10;// "/Resources".Length;
            if (assetPath.Length <= CheckLength)
            {
                resourcePathStart = 0;
                return false;
            }

            resourcePathStart = 6;// "Assets".Length;
            while (true)
            {
                resourcePathStart = assetPath.IndexOf("/Resources", resourcePathStart);

                if (resourcePathStart >= 0)
                {
                    resourcePathStart += CheckLength;
                    if (resourcePathStart >= assetPath.Length || assetPath[resourcePathStart] == '/')
                    {
                        resourcePathStart++;
                        return true;
                    }
                }
                else break;
            }

            resourcePathStart = 0;
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the resource path substring of the `assetPath` starting at the `resourcePathStart` and ending
        /// without the file extension.
        /// </summary>
        public static string AssetToResourcePath(string assetPath, int resourcePathStart)
        {
            assetPath = assetPath.Substring(resourcePathStart, assetPath.Length - resourcePathStart);
            assetPath = GetPathWithoutExtension(assetPath);
            return assetPath;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Deletes the specified directory if it is empty (ignoring metadata files), then does the same recursively
        /// for each parent directory. Refreshes the <see cref=" AssetDatabase"/> if anything was deleted.
        /// </summary>
        public static void DeleteEmptyDirectories(string path)
        {
            path = path.RemoveTrailingSlashes();

            AssetDatabase.StartAssetEditing();

            do
            {
                if (!AssetDatabase.IsValidFolder(path) ||
                    ContainsNonMetaFiles(path, out string[] _))
                    break;

                AssetDatabase.DeleteAsset(path);

                path = Path.GetDirectoryName(path);
            }
            while (path != null);

            AssetDatabase.StopAssetEditing();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Checks if the specified directory contains any files which don't end with ".meta".
        /// </summary>
        public static bool ContainsNonMetaFiles(string directory, out string[] files)
        {
            files = Directory.GetFileSystemEntries(directory);

            for (int i = 0; i < files.Length; i++)
                if (!files[i].EndsWith(".meta"))
                    return true;

            return false;
        }

        /************************************************************************************************************************/

        internal static bool AskAndDeleteWeaver()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            var weaverDirectory = GetRelativeAssemblyPath(typeof(WeaverUtilities));

            var message = WeaverUtilities.GetStringBuilder()
                .AppendLineConst("Are you sure you want to remove Weaver from your project?")
                .AppendLineConst()
                .AppendLineConst("The following directories will be deleted:")
                .Append("- ").AppendLineConst(weaverDirectory);

            const string OldOutputDirectory = "Assets/Weaver";
            if (Directory.Exists(OldOutputDirectory))
                message.AppendLineConst("- " + OldOutputDirectory);

            var outputDirectory = ProceduralAssetSettings.OutputDirectory;
            outputDirectory = outputDirectory.Substring(0, outputDirectory.Length - 1);
            if (outputDirectory != OldOutputDirectory && Directory.Exists(outputDirectory))
                message.Append("- ").AppendLineConst(outputDirectory);

            message.AppendLineConst().Append("You cannot undo this action.");

            message.Replace('\\', '/');

            if (EditorUtility.DisplayDialog("Delete Weaver?", message.ReleaseToString(), "Delete", "Cancel"))
            {
                var windows = Object.FindObjectsOfType<WeaverWindow>();
                for (int i = 0; i < windows.Length; i++)
                    Object.DestroyImmediate(windows[i]);

                AssetDatabase.StartAssetEditing();
                {
                    AssetDatabase.DeleteAsset(weaverDirectory);
                    AssetDatabase.DeleteAsset(OldOutputDirectory);
                    AssetDatabase.DeleteAsset(outputDirectory);

                    weaverDirectory = Path.GetDirectoryName(weaverDirectory);
                    if (!string.IsNullOrEmpty(weaverDirectory))
                        DeleteEmptyDirectories(weaverDirectory);
                }
                AssetDatabase.StopAssetEditing();

                Debug.Log("Weaver has been deleted. Thanks for trying it out." +
                    $" If you have any feedback, please send it to {WeaverUtilities.DeveloperEmail}");

                return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private static string GetRelativeAssemblyPath(Type type)
        {
            var directory = Path.GetDirectoryName(type.Assembly.Location);
            if (directory.StartsWith(Environment.CurrentDirectory))
                directory = directory.Substring(
                    Environment.CurrentDirectory.Length + 1,
                    directory.Length - (Environment.CurrentDirectory.Length + 1));
            return directory;
        }

        /************************************************************************************************************************/

        private static string _WeaverPluginsDirectory;

        /// <summary>
        /// The asset path of the folder containing the Weaver assembly. "Assets/Plugins/Weaver" by default.
        /// </summary>
        public static string WeaverPluginsDirectory
        {
            get
            {
                if (_WeaverPluginsDirectory != null)
                    return _WeaverPluginsDirectory;

                const string Default = "Assets/Plugins/Weaver";

                var script = MonoScript.FromScriptableObject(WeaverSettings.Instance);
                if (script == null)
                    return _WeaverPluginsDirectory = Default;

                // Assets/Plugins/Weaver/Utilities/WeaverSettings.cs (by default).
                _WeaverPluginsDirectory = AssetDatabase.GetAssetPath(script);

                // Assets/Plugins/Weaver/Utilities (by default).
                _WeaverPluginsDirectory = Path.GetDirectoryName(_WeaverPluginsDirectory);

                // Assets/Plugins/Weaver (by default).
                if (_WeaverPluginsDirectory.ReplaceSlashesForward().EndsWith("/Weaver/Utilities"))
                    _WeaverPluginsDirectory = Path.GetDirectoryName(_WeaverPluginsDirectory);

                return _WeaverPluginsDirectory;
            }
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<Type, string[]>
            TypeToAssetPaths = new Dictionary<Type, string[]>();

        /// <summary>
        /// Returns the paths of all assets of the specified `type`.
        /// <para></para>
        /// The result is cached for efficiency and can be cleared by <see cref="ClearAssetPathCache"/>.
        /// <para></para>
        /// If the `type` is a <see cref="Component"/>, this method returns the paths of all prefabs so they will need
        /// to be checked for that component individually.
        /// </summary>
        public static string[] FindAllAssetPaths(Type type)
        {
            if (!TypeToAssetPaths.TryGetValue(type, out var assetPaths))
            {
                if (typeof(Component).IsAssignableFrom(type))
                {
                    assetPaths = FindAllAssetPaths(typeof(GameObject));
                }
                else
                {
                    assetPaths = AssetDatabase.FindAssets("t:" + type.Name);
                    GUIDsToAssetPaths(assetPaths);
                }

                TypeToAssetPaths.Add(type, assetPaths);
            }

            return assetPaths;
        }

        /// <summary>
        /// Clears all paths cached by <see cref="FindAllAssetPaths"/>.
        /// </summary>
        public static void ClearAssetPathCache()
        {
            TypeToAssetPaths.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns the default file extension for a type derived from <see cref="Object"/>.</summary>
        public static string GetDefaultFileExtension(Type type)
        {
            if (typeof(GameObject) == type ||
                typeof(Component).IsAssignableFrom(type))
                return "prefab";

            if (typeof(ScriptableObject) == type)
                return "asset";

            if (typeof(Texture) == type ||
                typeof(Texture2D) == type ||
                typeof(Sprite) == type)
                return "png";

            if (typeof(AnimationClip) == type) return "anim";
            if (typeof(RuntimeAnimatorController) == type) return "controller";
            if (typeof(AnimatorOverrideController) == type) return "overrideController";
            if (typeof(UnityEngine.Audio.AudioMixer) == type) return "mixer";
            if (typeof(AvatarMask) == type) return "mask";
            if (typeof(Font) == type) return "fontsettings";
            if (typeof(GUISkin) == type) return "guiskin";
            if (typeof(Material) == type) return "mat";
            if (typeof(PhysicMaterial) == type) return "physicMaterial";
            if (typeof(PhysicsMaterial2D) == type) return "physicsMaterial2D";

            if (typeof(Shader) == type) return "shader";
            if (typeof(ShaderVariantCollection) == type) return "shadervariants";
            if (typeof(ComputeShader) == type) return "compute";
            if (typeof(Cubemap) == type) return "cubemap";
            if (typeof(Flare) == type) return "flare";
            if (typeof(LightmapParameters) == type) return "giparams";

            if (typeof(MonoScript) == type) return "cs";
            if (typeof(TextAsset).IsAssignableFrom(type)) return "txt";

            if (typeof(UnityEngine.Audio.AudioMixer) == type) return "mixer";
            if (typeof(UnityEngine.U2D.SpriteAtlas) == type) return "spriteatlas";

            if (typeof(Avatar) == type ||
                typeof(AudioClip) == type ||
                typeof(Texture2DArray) == type ||
                typeof(Texture3D) == type)
                return null;

            return "asset";
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new asset of the specified `type` at the specified `assetPath`.</summary>
        public static Object CreateNewAsset(Type type, string assetPath)
        {
            Object asset;

            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                asset = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else if (typeof(Component).IsAssignableFrom(type))
            {
                var name = Path.GetFileNameWithoutExtension(assetPath);
                var component = new GameObject(name).AddComponent(type);
                asset = PrefabUtility.SaveAsPrefabAsset(component.gameObject, assetPath, out _);
            }
            else if (typeof(GameObject) == type)
            {
                var name = Path.GetFileNameWithoutExtension(assetPath);
                var gameObject = new GameObject(name);
                asset = PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath, out _);
            }
            else
            {
                asset = (Object)Activator.CreateInstance(type);
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            return asset;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Sub Assets
        /************************************************************************************************************************/

        /// <summary>
        /// [Editor-Only] After you save the scene object `obj` as an asset file and load it as `asset`, this method
        /// goes through all the serialized <see cref="Object"/> fields which weren't saved and adds them
        /// as sub-assets. Returns true if any sub-assets were saved.
        /// <para></para>
        /// For example, creating a procedural <see cref="Mesh"/>, assigning it to a <see cref="MeshFilter"/>, and
        /// saving the object as a prefab would not save the mesh anywhere unless you call this method or
        /// <see cref=" AssetDatabase.AddObjectToAsset(Object, Object)"/>.
        /// </summary>
        public static bool SaveSubAssets(Object obj, Object asset)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            if (obj is Component component)
                obj = component.gameObject;

            var checkedObjects = WeaverUtilities.GetDictionary<Object, bool>();
            var hasSubAssets = GatherSubAssets(obj, asset, checkedObjects);
            checkedObjects.Release();

            if (hasSubAssets)
            {
                // Sub asset references don't get assigned properly in 2018.3+ otherwise.
                if (obj is GameObject gameObject)
                {
                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath, out _);
                }

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        private static bool GatherSubAssets(Object target, Object asset, Dictionary<Object, bool> checkedObjects)
        {
            // checkedObjects contains objects mapped to a value indicating if they are sub assets.

            if (checkedObjects.ContainsKey(target))
                return false;

            checkedObjects.Add(target, false);

            if (!MightHaveSubAssets(target))
                return false;

            using (var serializedObject = new SerializedObject(target))
            {
                var targetProperty = serializedObject.GetIterator();
                if (!targetProperty.Next(true))
                    return false;

                using (var assetObject = new SerializedObject(asset))
                {
                    var modified = false;
                    var hasSubAssets = false;

                    do
                    {
                        if (targetProperty.propertyType != SerializedPropertyType.ObjectReference)
                            continue;

                        var obj = targetProperty.objectReferenceValue;
                        if (obj == null)
                            continue;

                        var assetProperty = assetObject.FindProperty(targetProperty.propertyPath);
                        if (assetProperty == null)
                            continue;

                        if (assetProperty.objectReferenceValue == null)
                        {
                            if (checkedObjects.TryGetValue(obj, out bool isSubAsset))
                            {
                                if (isSubAsset)
                                    goto ReAssignObject;
                                else
                                    continue;
                            }

                            if (obj is GameObject ||
                                obj is Component ||
                                 AssetDatabase.Contains(obj))
                                continue;

#if WEAVER_DEBUG
                            Debug.Log($"Saving Sub Asset: {obj} in asset: {asset}");
#endif

                            checkedObjects.Add(obj, true);

                            AssetDatabase.AddObjectToAsset(obj, asset);

                            ReAssignObject:
                            assetProperty.objectReferenceValue = obj;

                            modified = true;
                            hasSubAssets = true;
                        }
                        else
                        {
                            if (GatherSubAssets(obj, assetProperty.objectReferenceValue, checkedObjects))
                                hasSubAssets = true;
                        }
                    }
                    while (targetProperty.Next(true));

                    if (modified)
                        assetObject.ApplyModifiedPropertiesWithoutUndo();

                    return hasSubAssets;
                }
            }
        }

        /************************************************************************************************************************/

        private static bool MightHaveSubAssets(Object obj)
        {
            // Using a Dictionary for this increases the startup time without reducing the execution time. :(

            if (obj is GameObject ||
                obj is Transform ||
                obj is MonoBehaviour ||
                obj is ScriptableObject)
                return true;

            if (obj is Rigidbody ||
                obj is Rigidbody2D ||
                obj is Material ||
                obj is Texture ||
                obj is AudioClip ||
                obj is TextAsset ||
                obj is ParticleSystem ||
                obj is Mesh ||
                obj is PhysicMaterial ||
                obj is PhysicsMaterial2D ||
                obj is Sprite ||
                obj is RuntimeAnimatorController ||
                obj is Font ||
                obj is Flare ||
                obj is LightProbes ||
                obj is AssetBundle ||
                obj is AssetBundleManifest ||
                obj is RenderSettings ||
                obj is QualitySettings ||
                obj is Motion ||
                obj is BillboardAsset ||
                obj is LightmapSettings ||
                obj is AnimationClip ||
                obj is Avatar ||
                obj is TerrainData ||
                obj is Shader ||
                obj is ShaderVariantCollection ||
                obj is UnityEngine.Rendering.GraphicsSettings ||
                obj is UnityEngine.Audio.AudioMixer ||
                obj is UnityEngine.Audio.AudioMixerSnapshot ||
                obj is UnityEngine.Audio.AudioMixerGroup ||
                obj is AssetImporter ||
                obj is UnityEditor.Animations.AnimatorTransitionBase ||
                obj is UnityEditor.Animations.AnimatorState ||
                obj is UnityEditor.Animations.AnimatorStateMachine ||
                obj is AvatarMask ||
                obj is UnityEditor.Animations.BlendTree ||
                obj is DefaultAsset ||
                obj is UnityEditor.Editor ||
                obj is EditorSettings ||
                obj is EditorUserSettings ||
                obj is EditorWindow ||
                obj is Tools ||
                obj is HumanTemplate ||
                obj is PlayerSettings ||
                obj is SceneAsset ||
                obj is LightingDataAsset ||
                obj is LightmapParameters)
                return false;

            return obj.GetType() != typeof(Object);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Destroys all sub-assets which are part of the specified asset.</summary>
        public static void DestroySubAssets(string assetPath)
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                var subAsset = subAssets[i];
                if (!(subAsset is Component) && !(subAsset is GameObject) && AssetDatabase.IsSubAsset(subAsset))
                {
#if WEAVER_DEBUG
                    Debug.Log("Destroying sub asset " + subAsset);
#endif

                    Object.DestroyImmediate(subAsset, true);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Menu Items
        /************************************************************************************************************************/

        /// <summary>
        /// Opens the specified `url`. If it begins with a '/' it is treated as a path relative to the
        /// <see cref="WeaverUtilities.DocumentationURL"/>.
        /// </summary>
        public static void OpenRelativeURL(string url)
        {
            if (url[0] == '/')
                url = WeaverUtilities.DocumentationURL + url;

            Application.OpenURL(url);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a function to open the specified `url`. If it begins with a '/' it is treated as a path relative to
        /// the <see cref="WeaverUtilities.DocumentationURL"/>.
        /// </summary>
        public static void AddLinkToURL(GenericMenu menu, string text, string url)
        {
            menu.AddItem(new GUIContent(text), false, () => OpenRelativeURL(url));
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Strings
        /************************************************************************************************************************/

        private static Dictionary<Type, string> _AttributeTypeToDisplayString;

        /// <summary>
        /// Returns a friendly display string for the specified attribute `type` surrounded by square brackets.
        /// <para></para>
        /// For example, <see cref="AssetReferenceAttribute"/> would return "[AssetReference]".
        /// </summary>
        public static string GetAttributeDisplayString(Type type)
        {
            if (_AttributeTypeToDisplayString == null)
                _AttributeTypeToDisplayString = new Dictionary<Type, string>();

            if (!_AttributeTypeToDisplayString.TryGetValue(type, out var str))
            {
                str = type.GetNameCS();

                if (str.EndsWith("Attribute"))
                    str = str.Substring(0, str.Length - 9);

                str = $"[{str}]";
                _AttributeTypeToDisplayString.Add(type, str);
            }

            return str;
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, string> _TypeToFullNameWithoutNamespace;

        /// <summary>
        /// Returns the full name of the `type` and any types it is nested inside, but without the namespace prefix.
        /// </summary>
        public static string GetFullNameWithoutNamespace(Type type)
        {
            if (type.DeclaringType == null)
                return type.Name;

            if (_TypeToFullNameWithoutNamespace == null)
                _TypeToFullNameWithoutNamespace = new Dictionary<Type, string>();

            if (!_TypeToFullNameWithoutNamespace.TryGetValue(type, out var name))
            {
                name = GetFullNameWithoutNamespace(type.DeclaringType) + "." + type.Name;
                _TypeToFullNameWithoutNamespace.Add(type, name);
            }

            return name;
        }

        /************************************************************************************************************************/

        private static Dictionary<string, string> _PathsWithoutExtension;

        /// <summary>
        /// Returns a copy of the `path` with the file extension removed from the end and caches the result.
        /// </summary>
        public static string GetPathWithoutExtension(string path)
        {
            if (_PathsWithoutExtension == null)
                _PathsWithoutExtension = new Dictionary<string, string>();

            if (!_PathsWithoutExtension.TryGetValue(path, out var withoutExtension))
            {
                var index = path.LastIndexOf('/') + 1;
                index = path.LastIndexOf('.', path.Length - 1, path.Length - index);

                if (index >= 0)
                {
                    withoutExtension = path.Substring(0, index);
                }
                else
                {
                    withoutExtension = path;
                }

                _PathsWithoutExtension.Add(path, withoutExtension);
            }

            return withoutExtension;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

