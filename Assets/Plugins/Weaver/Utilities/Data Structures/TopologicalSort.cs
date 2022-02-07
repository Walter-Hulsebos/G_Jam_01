// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;

namespace Weaver
{
    /// <summary>
    /// Indicates that a type can depend on other objects.
    /// Used for topological sorting.
    /// </summary>
    public interface IDependant<T>
    {
        /// <summary>Everything that this object is dependant on. Return null if there are none.</summary>
        IEnumerable<T> Dependancies { get; }
    }

    /************************************************************************************************************************/

    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if [on] is a dependancy of [dependant].
        /// </summary>
        public static bool IsDependantOn<T>(this IDependant<T> dependant, T on)
        {
            var dependancies = dependant.Dependancies;
            if (dependancies == null) return false;

            foreach (var dependancy in dependancies)
            {
                if (EqualityComparer<T>.Default.Equals(dependancy, on))
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts a collection of <see cref="IDependant{T}"/>.
        /// If ItemA depends on ItemB, ItemA will be put later in the returned list.
        /// </summary>
        /// <param name="collection">The collection to sort. If any item depends on a type that isn't present, it will be added automatically.</param>
        /// <param name="ignoreCycles">If false, an <see cref="ArgumentException"/> will be thrown when a cyclic dependancy is encountered.</param>
        public static List<T> TopologicalSort<T>(IEnumerable<T> collection, bool ignoreCycles = false) where T : IDependant<T>
        {
            return TopologicalSort(collection, item => item.Dependancies, null, ignoreCycles);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts a collection according to a collection of dependants.
        /// If ItemA depends on ItemB, ItemA will be put later in the returned list.
        /// </summary>
        /// <param name="collection">The collection to sort. If any element depends on something that isn't present, it will be added automatically.</param>
        /// <param name="dependants">An array which specifies what each element is dependant on.</param>
        /// <param name="comparer">The equality comparer to use. Null will use the default comparer.</param>
        /// <param name="ignoreCycles">If false, an <see cref="ArgumentException"/> will be thrown when a cyclic dependancy is encountered</param>
        public static List<T> TopologicalSort<T>(IEnumerable<T> collection, IEnumerable<IDependant<T>> dependants, IEqualityComparer<T> comparer = null, bool ignoreCycles = false)
        {
            var dependancies = new Dictionary<T, IEnumerable<T>>();
            var collectionEnumerator = collection.GetEnumerator();
            var dependancyEnumerator = dependants.GetEnumerator();
            while (collectionEnumerator.MoveNext())
            {
                dependancyEnumerator.MoveNext();
                if (dependancyEnumerator.Current != null && dependancyEnumerator.Current.Dependancies != null)
                {
                    dependancies.Add(collectionEnumerator.Current, dependancyEnumerator.Current.Dependancies);
                }
            }

            return TopologicalSort(collection, delegate (T key)
            {
                dependancies.TryGetValue(key, out var value);
                return value;
            }, comparer, ignoreCycles);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts an array according to an array of dependants.
        /// If ItemA depends on ItemB, ItemA will be put later in the returned list.
        /// </summary>
        /// <param name="collection">The collection to sort. If any element depends on something that isn't present, it will be added automatically.</param>
        /// <param name="getDependancies">A delegate that can return the dependancies of any given element.</param>
        /// <param name="comparer">The equality comparer to use. Null will use the default comparer.</param>
        /// <param name="ignoreCycles">If false, an <see cref="ArgumentException"/> will be thrown when a cyclic dependancy is encountered</param>
        public static List<T> TopologicalSort<T>(IEnumerable<T> collection, Func<T, IEnumerable<T>> getDependancies, IEqualityComparer<T> comparer = null, bool ignoreCycles = false)
        {
            var sorted = WeaverUtilities.GetList<T>();
            var visiting = new Dictionary<T, bool>(comparer);

            foreach (var item in collection)
            {
                Visit(item, getDependancies, sorted, visiting, ignoreCycles);
            }

            return sorted;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts an array according to an array of dependants.
        /// If ItemA depends on ItemB, ItemA will be put later in the returned list.
        /// </summary>
        /// <param name="list">The list to sort. If any element depends on something that isn't present, it will be added automatically.</param>
        /// <param name="skip">The index at which to start sorting. Everything before this index is kept in the same order as the input list.</param>
        /// <param name="getDependancies">A delegate that can return the dependancies of any given element.</param>
        /// <param name="comparer">The equality comparer to use. Null will use the default comparer.</param>
        /// <param name="ignoreCycles">If false, an <see cref="ArgumentException"/> will be thrown when a cyclic dependancy is encountered</param>
        public static List<T> TopologicalSort<T>(List<T> list, int skip, Func<T, IEnumerable<T>> getDependancies, IEqualityComparer<T> comparer = null, bool ignoreCycles = false)
        {
            var sorted = WeaverUtilities.GetList<T>();
            var visiting = new Dictionary<T, bool>(comparer);

            for (int i = 0; i < skip; i++)
            {
                var item = list[i];
                sorted.Add(item);
                visiting.Add(item, false);
            }

            for (; skip < list.Count; skip++)
                Visit(list[skip], getDependancies, sorted, visiting, ignoreCycles);

            return sorted;
        }

        /// <summary>
        /// Sorts an array according to an array of dependants.
        /// If ItemA depends on ItemB, ItemA will be put later in the returned list.
        /// This method assigns a new list and releases the old one to the CollectionPool.
        /// </summary>
        /// <param name="list">The list to sort. If any element depends on something that isn't present, it will be added automatically.</param>
        /// <param name="skip">The index at which to start sorting. Everything before this index is kept in the same order as the input list.</param>
        /// <param name="getDependancies">A delegate that can return the dependancies of any given element.</param>
        /// <param name="comparer">The equality comparer to use. Null will use the default comparer.</param>
        /// <param name="ignoreCycles">If false, an <see cref="ArgumentException"/> will be thrown when a cyclic dependancy is encountered</param>
        public static void TopologicalSort<T>(ref List<T> list, int skip, Func<T, IEnumerable<T>> getDependancies, IEqualityComparer<T> comparer = null, bool ignoreCycles = false)
        {
            var sortedList = TopologicalSort(list, skip, getDependancies, comparer, ignoreCycles);
            list.Release();
            list = sortedList;
        }

        /************************************************************************************************************************/

        private static void Visit<T>(T item, Func<T, IEnumerable<T>> getDependancies, List<T> sorted, Dictionary<T, bool> visiting, bool ignoreCycles)
        {
            if (item == null) return;

            if (visiting.TryGetValue(item, out bool isVisiting))
            {
                if (isVisiting && !ignoreCycles)
                {
                    // If you found a cyclic dependancy, build it into a string and throw an exception.
                    var text = WeaverUtilities.GetStringBuilder();
                    text.Append("Cyclic dependancy found: ");
                    text.Append(item.ToString());

                    var dependancy = item;
                    do
                    {
                        var dependancies = getDependancies(dependancy);
                        foreach (var otherDependancy in dependancies)
                        {
                            visiting.TryGetValue(otherDependancy, out isVisiting);
                            if (isVisiting) break;
                        }

                        text.Append(" -> ");
                        text.Append(dependancy.ToString());
                    }
                    while (!visiting.Comparer.Equals(dependancy, item));

                    throw new ArgumentException(text.ReleaseToString());
                }
            }
            else
            {
                visiting[item] = true;

                var dependancies = getDependancies(item);
                if (dependancies != null)
                {
                    foreach (var dependancy in dependancies)
                    {
                        Visit(dependancy, getDependancies, sorted, visiting, ignoreCycles);
                    }
                }

                visiting[item] = false;
                sorted.Add(item);
            }
        }

        /************************************************************************************************************************/
    }
}

