// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>A keyword that can go before a method parameter.</summary>
    public enum ParameterModifier
    {
        None,
        In,
        Out,
        Ref,
        Params,
        This,
    }

    public static partial class CSharpProcedural
    {
        /************************************************************************************************************************/

        /// <summary>Gets the C# keyword associated with the `modifier`.</summary>
        public static string GetKeyword(this ParameterModifier modifier)
        {
            switch (modifier)
            {
                case ParameterModifier.None: return null;
                case ParameterModifier.In: return "in";
                case ParameterModifier.Out: return "out";
                case ParameterModifier.Ref: return "ref";
                case ParameterModifier.Params: return "params";
                case ParameterModifier.This: return "this";
                default: throw new ArgumentException($"Unsupported {nameof(ParameterModifier)}: {modifier}");
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

