// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Weaver.Editor;
#endif

namespace Weaver
{
    /// <summary>
    /// A wrapper which simplifies the process of lazy loading and caching resources and other assets, i.e. only
    /// loading them when they are first needed instead of on startup.
    /// </summary>
    /// <typeparam name="T">The type of asset being wrapped. Must derive from <see cref="Object"/>.</typeparam>
    /// <example>
    /// This class can essentially replace the following code:
    /// <code>
    /// private static GameObject _Warrior;
    /// public static GameObject Warrior
    /// {
    ///     get
    ///     {
    ///         if (_Warrior == null)
    ///             _Warrior = Resources.Load&lt;GameObject&gt;("Creatures/Goblins/Warrior");
    ///         return _Warrior;
    ///     }
    /// }
    /// </code>
    /// With a single field:
    /// <code>
    /// public static readonly Asset&lt;GameObject&gt; Warrior = "Creatures/Goblins/Warrior";
    /// </code>
    ///
    /// <para></para>
    /// <list type="bullet">
    ///   <item>
    ///   You can either use an implicit conversion from string as shown above, or you can use a regular constructor
    ///   like so: <c>new Asset&lt;GameObject&gt;("Creatures/Goblins/Warrior").</c>
    ///   </item>
    ///   <item>
    ///   You can access the actual goblin warrior prefab using <c>Warrior.Target</c>. This will load the prefab
    ///   the first time it is actually used and cache the value for better performance when you need the asset again
    ///   in the future, just like the property in the above example.
    ///   </item>
    ///   <item>
    ///   You can also implicitly cast the <c>Warrior</c> field to a <c>GameObject</c>. Unfortunately, you
    ///   can’t use the basic <c>Object.Instantiate(Warrior)</c> due to ambiguity, but you can pass
    ///   <c>Warrior.Target</c> into any of the other overloads or use <c>Warrior.Instantiate()</c>.
    ///   </item>
    ///   <item>
    ///   If you specify a path that begins with <em>"Assets/"</em> and includes the file extension (such as
    ///   <em>"Assets/Art/Creatures/Goblins/Warrior.fbx"</em>), it will use <c>AssetDatabase.LoadAssetAtPath</c>
    ///   instead of <c>Resources.Load</c>. This allows you to target any asset in your project, though it won’t
    ///   be able to load it at runtime once your project is built.
    ///   </item>
    /// </list>
    /// </example>
    public sealed class Asset<T> where T : Object
    {
        /************************************************************************************************************************/

        /// <summary>The resource path or asset path of the asset.</summary>
        public readonly string Path;

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="Asset{T}"/> targeting the specified asset or resource path.</summary>
        public Asset(string path)
        {
            Path = path;
        }

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="Asset{T}"/> targeting the specified asset or resource path.</summary>
        public static implicit operator Asset<T>(string path)
        {
            return new Asset<T>(path);
        }

        /************************************************************************************************************************/

        private T _Target;

        /// <summary>Loads, caches, and returns the asset.</summary>
        public T Target
        {
            get
            {
                Load();
                return _Target;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Loads the asset into memory if it wasn't already loaded.</summary>
        public void Load()
        {
            if (_Target != null)
                return;

            _Target = Resources.Load<T>(Path);

#if UNITY_EDITOR

            if (_Target != null)
                return;

            _Target = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Path);

#endif
        }

        /************************************************************************************************************************/

        /// <summary>Unloads the asset from memory using <see cref="Resources.UnloadAsset"/>.</summary>
        public void Unload()
        {
            if (_Target != null)
            {
                Resources.UnloadAsset(_Target);
                _Target = null;
            }
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Reloads the asset, even if it was already loaded.</summary>
        public void ForceReload()
        {
            _Target = null;
            Load();
        }
#endif

        /************************************************************************************************************************/

        /// <summary>The file name of the asset (without the file extension).</summary>
        public string Name
        {
            get
            {
                if (_Target != null)
                    return _Target.name;
                else
                    return System.IO.Path.GetFileNameWithoutExtension(Path);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Checks if the asset is currently loaded.</summary>
        public bool IsLoaded
        {
            get { return _Target != null; }
        }

        /************************************************************************************************************************/

        /// <summary>Returns the <see cref="Target"/> asset.</summary>
        public static implicit operator T(Asset<T> asset)
        {
            return asset?.Target;
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Used as an alternate icon by <see cref="AssetReferenceAttribute"/>.</summary>
        private static Texture Icon => Icons.LazyReference;
#endif

        /************************************************************************************************************************/
        #region Instantiate Methods
        /************************************************************************************************************************/

#if UNITY_EDITOR
        private T EditorInstantiatePrefab(Transform parent, bool worldPositionStays, Vector3? position, Quaternion? rotation)
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return null;

            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(Target) as T;

            var gameObject = WeaverEditorUtilities.GetGameObject(instance, out _);
            if (gameObject == null)
                Object.DestroyImmediate(instance);

            var transform = gameObject.transform;

            transform.SetParent(parent, worldPositionStays);
            if (position != null)
                transform.position = position.Value;
            if (rotation != null)
                transform.rotation = rotation.Value;

            return instance;
        }
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// Clones the specified asset and returns the clone.
        /// Uses <see cref="Object.Instantiate(Object, Transform, bool)"/>.
        /// In the Unity Editor, it uses PrefabUtility.InstantiatePrefab instead.
        /// </summary>
        public T Instantiate(Transform parent, bool worldPositionStays)
        {
#if UNITY_EDITOR
            var instance = EditorInstantiatePrefab(parent, worldPositionStays, null, null);
            if (instance != null)
                return instance;
#endif

            return Object.Instantiate(Target, parent, worldPositionStays);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clones the specified asset and returns the clone.
        /// Uses <see cref="Object.Instantiate(Object, Transform)"/>.
        /// In the Unity Editor, it uses PrefabUtility.InstantiatePrefab instead.
        /// </summary>
        public T Instantiate(Transform parent)
        {
            return Instantiate(parent, true);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clones the specified asset and returns the clone.
        /// Uses <see cref="Object.Instantiate(Object, Vector3, Quaternion, Transform)"/>.
        /// In the Unity Editor, it uses PrefabUtility.InstantiatePrefab instead.
        /// </summary>
        public T Instantiate(Vector3 position, Quaternion rotation, Transform parent)
        {
#if UNITY_EDITOR
            var instance = EditorInstantiatePrefab(parent, false, position, rotation);
            if (instance != null)
                return instance;
#endif

            return Object.Instantiate(Target, position, rotation, parent);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clones the specified asset and returns the clone.
        /// Uses <see cref="Object.Instantiate(Object, Vector3, Quaternion)"/>.
        /// In the Unity Editor, it uses PrefabUtility.InstantiatePrefab instead.
        /// </summary>
        public T Instantiate(Vector3 position, Quaternion rotation)
        {
#if UNITY_EDITOR
            var instance = EditorInstantiatePrefab(null, true, position, rotation);
            if (instance != null)
                return instance;
#endif

            return Object.Instantiate(Target, position, rotation);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

