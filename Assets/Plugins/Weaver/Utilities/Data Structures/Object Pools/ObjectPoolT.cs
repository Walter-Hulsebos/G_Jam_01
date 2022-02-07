// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>
    /// A collection of objects that can create new items as necessary.
    /// Get an object from the pool with <see cref="Acquire"/>.
    /// Return it to the pool with <see cref="Release(T)"/>.
    /// <para></para>
    /// The non-generic <see cref="ObjectPool"/> class contains some useful methods of creating commonly used pools.
    /// <para></para>
    /// More detailed instructons on how to use this class and those related to it can be found at
    /// https://kybernetik.com.au/weaver/docs/misc/object-pooling.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns the pool currently creating a new item, or null at all other times.
        /// This allows the created item's constructor to determine which pool it came from (if any).
        /// <para></para>
        /// If <typeparamref name="T"/> implements <see cref="IPoolable"/> you should use
        /// <see cref="ObjectPool.GetCurrentPool{T}"/> instead.
        /// </summary>
        public static ObjectPool<T> Current { get; private set; }

        /************************************************************************************************************************/

        /// <summary>The objects currently in the pool waiting to be reused.</summary>
        /// <remarks>Not a Stack because it would create an unnecessary dependancy on System.dll.</remarks>
        public readonly List<T> InactiveObjects = new List<T>();

        /// <summary>
        /// The objects currently in use. May be null if this pool doesn't track objects acquired from it.
        /// This will be a <see cref="HashSet{T}"/> unless a different collection was provided in the constructor.
        /// </summary>
        public readonly ICollection<T> ActiveObjects;

        /// <summary>The factory delegate which is used to create new items if there are none in the pool.</summary>
        public readonly Func<T> CreateItem;

        /// <summary>An optional callback which is triggered by <see cref="Release(T)"/>.</summary>
        public Action<T> OnRelease { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> which uses the `createItem` function to create new objects
        /// when the pool is empty. The pool immediately creates a number of items specified by `preAllocate`.
        /// </summary>
        public ObjectPool(ICollection<T> activeObjects, Func<T> createItem, int preAllocate = 0)
        {
            ActiveObjects = activeObjects;
            CreateItem = createItem;

            if (preAllocate <= 0)
                return;

#if UNITY_EDITOR
            // The Unity Editor can trigger static constructors and field initializers at any time so we need to make
            // sure we only try to create UnityEngine.Objects on the main thread.
            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                // Don't pre-allocate UnityEngine.Objects in Edit Mode.
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    UnityEditor.EditorApplication.delayCall += () => SetMinCount(preAllocate);
                return;
            }
#endif

            InactiveCount = preAllocate;
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> which uses the `createItem` function to create new objects
        /// when the pool is empty. The pool immediately creates a number of items specified by `preAllocate`.
        /// </summary>
        public ObjectPool(Func<T> createItem, int preAllocate = 0)
            : this(new HashSet<T>(), createItem, preAllocate)
        { }

        /************************************************************************************************************************/

        /// <summary>
        /// The number of items in the <see cref="InactiveObjects"/> list.
        /// </summary>
        public int InactiveCount
        {
            get { return InactiveObjects.Count; }
            set
            {
                if (value > InactiveObjects.Count)
                {
                    var capacity = Mathf.Max(value, 16);
                    if (InactiveObjects.Capacity < capacity)
                        InactiveObjects.Capacity = capacity;

                    Current = this;
                    while (value-- > 0)
                        InactiveObjects.Add(CreateItem());
                    Current = null;
                }
                else
                {
                    InactiveObjects.RemoveRange(value, InactiveObjects.Count - value);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates new items in the pool until the number of <see cref="InactiveObjects"/> reaches the specified `count`.
        /// </summary>
        public void SetMinCount(int count)
        {
            if (InactiveCount < count)
                InactiveCount = count;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an available item, either by removing it from the <see cref="InactiveObjects"/> list if it contains
        /// any or by creating a new one. Also adds that item to the <see cref="ActiveObjects"/> collection.
        /// </summary>
        public T Acquire()
        {
            T item;

            var count = InactiveObjects.Count;
            if (count > 0)
            {
                count--;
                item = InactiveObjects[count];
                InactiveObjects.RemoveAt(count);
            }
            else
            {
                try
                {
                    Current = this;
                    item = CreateItem();
                }
                finally
                {
                    Current = null;
                }
            }

            ActiveObjects?.Add(item);

            return item;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Puts an item back into the pool to be available for future use.
        /// </summary>
        public void Release(T item)
        {
            AssertNotAlreadyReleased(item);

            ActiveObjects?.Remove(item);

            InactiveObjects.Add(item);

            OnRelease?.Invoke(item);
        }

        /// <summary>
        /// Puts an item back into the pool to be available for future use.
        /// Also sets the item to to avoid accidental use afterwards.
        /// </summary>
        public void Release(ref T item)
        {
            Release(item);
            item = default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Puts a collection of items back into the pool to be available for future use.
        /// </summary>
        public void ReleaseRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Release(item);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Releases all elements of this list back to the pool and clears it.
        /// <para></para>
        /// Throws a <see cref="NullReferenceException"/> if <see cref="ActiveObjects"/> is null.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var item in ActiveObjects)
            {
                AssertNotAlreadyReleased(item);
                InactiveObjects.Add(item);
                OnRelease?.Invoke(item);
            }

            ActiveObjects.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// Logs an error if the `item` is already the last object added to the <see cref="InactiveObjects"/> list.
        /// </summary>
        [System.Diagnostics.Conditional(WeaverUtilities.UnityEditor)]
        protected void AssertNotAlreadyReleased(T item)
        {
            if (InactiveObjects.Count > 0 && InactiveObjects[InactiveObjects.Count - 1] == item)
                Debug.LogError("Item has been released to its ObjectPool more than once: " + item, item as Object);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the contents of this pool.
        /// </summary>
        public override string ToString()
        {
            return $"{base.ToString()}" +
                $"\n- Create Item: {CreateItem.Method.GetNameCS()}" +
                $"\n- On Release: {OnRelease?.Method.GetNameCS()}" +
                $"\n- Active Objects: {ActiveObjects?.DeepToString()}" +
                $"\n- Inactive Objects: {InactiveObjects.DeepToString()}";
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Draws the details of this pool in the GUI.</summary>
        public void DoInspectorGUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Object Pool", typeof(T).GetNameCS());
            UnityEditor.EditorGUILayout.LabelField("Create Item", CreateItem.Method.GetNameCS(CSharp.NameVerbosity.Basic));
            UnityEditor.EditorGUILayout.LabelField("On Release", OnRelease != null ? OnRelease.Method.GetNameCS(CSharp.NameVerbosity.Basic) : "null");

            if (ActiveObjects == null)
            {
                UnityEditor.EditorGUILayout.LabelField("Active Objects", "null");
            }
            else
            {
                UnityEditor.EditorGUILayout.LabelField("Active Objects", ActiveObjects.Count.ToString());

                foreach (var item in ActiveObjects)
                {
                    var obj = item as Object;
                    if (obj != null)
                        UnityEditor.EditorGUILayout.ObjectField(obj, typeof(T), true);
                    else
                        GUILayout.Label(item?.ToString());
                }
            }

            UnityEditor.EditorGUILayout.LabelField("Inactive Objects", InactiveObjects.Count.ToString());

            foreach (var item in InactiveObjects)
            {
                var obj = item as Object;
                if (obj != null)
                    UnityEditor.EditorGUILayout.ObjectField(obj, typeof(T), true);
                else
                    GUILayout.Label(item?.ToString());
            }
        }
#endif

        /************************************************************************************************************************/
    }
}

