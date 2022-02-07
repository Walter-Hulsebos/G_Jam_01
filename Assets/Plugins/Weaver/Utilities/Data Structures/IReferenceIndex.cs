// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Collections.Generic;

namespace Weaver
{
    /// <summary>
    /// An object that references a particular index in another list.
    /// </summary>
    public interface IReferenceIndex
    {
        /// <summary>The index being referenced.</summary>
        int ReferencedIndex { get; set; }
    }

    public partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns the first element in the `list` which has its <see cref="IReferenceIndex.ReferencedIndex"/> == `index`.
        /// </summary>
        public static T GetReferenceTo<T>(List<T> list, int index) where T : class, IReferenceIndex
        {
            if (list == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.ReferencedIndex == index)
                    return item;
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Elements in the specified `list` reference specific indices in another list.
        /// Call this method when removing an element from the other list to adjust the
        /// <see cref="IReferenceIndex.ReferencedIndex"/> of the elements in this `list` accordingly.
        /// </summary>
        public static void OnReferenceRemoved<T>(List<T> list, int index) where T : class, IReferenceIndex
        {
            if (list == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item.ReferencedIndex >= index)
                {
                    if (item.ReferencedIndex == index)
                        list.RemoveAt(i);
                    else
                        item.ReferencedIndex--;
                }
            }
        }

        /************************************************************************************************************************/
    }
}

