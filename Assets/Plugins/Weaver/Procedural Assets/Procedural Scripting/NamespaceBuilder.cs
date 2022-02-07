// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Manages the details for building a namespace in a procedural C# script.</summary>
    public class NamespaceBuilder : ElementBuilder, IElementBuilderGroup
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>
        /// The members to build in this type, including <see cref="TypeBuilder"/>s for any nested types.
        /// </summary>
        public readonly List<ElementBuilder> Elements = new List<ElementBuilder>();

        List<ElementBuilder> IElementBuilderGroup.Elements => Elements;

        /************************************************************************************************************************/

        /// <summary>Namespaces are not members, so this property returns <see cref="MemberTypes.Custom"/>.</summary>
        public override MemberTypes MemberType => MemberTypes.Custom;

        /// <summary>Namespaces are not generally allowed to have comments.</summary>
        protected override Action<StringBuilder> GetDefaultCommentBuilder() => null;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string Namespace => Name;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<NamespaceBuilder> Pool = new List<NamespaceBuilder>();

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="NamespaceBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static NamespaceBuilder Get(ScriptBuilder scriptBuilder, string nameSource)
        {
            var nameSpace = Pool.PopLastOrCreate();
            nameSpace.InitializeRootType(scriptBuilder, nameSource);
            return nameSpace;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="NamespaceBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static NamespaceBuilder Get(IElementBuilderGroup parent, string nameSource)
        {
            var type = Pool.PopLastOrCreate();
            type.Initialize(parent, nameSource);
            return type;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns false because namespaces don't have any <see cref="MemberInfo"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            return false;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            ReleaseElementsToPool();
        }

        /// <inheritdoc/>
        public override void ReleaseToPool()
        {
            Reset();
            Pool.Add(this);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Releases all elements currently in this namespace back to their respective pools.
        /// </summary>
        public void ReleaseElementsToPool()
        {
            Elements.ReleaseElementsToPool();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Reset this type and call <see cref="ElementBuilder.ReleaseToPool"/> on all its members so that it can be
        /// reused without releasing this type itself to the pool.
        /// </summary>
        public void PrepareForReuse()
        {
            if (IsFallbackName)
            {
                DetermineMemberName(ScriptBuilder);
            }

            Reset();

            ReleaseElementsToPool();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Creation
        /************************************************************************************************************************/

        /// <summary>
        /// Adds the specified `member` to the <see cref="Elements"/> list and returns it.
        /// </summary>
        public T AddMember<T>(T member) where T : ElementBuilder
        {
            Elements.Add(member);
            return member;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="ConstructorBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this namespace.
        /// </summary>
        public NamespaceBuilder AddNamespace(string nameSource)
        {
            return AddMember(NamespaceBuilder.Get(this, nameSource));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this namespace.
        /// </summary>
        public TypeBuilder AddType(string nameSource, CachedTypeInfo existingType)
        {
            return AddMember(TypeBuilder.Get(this, nameSource, existingType));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this namespace.
        /// </summary>
        public TypeBuilder AddType(string nameSource, Type existingType)
        {
            return AddMember(TypeBuilder.Get(this, nameSource, existingType));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this namespace.
        /// </summary>
        public TypeBuilder AddType(string nameSource)
        {
            return AddMember(TypeBuilder.Get(this, nameSource));
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building
        /************************************************************************************************************************/

        internal override void PrepareToBuild(bool retainObsoleteMembers, ref bool shouldRebuild)
        {
            this.ResolveMemberNamingConflicts();
            this.PrepareMembersToBuild(retainObsoleteMembers, ref shouldRebuild);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the declaration of this namespace in C# code to the specified `text`.
        /// </summary>
        public override void AppendScript(StringBuilder text, int indent)
        {
#if WEAVER_DEBUG
            UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder().Indent(indent).Append("Building ").Append(this).ReleaseToString());
#endif

            if (Name == null)
            {
                this.AppendElements(text, indent);
            }
            else
            {
                AppendHeader(text, indent);

                text.Indent(indent).Append("namespace ").AppendLineConst(Name);

                text.OpenScope(ref indent);
                {
                    this.AppendElements(text, indent);
                }
                text.CloseScope(ref indent);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a description of this namespace and its <see cref="Elements"/>.
        /// </summary>
        public override void AppendDescription(StringBuilder text, int indent)
        {
            base.AppendDescription(text, indent);

            indent++;
            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].AppendDescription(text, indent);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

