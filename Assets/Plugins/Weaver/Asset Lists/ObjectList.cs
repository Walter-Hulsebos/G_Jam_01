// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>An <see cref="AssetList"/> for any <see cref="Object"/>.</summary>
    [CreateAssetMenu(menuName = "Asset List", order = 30)]
    public sealed class ObjectList : AssetList<Object> { }
}

