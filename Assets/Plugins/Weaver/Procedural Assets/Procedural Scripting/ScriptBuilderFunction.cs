// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only]
    /// A delegate which appends some text at the specified `indent` level.
    /// </summary>
    public delegate void AppendFunction(StringBuilder text, int indent);
}

#endif

