// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>
    /// A <see cref="List{T}"/> of active objects backed by an <see cref="ObjectPool{T}"/> of inactive objects ready to
    /// be reused.
    /// <para></para>
    /// More detailed instructons on how to use this class and those related to it can be found at
    /// https://kybernetik.com.au/weaver/docs/misc/object-pooling.
    /// </summary>
    public sealed class PooledList<T> : ObjectPool<T> where T : class
    {
        /************************************************************************************************************************/

        /// <summary>The objects currently in use.</summary>
        public readonly new List<T> ActiveObjects;

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="PooledList{T}"/> which will create new items as necessary using the
        /// `createItem` delegate.
        /// </summary>
        public PooledList(Func<T> createItem, int preAllocate = 0)
            : base(new List<T>(), createItem, preAllocate)
        {
            ActiveObjects = (List<T>)base.ActiveObjects;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Puts an item back into the pool to be available for future use.
        /// </summary>
        public void ReleaseAt(int index)
        {
            var item = ActiveObjects[index];
            AssertNotAlreadyReleased(item);
            ActiveObjects.RemoveAt(index);
            InactiveObjects.Add(item);
            OnRelease?.Invoke(item);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Puts a range of items back into the pool to be available for future use.
        /// </summary>
        public void ReleaseRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = ActiveObjects[index + i];
                AssertNotAlreadyReleased(item);
                InactiveObjects.Add(item);
                OnRelease?.Invoke(item);
            }

            ActiveObjects.RemoveRange(index, count);
        }

        /************************************************************************************************************************/
    }

    /// <summary>
    /// Various utilities and extension methods for <see cref="PooledList{T}"/>.
    /// <para></para>
    /// More detailed instructons on how to use this class and those related to it can be found at
    /// https://kybernetik.com.au/weaver/docs/misc/object-pooling.
    /// </summary>
    public static class PooledList
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns a <see cref="PooledList{T}"/> that creates new items using a parameterless constructor.
        /// </summary>
        public static PooledList<T> CreateDefaultPool<T>(int preAllocate = 0)
            where T : class, new()
        {
#if UNITY_ASSERTIONS
            if (typeof(Object).IsAssignableFrom(typeof(T)))
                Debug.LogWarning(
                    $"Don't use {nameof(ObjectPool)}.{nameof(CreateDefaultPool)}() for types that inherit from " +
                    $"{nameof(Object)}. Try any of the other {nameof(PooledList)} methods instead.");
#endif

            return new PooledList<T>(() => new T(), preAllocate);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a <see cref="PooledList{T}"/> that creates new items by instantiating a specified
        /// <see cref="Component"/>. The objects will not be destroyed by scene loading.
        /// <para></para>
        /// In the Unity Editor the instantiated objects will be grouped under a common parent to keep the hierarchy
        /// view tidy, but this step is skipped in runtime builds for efficiency.
        /// </summary>
        public static PooledList<T> CreateComponentPool<T>(T original, int preAllocate = 0, bool releaseOnSceneLoad = true)
            where T : Component
        {
            var pool = new PooledList<T>(ObjectPool.GetFunctionToInstantiateComponent(original), preAllocate);
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
        public static PooledList<T> GetSharedComponentPool<T>(T original, int preAllocate = 0, bool releaseOnSceneLoad = true)
            where T : Component
        {
            if (!SharedPools<T>.PrefabToPool.TryGetValue(original, out var pool))
            {
                pool = CreateComponentPool(original, preAllocate, releaseOnSceneLoad);
                SharedPools<T>.PrefabToPool.Add(original, pool);
            }

            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="PooledList{T}"/> that creates new items by instantiating a specified
        /// <see cref="GameObject"/>. The objects will not be destroyed by scene loading.
        /// <para></para>
        /// In the Unity Editor the instantiated objects will be grouped under a common parent to keep the hierarchy
        /// view tidy, but this step is skipped in runtime builds for efficiency.
        /// </summary>
        public static PooledList<GameObject> CreatePrefabPool(GameObject original, int preAllocate = 0, bool releaseOnSceneLoad = true)
        {
            var pool = new PooledList<GameObject>(ObjectPool.GetFunctionToInstantiatePrefab(original), preAllocate);
            if (releaseOnSceneLoad)
                pool.ReleaseAllOnSceneUnload();
            return pool;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The first time this method is called for a particular `original` it will call
        /// <see cref="CreatePrefabPool"/> and cache the returned pool so that subsequent calls using the same
        /// `original` will return the same pool.
        /// </summary>
        public static PooledList<GameObject> GetSharedPrefabPool(GameObject original, int preAllocate = 0, bool releaseOnSceneLoad = true)
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
        /// A static dictionary which maps an original <see cref="Object"/> such as a prefab to a
        /// <see cref="PooledList{T}"/> that creates new items by instantiating the original.
        /// </summary>
        private static class SharedPools<T> where T : Object
        {
            public static readonly Dictionary<T, PooledList<T>>
                PrefabToPool = new Dictionary<T, PooledList<T>>();
        }

        /************************************************************************************************************************/
    }
}

