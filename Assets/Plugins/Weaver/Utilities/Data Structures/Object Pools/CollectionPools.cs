// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Collections.Generic;

namespace Weaver
{
    /// <summary>A variety of miscellaneous utility methods.</summary>
    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Maintains a pool of <see cref="ICollection{T}"/> so they can be reused without garbage collection.
        /// </summary>
        public static class CollectionPool<TCollection, TElement> where TCollection : ICollection<TElement>, new()
        {
            /************************************************************************************************************************/

            /// <remarks>Not a Stack because it would create an unnecessary dependancy on System.dll.</remarks>
            private static readonly List<TCollection> Pool = new List<TCollection>();

            /************************************************************************************************************************/

            /// <summary>
            /// Returns an available collection from the pool or creates a new one if there are none.
            /// </summary>
            public static TCollection Get()
            {
                if (Pool.Count > 0)
                {
                    var collection = Pool.Pop();

#if UNITY_EDITOR
                    if (collection.Count != 0)
                        UnityEngine.Debug.Log(nameof(WeaverUtilities) +
                            "." + typeof(CollectionPool<TCollection, TElement>).GetNameCS() + "." + nameof(Get) +
                            " returned a non-empty collection. You should never use a collection after releasing it.");
#endif

                    return collection;
                }
                else
                {
                    return new TCollection();
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Clears a collection and puts it into the pool to be available for future use.
            /// </summary>
            public static void Release(TCollection collection)
            {
                collection.Clear();
                Pool.Add(collection);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #region List Pool
        /************************************************************************************************************************/

        /// <summary>
        /// Returns an available <see cref="List{T}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static List<T> GetList<T>()
        {
            return CollectionPool<List<T>, T>.Get();
        }

        /// <summary>
        /// Assigns an available <see cref="List{T}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static void GetList<T>(out List<T> list)
        {
            list = GetList<T>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clears the `list` and puts it into the pool to be available for reuse.
        /// </summary>
        public static void Release<T>(this List<T> list)
        {
            CollectionPool<List<T>, T>.Release(list);
        }

        /// <summary>
        /// Clears the `list` and puts it into the pool to be available for reuse, then sets the reference to null.
        /// <para></para>
        /// If `list` is already null, this method will do nothing.
        /// </summary>
        public static void Release<T>(ref List<T> list)
        {
            if (list != null)
            {
                CollectionPool<List<T>, T>.Release(list);
                list = null;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Dictionary Pool
        /************************************************************************************************************************/

        /// <summary>
        /// Returns an available <see cref="Dictionary{TKey, TValue}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
        {
            return CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Get();
        }

        /// <summary>
        /// Assigns an available <see cref="Dictionary{TKey, TValue}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static void GetDictionary<TKey, TValue>(out Dictionary<TKey, TValue> dictionary)
        {
            dictionary = GetDictionary<TKey, TValue>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clears the `dictionary` and puts it into the pool to be available for reuse.
        /// </summary>
        public static void Release<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Release(dictionary);
        }

        /// <summary>
        /// Clears the `dictionary` and puts it into the pool to be available for reuse, then sets the reference to null.
        /// <para></para>
        /// If `dictionary` is already null, this method will do nothing.
        /// </summary>
        public static void Release<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary != null)
            {
                CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>.Release(dictionary);
                dictionary = null;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Hash Set Pool
        /************************************************************************************************************************/

        /// <summary>
        /// Returns an available <see cref="HashSet{T}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static HashSet<T> GetHashSet<T>()
        {
            return CollectionPool<HashSet<T>, T>.Get();
        }

        /// <summary>
        /// Assigns an available <see cref="HashSet{T}"/> from the pool or creates a new one if there are none.
        /// </summary>
        public static void GetHashSet<T>(out HashSet<T> set)
        {
            set = GetHashSet<T>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clears the `set` and puts it into the pool to be available for reuse.
        /// </summary>
        public static void Release<T>(this HashSet<T> set)
        {
            CollectionPool<HashSet<T>, T>.Release(set);
        }

        /// <summary>
        /// Clears the `set` and puts it into the pool to be available for reuse, then sets the reference to null.
        /// <para></para>
        /// If `set` is already null, this method will do nothing.
        /// </summary>
        public static void Release<T>(ref HashSet<T> set)
        {
            if (set != null)
            {
                CollectionPool<HashSet<T>, T>.Release(set);
                set = null;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

