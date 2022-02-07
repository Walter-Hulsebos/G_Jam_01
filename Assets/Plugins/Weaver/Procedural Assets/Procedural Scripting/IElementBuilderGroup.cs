// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] A manager for a list of <see cref="ElementBuilder"/>s.</summary>
    public interface IElementBuilderGroup
    {
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="ScriptBuilder"/> used by this type and its members to determine
        /// <see cref="Name"/>s, as well as for #region names, #if symbols, and the messages used for
        /// obsolete members.
        /// </summary>
        ScriptBuilder ScriptBuilder { get; }

        /// <summary>The builder of the type in which this element will be declared.</summary>
        IElementBuilderGroup Parent { get; }

        /// <summary>The name of this member.</summary>
        string Name { get; }

        /// <summary>The full name of the <see cref="NamespaceBuilder"/> containing this object (or null if there isn't one).</summary>
        string Namespace { get; }

        /// <summary>
        /// The index in <see cref="ScriptBuilder.CompilationSymbols"/> of the symbol in which this element will be declared, I.E. #if SYMBOL.
        /// </summary>
        int CompilationSymbolIndex { get; set; }

        /// <summary>
        /// The index in <see cref="ScriptBuilder.Regions"/> of the region in which this element will be declared, I.E. #region Region Name.
        /// </summary>
        int RegionIndex { get; set; }

        /// <summary>
        /// Returns true if this element is associated with an existing <see cref="MemberInfo"/>.
        /// </summary>
        bool HasExistingMember { get; }

        /// <summary>The members to build in this type.</summary>
        List<ElementBuilder> Elements { get; }

        /// <summary>
        /// Appends the full name of this member, including its <see cref="Parent"/> (and any types and namespaces it
        /// is nested inside).
        /// </summary>
        void AppendFullName(StringBuilder text);

        /// <summary>
        /// Resets this element and adds it to its object pool to be reused later.
        /// </summary>
        void ReleaseToPool();

        /// <summary>
        /// Releases all elements currently in this group back to their respective pools.
        /// </summary>
        void ReleaseElementsToPool();

        /// <summary>
        /// Gets a description of this group and its elements.
        /// </summary>
        string GetDescription();

        /************************************************************************************************************************/
    }

    /// <summary>
    /// Various extension methods for <see cref="IElementBuilderGroup"/>.
    /// </summary>
    public static class ElementBuilderGroupExtensions
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ElementBuilder.ResolveNamingConflicts"/> on all <see cref="IElementBuilderGroup.Elements"/>.
        /// </summary>
        public static void ResolveMemberNamingConflicts(this IElementBuilderGroup group)
        {
            var nameToElement = WeaverUtilities.GetDictionary<string, ElementBuilder>();

            for (int i = 0; i < group.Elements.Count; i++)
            {
                group.Elements[i].ResolveNamingConflicts(nameToElement);
            }

            nameToElement.Release();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ElementBuilder.PrepareToBuild"/> on all <see cref="IElementBuilderGroup.Elements"/>.
        /// </summary>
        public static void PrepareMembersToBuild(this IElementBuilderGroup group, bool retainObsoleteMembers, ref bool shouldRebuild)
        {
            for (int i = 0; i < group.Elements.Count; i++)
            {
                group.Elements[i].PrepareToBuild(retainObsoleteMembers, ref shouldRebuild);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts the <see cref="IElementBuilderGroup.Elements"/> list using the specified `comparison` and maintaining
        /// the order of any elements with an identical comparison (unlike the standard
        /// <see cref="List{T}.Sort(Comparison{T})"/> method).
        /// <para></para>
        /// This method is also called recursively for any nested groups.
        /// </summary>
        public static void SortElements(this IElementBuilderGroup group, Comparison<ElementBuilder> comparison)
        {
            var elements = group.Elements;

            WeaverUtilities.StableInsertionSort(elements, comparison);

            var count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                if (elements[i] is IElementBuilderGroup elementGroup)
                    elementGroup.SortElements(comparison);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If all members have the same <see cref="ElementBuilder.RegionIndex"/>, this type will be given
        /// the same value and this method returns true. This method is called on its nested groups as well.
        /// </summary>
        public static void MatchElementRegion(this IElementBuilderGroup group)
        {
            var elements = group.Elements;
            var count = elements.Count;
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                if (elements[i] is IElementBuilderGroup elementGroup)
                    elementGroup.MatchElementRegion();

            var index = elements[0].RegionIndex;
            for (int i = 1; i < count; i++)
                if (elements[i].RegionIndex != index)
                    return;

            group.RegionIndex = index;

            for (int i = 0; i < count; i++)
                elements[i].RegionIndex = -1;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If all members have the same <see cref="ElementBuilder.CompilationSymbolIndex"/>, this type will be given
        /// the same value and this method returns true. This method is called on its nested groups as well.
        /// </summary>
        public static void MatchElementCompilationSymbol(this IElementBuilderGroup group)
        {
            var elements = group.Elements;
            var count = elements.Count;
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                if (elements[i] is IElementBuilderGroup elementGroup)
                    elementGroup.MatchElementCompilationSymbol();

            var index = elements[0].CompilationSymbolIndex;
            for (int i = 1; i < count; i++)
                if (elements[i].CompilationSymbolIndex != index)
                    return;

            group.CompilationSymbolIndex = index;

            for (int i = 0; i < count; i++)
                elements[i].CompilationSymbolIndex = -1;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ElementBuilder.AppendScript"/> on all <see cref="IElementBuilderGroup.Elements"/>.
        /// </summary>
        public static void AppendElements(this IElementBuilderGroup group, StringBuilder text, int indent)
        {
            if (group.Elements.Count == 0)
                return;

            var symbolCount = group.ScriptBuilder.GetCompilationSymbolCount();
            var currentSymbolIndex = int.MinValue;
            var regionCount = group.ScriptBuilder.GetRegionCount();
            var currentRegionIndex = int.MinValue;

            for (int i = 0; i < group.Elements.Count; i++)
            {
                var member = group.Elements[i];

                if (i > 0)
                {
                    text.AppendLineConst();
                    CheckEndCompilationSymbol(text, indent, i, currentSymbolIndex, member.CompilationSymbolIndex, symbolCount);
                    CheckEndRegion(text, indent, i, currentRegionIndex, member.RegionIndex, regionCount);
                }

                CheckStartRegion(group, text, indent, i, currentRegionIndex, member.RegionIndex, regionCount);
                CheckStartCompilationSymbol(group, text, indent, i, currentSymbolIndex, member.CompilationSymbolIndex, symbolCount);

                currentRegionIndex = member.RegionIndex;
                currentSymbolIndex = member.CompilationSymbolIndex;

                member.AppendScript(text, indent);
            }

            EndCompilationSymbol(text, indent, currentSymbolIndex, symbolCount);
            EndRegion(text, indent, currentRegionIndex, regionCount);
        }

        /************************************************************************************************************************/

        private static void CheckStartRegion(IElementBuilderGroup group, StringBuilder text, int indent, int memberIndex, int previousRegionIndex, int nextRegionIndex, int regionCount)
        {
            if (nextRegionIndex == previousRegionIndex)
                return;

            if (nextRegionIndex >= 0 && nextRegionIndex < regionCount)
            {
                CSharpProcedural.AppendRegion(text, indent, group.ScriptBuilder.Regions[nextRegionIndex]);
                text.AppendLineConst();
            }
        }

        private static void CheckEndRegion(StringBuilder text, int indent, int memberIndex, int previousRegionIndex, int nextRegionIndex, int regionCount)
        {
            if (nextRegionIndex == previousRegionIndex)
                return;

            if (previousRegionIndex >= 0 && previousRegionIndex < regionCount)
            {
                CSharpProcedural.AppendEndRegion(text, indent);
                text.AppendLineConst();
            }
        }

        private static void EndRegion(StringBuilder text, int indent, int currentRegionIndex, int regionCount)
        {
            if (currentRegionIndex >= 0 && currentRegionIndex < regionCount)
            {
                text.AppendLineConst();
                CSharpProcedural.AppendEndRegion(text, indent);
            }
        }

        /************************************************************************************************************************/

        private static void CheckStartCompilationSymbol(IElementBuilderGroup group, StringBuilder text, int indent, int memberIndex, int previousSymbolIndex, int nextSymbolIndex, int symbolCount)
        {
            if (nextSymbolIndex == previousSymbolIndex)
                return;

            if (nextSymbolIndex >= 0 && nextSymbolIndex < symbolCount)
            {
                text.Append("#if ").AppendLineConst(group.ScriptBuilder.CompilationSymbols[nextSymbolIndex]);
                text.AppendLineConst();
            }
        }

        private static void CheckEndCompilationSymbol(StringBuilder text, int indent, int memberIndex, int previousSymbolIndex, int nextSymbolIndex, int symbolCount)
        {
            if (nextSymbolIndex == previousSymbolIndex)
                return;

            if (previousSymbolIndex >= 0 && previousSymbolIndex < symbolCount)
            {
                text.AppendLineConst("#endif");
                text.AppendLineConst();
            }
        }

        private static void EndCompilationSymbol(StringBuilder text, int indent, int currentSymbolIndex, int symbolCount)
        {
            if (currentSymbolIndex >= 0 && currentSymbolIndex < symbolCount)
            {
                text.AppendLineConst();
                text.AppendLineConst("#endif");
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the specified `type` has no members, remove it from its declaring type, call
        /// <see cref="ElementBuilder.ReleaseToPool"/>, and set the reference to null.
        /// </summary>
        public static void ReleaseToPoolIfEmpty<T>(ref T group) where T : ElementBuilder, IElementBuilderGroup
        {
            if (group == null || group.Elements.Count > 0)
                return;

            group.Parent?.Elements.Remove(group);
            group.ReleaseToPool();
            group = null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ElementBuilder.ReleaseToPool"/> on each item in the list, then clears it.
        /// </summary>
        public static void ReleaseElementsToPool(this List<ElementBuilder> elements)
        {
            var count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                elements[i].ReleaseToPool();
            }
            elements.Clear();
        }

        /************************************************************************************************************************/
    }
}

#endif

