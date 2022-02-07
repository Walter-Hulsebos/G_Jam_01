// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>A variety of miscellaneous utility methods.</summary>
    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Gets an instance of the specified component type on a game object or adds one if it doesn't have one.
        /// </summary>
        /// <typeparam name="T">The type of component to get.</typeparam>
        /// <param name="gameObject">The game object to get it from.</param>
        /// <returns>The component that was found or added.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
                return gameObject.AddComponent<T>();
            else
                return component;
        }

        /************************************************************************************************************************/

        /// <summary>Is the array <c>null</c> or its <see cref="Array.Length"/> 0?</summary>
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the reference to its default value (null for reference types) and returns the original value.
        /// </summary>
        public static T Nullify<T>(ref T obj)
        {
            var temp = obj;
            obj = default;
            return temp;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Re-scales `value` from the old range (`oldMin` to `oldMax`) to the new range (0 to 1).
        /// </summary>
        public static float LinearRescaleTo01(this float value, float oldMin, float oldMax)
        {
            if (oldMin != oldMax)
                return (value - oldMin) / (oldMax - oldMin);
            else
                return 0.5f;
        }

        /// <summary>
        /// Re-scales `value` from the old range (`oldMin` to `oldMax`) to the new range (`newMin` to `newMax`).
        /// </summary>
        public static float LinearRescale(this float value, float oldMin, float oldmax, float newMin, float newmax)
        {
            return value.LinearRescaleTo01(oldMin, oldmax) * (newmax - newMin) + newMin;
        }

        /// <summary>
        /// Re-scales `value` from the old range (`oldMin` to `oldMax`) to the new range (`newMin` to `newMax`) and
        /// clamps it within that range.
        /// </summary>
        public static float LinearRescaleClamped(this float value, float oldMin, float oldmax, float newMin, float newmax)
        {
            return Mathf.Clamp01(value.LinearRescaleTo01(oldMin, oldmax)) * (newmax - newMin) + newMin;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional] <c>target.name = name</c></summary>
        [System.Diagnostics.Conditional(UnityEditor)]
        public static void EditorSetName(this Object target, string name) => target.name = name;

        /************************************************************************************************************************/

#if UNITY_EDITOR
        private static Dictionary<string, Transform> _NameToParent;
#endif

        /// <summary>[Editor-Conditional]
        /// Sets the <see cref="Transform.parent"/> to a default object based on its name.
        /// <para></para>
        /// This keeps the hierarchy neat in the Unity Editor without wasting processing time on it at runtime.
        /// </summary>
        [System.Diagnostics.Conditional(UnityEditor)]
        public static void EditorSetDefaultParent(Transform transform, string suffix = null)
        {
#if UNITY_EDITOR

            // Remove "(Clone)" from the end of the name to determine the parent name.
            var name = transform.name;
            while (name.EndsWith("(Clone)"))
                name = name.Substring(0, name.Length - 7);

            name += suffix;

            if (_NameToParent == null)
                _NameToParent = new Dictionary<string, Transform>();

            // Get or create a parent with that name.
            if (!_NameToParent.TryGetValue(name, out var parent))
            {
                parent = new GameObject(name).transform;
                Object.DontDestroyOnLoad(parent.gameObject);
                _NameToParent.Add(name, parent);
            }

            transform.SetParent(parent);

#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If <c>RESTRICT_USAGE</c> is defined, this method will log a warning. This is useful for verifying that
        /// certain methods are only used in certain contexts. For example, you might want to ensure that an
        /// inefficient method or class is not used in a release build of your application.
        /// </summary>
        [System.Diagnostics.Conditional("RESTRICT_USAGE")]
        public static void LogIfRestricted(string name)
        {
#if RESTRICT_USAGE
            Debug.LogWarning(name + " was used while RESTRICT_USAGE is defined.");
#endif
        }

        /************************************************************************************************************************/
        #region Collections
        /************************************************************************************************************************/

        /// <summary>
        /// If the `array` is null or its length isn't equal to the specified `size` this method replaces it with a new
        /// array of that `size`. Unlike <see cref="Array.Resize"/> this method does not copy the elements from the old
        /// array into the new one.
        /// </summary>
        public static void SetSize<T>(ref T[] array, int size)
        {
            if (array == null || array.Length != size)
                array = new T[size];
        }

        /// <summary>
        /// If the `array` is null or its length isn't equal to the specified `size` this method allocates a new array
        /// of that `size`, otherwise it just returns the `array`. Unlike <see cref="Array.Resize"/> this method does
        /// not copy the elements from the old array into the new one.
        /// </summary>
        public static T[] SetSize<T>(T[] array, int size)
        {
            if (array == null || array.Length != size)
                array = new T[size];

            return array;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the dictionary contains a value for the given key, that value is returned.
        /// Otherwise the default value is returned.
        /// </summary>
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;
            else
                return defaultValue;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes and returns the last element in a list.
        /// </summary>
        public static T Pop<T>(this List<T> list)
        {
            var value = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return value;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes and returns the last element in a list or creates a new one if the list is empty.
        /// </summary>
        public static T PopLastOrCreate<T>(this List<T> list) where T : new()
        {
            if (list.Count > 0)
            {
                var value = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return value;
            }
            else return new T();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a copy of `array` with `element` inserted at `index`.
        /// </summary>
        public static T[] InsertAt<T>(this T[] array, int index, T element)
        {
            if (array.IsNullOrEmpty())
                return new T[] { element };

            // Create new array.
            var newArray = new T[array.Length + 1];

            // Copy from the start of the old array up to the index where you are inserting the new element.
            Array.Copy(array, newArray, index);

            // Assign the new element at the desired index.
            newArray[index] = element;

            // Copy the rest of the old array after the new element.
            if (index < array.Length) Array.Copy(array, index, newArray, index + 1, array.Length - index);

            return newArray;
        }

        /// <summary>
        /// Returns a copy of `array` with `element` inserted at the end.
        /// </summary>
        public static T[] InsertAt<T>(this T[] array, T element)
        {
            if (array.IsNullOrEmpty())
                return new T[] { element };
            else
                return array.InsertAt(array.Length, element);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a copy of `array` with the element at `index` removed.
        /// </summary>
        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            // Create new arrays.
            var newArray = new T[array.Length - 1];

            // Copy from the start of the old array up to the target index.
            Array.Copy(array, newArray, index);

            // Skip over that index and copy the rest after that.
            Array.Copy(array, index + 1, newArray, index, newArray.Length - index);

            return newArray;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If `collection` doesn't already contain `value`, this method adds it and returns true.
        /// </summary>
        public static bool AddIfNew<T>(this ICollection<T> collection, T value)
        {
            if (!collection.Contains(value))
            {
                collection.Add(value);
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts `list`, maintaining the order of any elements with an identical comparison
        /// (unlike the standard <see cref="List{T}.Sort(Comparison{T})"/> method).
        /// </summary>
        public static void StableInsertionSort<T>(IList<T> list, Comparison<T> comparison)
        {
            var count = list.Count;
            for (int j = 1; j < count; j++)
            {
                var key = list[j];

                var i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

        /// <summary>
        /// Sorts `list`, maintaining the order of any elements with an identical comparison
        /// (unlike the standard <see cref="List{T}.Sort()"/> method).
        /// </summary>
        public static void StableInsertionSort<T>(IList<T> list) where T : IComparable<T>
        {
            StableInsertionSort(list, (a, b) => a.CompareTo(b));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Inserts a new value into a sorted list.
        /// </summary>
        public static void InsertSorted<T>(List<T> list, T item, IComparer<T> comparer)
        {
            if (list.Count == 0)
            {
                // Simple add.
                list.Add(item);
            }
            else if (comparer.Compare(item, list[list.Count - 1]) >= 0)
            {
                // Add to the end as the item being added is greater than the last item by comparison.
                list.Add(item);
            }
            else if (comparer.Compare(item, list[0]) <= 0)
            {
                // Add to the start as the item being added is less than the first item by comparison.
                list.Insert(0, item);
            }
            else
            {
                // Otherwise, search for the place to insert.
                var index = list.BinarySearch(item, comparer);
                if (index < 0)
                {
                    // The zero-based index of item if item is found;
                    // otherwise, a negative number that is the bitwise complement of the index of the next element
                    // that is larger than item or, if there is no larger element, the bitwise complement of Count.
                    index = ~index;
                }

                list.Insert(index, item);
            }
        }
        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="List{T}.Capacity"/> is less than the specified value, it is increased to that value.
        /// </summary>
        public static void RequireCapacity<T>(this List<T> list, int minCapacity)
        {
            if (list.Capacity < minCapacity)
                list.Capacity = minCapacity;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds or removes items to bring the <see cref="List{T}.Count"/> equal to the specified `count`.
        /// </summary>
        public static void SetCount<T>(List<T> list, int count)
        {
            if (count > list.Count)
            {
                list.Capacity = count;

                count -= list.Count;
                while (count-- > 0)
                    list.Add(default);
            }
            else
            {
                list.RemoveRange(count, list.Count - count);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

