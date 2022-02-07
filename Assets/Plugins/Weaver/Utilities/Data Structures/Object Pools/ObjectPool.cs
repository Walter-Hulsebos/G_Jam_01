// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>
    /// An object with a callback method for when it is released to an <see cref="ObjectPool{T}"/>.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called by an <see cref="ObjectPool{T}"/> when releasing this item back to the pool.
        /// </summary>
        void OnRelease();
    }

    /************************************************************************************************************************/

    /// <summary>
    /// Various utilities and extension methods for <see cref="ObjectPool{T}"/>.
    /// <para></para>
    /// More detailed instructons on how to use this class and those related to it can be found at
    /// https://kybernetik.com.au/weaver/docs/misc/object-pooling.
    /// </summary>
    public static class ObjectPool
    {
        /************************************************************************************************************************/
        #region Create
        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that creates new items using a the default constructor of
        /// <typeparamref name="T"/> and uses a <see cref="HashSet{T}"/> to keep track of its
        /// <see cref="ObjectPool{T}.ActiveObjects"/>.
        /// </summary>
        public static ObjectPool<T> CreateDefaultPool<T>(int preAllocate = 0) where T : class, new()
        {
            return CreateDefaultPool(new HashSet<T>(), preAllocate);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that creates new items using a parameterless constructor.
        /// </summary>
        public static ObjectPool<T> CreateDefaultPool<T>(ICollection<T> activeObjects, int preAllocate = 0) where T : class, new()
        {
#if UNITY_ASSERTIONS
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                Debug.LogWarning(
                    $"Don't use {nameof(ObjectPool)}.{nameof(CreateDefaultPool)}() for types that inherit from " +
                    $"{nameof(Object)}. Try any of the other {nameof(ObjectPool)} methods instead.");
#endif

            return new ObjectPool<T>(activeObjects, () => new T(), preAllocate);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that creates new items by instantiating a specified
        /// <see cref="Component"/>. The objects will not be destroyed by scene loading.
        /// <para></para>
        /// In the Unity Editor the instantiated objects will be grouped under a common parent to keep the hierarchy
        /// view tidy, but this step is skipped in runtime builds for efficiency.
        /// </summary>
        public static ObjectPool<T> CreateComponentPool<T>(T original, int preAllocate = 0, bool releaseOnSceneLoad = true)
            where T : Component
        {
            var pool = new ObjectPool<T>(GetFunctionToInstantiateComponent(original), preAllocate);
            if (releaseOnSceneLoad)
                pool.ReleaseAllOnSceneUnload();
            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The first time this method is called for a particular `original` it will call
        /// <see cref="CreateComponentPool"/> and cache the returned pool so that subsequent calls using the same
        /// `original` will return the same pool.
        /// </summary>
        public static ObjectPool<T> GetSharedComponentPool<T>(T original, int preAllocate = 0, bool releaseOnSceneUnload = true)
            where T : Component
        {
            if (!SharedPools<T>.PrefabToPool.TryGetValue(original, out var pool))
            {
                pool = CreateComponentPool(original, preAllocate, releaseOnSceneUnload);
                SharedPools<T>.PrefabToPool.Add(original, pool);
            }

            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> that creates new items by instantiating a specified
        /// <see cref="GameObject"/>. The objects will not be destroyed by scene loading.
        /// <para></para>
        /// In the Unity Editor the instantiated objects will be grouped under a common parent to keep the hierarchy
        /// view tidy, but this step is skipped in runtime builds for efficiency.
        /// </summary>
        public static ObjectPool<GameObject> CreatePrefabPool(GameObject original, int preAllocate = 0, bool releaseOnSceneUnload = true)
        {
            var pool = new ObjectPool<GameObject>(GetFunctionToInstantiatePrefab(original), preAllocate);
            if (releaseOnSceneUnload)
                pool.ReleaseAllOnSceneUnload();
            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The first time this method is called for a particular `original` it will call
        /// <see cref="CreatePrefabPool"/> and cache the returned pool so that subsequent calls using the same
        /// `original` will return the same pool.
        /// </summary>
        public static ObjectPool<GameObject> GetSharedPrefabPool(GameObject original, int preAllocate = 0, bool releaseOnSceneLoad = true)
        {
            if (!SharedPools<GameObject>.PrefabToPool.TryGetValue(original, out var pool))
            {
                pool = CreatePrefabPool(original, preAllocate, releaseOnSceneLoad);
                SharedPools<GameObject>.PrefabToPool.Add(original, pool);
            }

            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// A static dictionary which maps an original <see cref="Object"/> such as a prefab to an
        /// <see cref="ObjectPool{T}"/> that creates new items by instantiating the original.
        /// </summary>
        private static class SharedPools<T> where T : Object
        {
            public static readonly Dictionary<T, ObjectPool<T>>
                PrefabToPool = new Dictionary<T, ObjectPool<T>>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a delegate that instantiates an inactive copy of the `original` object and calls
        /// <see cref="Object.DontDestroyOnLoad"/> on it.
        /// <para></para>
        /// In the Unity Editor the copies are also grouped under a parent object to keep the hierarchy view clean.
        /// </summary>
        public static Func<T> GetFunctionToInstantiateComponent<T>(T original) where T : Component
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            return () =>
            {
                var instance = Object.Instantiate(original);
                Object.DontDestroyOnLoad(instance);
                WeaverUtilities.EditorSetDefaultParent(instance.transform, " Pool");
                instance.gameObject.SetActive(false);
                return instance;
            };
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a delegate that instantiates an inactive copy of the `original` object and calls
        /// <see cref="Object.DontDestroyOnLoad"/> on it.
        /// <para></para>
        /// In the Unity Editor the copies are also grouped under a parent object to keep the hierarchy view clean.
        /// </summary>
        public static Func<GameObject> GetFunctionToInstantiatePrefab(GameObject original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            return () =>
            {
                var instance = Object.Instantiate(original);
                Object.DontDestroyOnLoad(instance);
                WeaverUtilities.EditorSetDefaultParent(instance.transform, " Pool");
                instance.SetActive(false);
                return instance;
            };
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the pool currently creating a new item, or null at all other times.
        /// This allows the created item's constructor to determine which pool it came from (if any).
        /// <para></para>
        /// Also registers the <see cref="IPoolable.OnRelease"/> of <typeparamref name="T"/> as the
        /// <see cref="ObjectPool{T}.OnRelease"/> callback if nothing was previously registered.
        /// </summary>
        public static ObjectPool<T> GetCurrentPool<T>() where T : class, IPoolable
        {
            var current = ObjectPool<T>.Current;

            if (current != null && current.OnRelease == null)
                current.OnRelease = (item) => item.OnRelease();

            return current;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Release
        /************************************************************************************************************************/

        /// <summary>
        /// If the `pool` isn't null this method gives the `item` to it and returns true.
        /// </summary>
        public static bool TryRelease<T>(this ObjectPool<T> pool, T item) where T : class
        {
            if (pool != null)
            {
                pool.Release(item);
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the `pool` isn't null this method gives the `item` to it and returns true.
        /// Otherwise this method destroys it and returns false.
        /// <para></para>
        /// Note that this method is likely not what you want to use for <see cref="Component"/>s.
        /// Use <see cref="TryReleaseOrDestroyGameObject{T}(ObjectPool{T}, T)"/> instead.
        /// </summary>
        public static bool TryReleaseOrDestroy<T>(this ObjectPool<T> pool, T item) where T : Object
        {
            if (pool.TryRelease(item))
            {
                return true;
            }
            else
            {
                Object.Destroy(item);
                return false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the `pool` isn't null this method releases the `component` to it, disables its <see cref="GameObject"/>,
        /// and returns true. Otherwise this method destroys the <see cref="GameObject"/> and returns false.
        /// </summary>
        public static bool TryReleaseOrDestroyGameObject<T>(this ObjectPool<T> pool, T component) where T : Component
        {
            if (pool.TryRelease(component))
            {
                component.gameObject.SetActive(false);
                return true;
            }
            else
            {
                Object.Destroy(component.gameObject);
                return false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the `behaviour` was created by an <see cref="ObjectPool{T}"/> this method releases the `behaviour` to
        /// it, disables its <see cref="GameObject"/>, and returns true.
        /// Otherwise this method destroys the <see cref="GameObject"/> and returns false.
        /// </summary>
        public static bool TryReleaseOrDestroyGameObject<T>(this T behaviour) where T : PoolableBehaviour<T>
        {
            return behaviour.Pool.TryReleaseOrDestroyGameObject(behaviour);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the `pool` isn't null this method gives the `component` to it, disables its <see cref="GameObject"/>,
        /// and returns true. Otherwise this method destroys the <see cref="GameObject"/> and returns false.
        /// </summary>
        public static bool TryReleaseOrDestroyGameObject(this ObjectPool<GameObject> pool, GameObject gameObject)
        {
            if (pool.TryRelease(gameObject))
            {
                gameObject.SetActive(false);
                return true;
            }
            else
            {
                Object.Destroy(gameObject);
                return false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Registers a <see cref="SceneManager.sceneUnloaded"/> callback to call <see cref="ObjectPool{T}.ReleaseAll()"/>.
        /// <para></para>
        /// The returned delegate can be stored to later unregister from the event if necessary.
        /// </summary>
        public static UnityAction<Scene> ReleaseAllOnSceneUnload<T>(this ObjectPool<T> pool)
            where T : class
        {
            UnityAction<Scene> onSceneUnloaded = (scene) => pool.ReleaseAll();

            SceneManager.sceneUnloaded += onSceneUnloaded;
            return onSceneUnloaded;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes and destroys items from the pool until the <see cref="List{T}.Count"/> reaches the
        /// `remainingSize`.
        /// </summary>
        public static void DestroyExcess<T>(this ObjectPool<T> pool, int remainingSize = 0) where T : Component
        {
            var items = pool.InactiveObjects;

            for (int i = remainingSize; i < items.Count; i++)
                Object.Destroy(items[i]?.gameObject);

            items.RemoveRange(remainingSize, items.Count - remainingSize);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

