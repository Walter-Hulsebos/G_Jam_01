// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>A strongly typed asset list.</summary>
    public interface IAssetList<T> : IEnumerable<T> where T : Object
    {
        /************************************************************************************************************************/

        /// <summary>The number of items in the list.</summary>
        int Count { get; }

        /// <summary>Gets the item at the specified `index` in the list.</summary>
        T this[int index] { get; }

        /************************************************************************************************************************/
    }

    public partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>Returns a random element from the `list`.</summary>
        public static T GetRandomElement<T>(this IAssetList<T> list) where T : Object
            => list[UnityEngine.Random.Range(0, list.Count)];

        /************************************************************************************************************************/
    }
}

