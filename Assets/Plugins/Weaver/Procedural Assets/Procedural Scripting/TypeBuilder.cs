// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Manages the details for building a type in a procedural C# script.</summary>
    public class TypeBuilder : MemberBuilder, IElementBuilderGroup
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

        /// <summary>
        /// The <see cref="TypeBuilder"/> which builds the type in which this type is nested (or null if this isn't nested).
        /// </summary>
        public TypeBuilder DeclaringType { get; private set; }

        /// <summary>
        /// During <see cref="PrepareToBuild"/> this type will compare all members in the existing
        /// <see cref="Type"/> with the <see cref="Elements"/> to be built, and any that don't match up are kept in this
        /// list so they can be re-implemented as stubs and marked with the [<see cref="ObsoleteAttribute"/>] in order
        /// to avoid causing compile errors when members are removed or renamed when the script it rebuilt.
        /// <para></para>
        /// This list will be null if no obsolete members are found or if the `retainObsoleteMembers` parameter in
        /// <see cref="PrepareToBuild"/> is false.
        /// </summary>
        public List<MemberInfo> ObsoleteMembers { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="CachedTypeInfo"/> of the type with the same type and name as this builder.
        /// </summary>
        public CachedTypeInfo ExistingType { get; private set; }

        /// <summary>
        /// The <see cref="Type"/> of the type with the same type and name as this builder.
        /// </summary>
        public override MemberInfo ExistingMember => ExistingType?.Type;

        /************************************************************************************************************************/

        /// <summary>
        /// This builder is a <see cref="MemberTypes.TypeInfo"/> if it has no <see cref="ElementBuilder.Parent"/>,
        /// otherwise it is a <see cref="MemberTypes.NestedType"/>.
        /// </summary>
        public override MemberTypes MemberType => Parent == null ? MemberTypes.TypeInfo : MemberTypes.NestedType;

        /************************************************************************************************************************/

        /// <summary>The <see cref="Type.BaseType"/> of the type being built.</summary>
        public Type BaseType { get; set; }

        /// <summary>The <see cref="TypeBuilder"/> that will build the <see cref="BaseType"/>.</summary>
        /// <remarks>Not used if the <see cref="BaseType"/> is set.</remarks>
        public TypeBuilder BaseTypeBuilder { get; set; }

        /// <summary>Appends the name of the <see cref="Type.BaseType"/>.</summary>
        /// <remarks>Not used if the <see cref="BaseType"/> or <see cref="BaseTypeBuilder"/> are set.</remarks>
        public Action<StringBuilder> AppendBaseType { get; set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<TypeBuilder> Pool = new List<TypeBuilder>();

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static TypeBuilder Get(ScriptBuilder scriptBuilder, string nameSource)
        {
            var type = Pool.PopLastOrCreate();
            type.InitializeRootType(scriptBuilder, nameSource);
            return type;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static TypeBuilder Get(IElementBuilderGroup parent, string nameSource)
        {
            var type = Pool.PopLastOrCreate();
            type.DeclaringType = parent as TypeBuilder;
            type.Initialize(parent, nameSource);
            return type;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static TypeBuilder Get(IElementBuilderGroup parent, string nameSource, CachedTypeInfo existingType)
        {
            var type = Get(parent, nameSource);
            type.ExistingType = existingType;
            return type;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static TypeBuilder Get(IElementBuilderGroup parent, string nameSource, Type existingType)
        {
            var type = Get(parent, nameSource);
            if (existingType != null)
                type.ExistingType = CachedTypeInfo.Get(existingType);
            return type;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static TypeBuilder Get(TypeBuilder declaringType, string nameSource)
        {
            var type = Pool.PopLastOrCreate();
            type.DeclaringType = declaringType;
            type.Initialize(declaringType, nameSource);
            return type;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            DeclaringType = null;
            ExistingType = null;
            BaseType = null;
            BaseTypeBuilder = null;
            AppendBaseType = null;
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
        /// Releases all elements currently in this type back to their respective pools.
        /// </summary>
        public void ReleaseElementsToPool()
        {
            Elements.ReleaseElementsToPool();

            if (ObsoleteMembers != null)
            {
                ObsoleteMembers.Release();
                ObsoleteMembers = null;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Creation
        /************************************************************************************************************************/

        /// <summary>
        /// Adds the specified `member` to the <see cref="Elements"/> list and returns it.
        /// </summary>
        public T AddMember<T>(T member) where T : MemberBuilder
        {
            Elements.Add(member);
            return member;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="ConstructorBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public ConstructorBuilder AddConstructor(AppendFunction bodyBuilder)
            => AddMember(ConstructorBuilder.Get(this, bodyBuilder));

        /// <summary>
        /// Get a <see cref="ConstructorBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public ConstructorBuilder AddConstructor(ParameterBuilder[] parameters, AppendFunction bodyBuilder)
            => AddMember(ConstructorBuilder.Get(this, parameters, bodyBuilder));

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public FieldBuilder AddField(string nameSource, Type fieldType)
            => AddMember(FieldBuilder.Get(this, nameSource, fieldType));

        /// <summary>
        /// Get a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public FieldBuilder AddField(string nameSource, TypeName fieldType)
            => AddMember(FieldBuilder.Get(this, nameSource, fieldType));

        /// <summary>
        /// Get a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public FieldBuilder AddField<T>(string nameSource, T value)
            => AddMember(FieldBuilder.Get<T>(this, nameSource, value));

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="PropertyBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public PropertyBuilder AddProperty(string nameSource, TypeName propertyType,
            AppendFunction getterBuilder, AppendFunction setterBuilder)
            => AddMember(PropertyBuilder.Get(this, nameSource, propertyType, getterBuilder, setterBuilder));

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public MethodBuilder AddMethod(string nameSource, AppendFunction bodyBuilder)
            => AddMember(MethodBuilder.Get(this, nameSource, bodyBuilder));

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public MethodBuilder AddMethod(string nameSource, TypeName returnType, AppendFunction bodyBuilder)
            => AddMember(MethodBuilder.Get(this, nameSource, returnType, bodyBuilder));

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public MethodBuilder AddMethod(string nameSource, TypeName returnType, ParameterBuilder[] parameters,
            AppendFunction bodyBuilder)
            => AddMember(MethodBuilder.Get(this, nameSource, returnType, parameters, bodyBuilder));

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="TypeBuilder"/> from the object pool and initialize it with the specified parameters as a
        /// member of this type.
        /// </summary>
        public TypeBuilder AddNestedType(string nameSource)
            => AddMember(TypeBuilder.Get(this, nameSource));

        /************************************************************************************************************************/

        /// <summary>
        /// Creates (if necessary) and returns a <see cref="TypeBuilder"/> such that each directory in the specified
        /// `filePath` corresponds to a nested type.
        /// </summary>
        public TypeBuilder GetOrAddNestedTypesForDirectories(string filePath)
            => GetOrAddNestedTypesForDirectories(new Substring(filePath));

        /// <summary>
        /// Creates (if necessary) and returns a <see cref="TypeBuilder"/> such that each directory in the specified
        /// `filePath` corresponds to a nested type.
        /// </summary>
        public TypeBuilder GetOrAddNestedTypesForDirectories(Substring filePath)
            => GetOrAddNestedTypesForDirectories(filePath, filePath.startIndex);

        /// <summary>
        /// Creates (if necessary) and returns a <see cref="TypeBuilder"/> such that each directory in the specified
        /// `filePath` corresponds to a nested type.
        /// </summary>
        public TypeBuilder GetOrAddNestedTypesForDirectories(Substring filePath, int commentStartIndex)
        {
            // Move the name to contain the first directory.
            filePath.endIndex = filePath.rawString.IndexOf('/', filePath.startIndex);

            // If there is no directory, return the root type.
            if (filePath.endIndex < 0)
            {
                filePath.endIndex = filePath.rawString.Length;
                return this;
            }

            var type = this;

            // If the first directory is this type, skip it.
            if (filePath == type.NameSource)
                filePath.MoveToNextDirectory();

            // Move through the path, finding or creating a nested type for each directory.
            while (!filePath.IsAtEnd)
            {
                type = type.FindOrCreateNestedType(filePath, commentStartIndex);
                if (!filePath.MoveToNextDirectory())
                    break;
            }

            return type;
        }

        /************************************************************************************************************************/

        private TypeBuilder FindOrCreateNestedType(Substring name, int commentStartIndex)
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                var member = Elements[i];
                if (member.MemberType == MemberTypes.NestedType &&
                    name == member.NameSource)
                {
                    return member as TypeBuilder;
                }
            }

            // Not found, create a new nested type.

            var type = AddNestedType(name);

            var namePathEnd = name.endIndex;
            type.CommentBuilder = (text) =>
            {
                text.Append(name.rawString.Substring(commentStartIndex, namePathEnd - commentStartIndex));
            };

            return type;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Build Preparation
        /************************************************************************************************************************/

        internal override void PrepareToBuild(bool retainObsoleteMembers, ref bool shouldRebuild)
        {
            this.ResolveMemberNamingConflicts();

            if (ExistingType == null)
            {
                ScriptBuilder.LogRebuildReason($"a new Type should be added: {this}");
                shouldRebuild = true;
            }
            else
            {
                VerifyBaseType(ref shouldRebuild);
            }

            MatchMembersWithExistingInfo(retainObsoleteMembers, ref shouldRebuild);

            this.PrepareMembersToBuild(retainObsoleteMembers, ref shouldRebuild);
        }

        /************************************************************************************************************************/

        private void VerifyBaseType(ref bool shouldRebuild)
        {
            Type baseType;
            if (BaseType != null)
            {
                baseType = BaseType;
            }
            else if (BaseTypeBuilder == null)
            {
                baseType = typeof(object);
            }
            else
            {
                if (BaseTypeBuilder != null && ExistingType.Type.BaseType.FullName != BaseTypeBuilder.FullName)
                {
                    ScriptBuilder.LogRebuildReason($"a Type has the wrong BaseType: {this}" +
                        $" ({ExistingType.Type.BaseType.GetNameCS()} should be {BaseTypeBuilder.FullName})");
                    shouldRebuild = true;
                }

                return;
            }

            if (ExistingType.Type.BaseType != baseType)
            {
                ScriptBuilder.LogRebuildReason($"a Type has the wrong BaseType: {this}" +
                    $" ({ExistingType.Type.BaseType.GetNameCS()} should be {baseType.GetNameCS()})");
                shouldRebuild = true;
            }
        }

        /************************************************************************************************************************/

        private void MatchMembersWithExistingInfo(bool retainObsoleteMembers, ref bool shouldRebuild)
        {
            if (ExistingType == null && DeclaringType == null)
                ExistingType = CachedTypeInfo.FindExistingType(Name, FullName, ref shouldRebuild);

            if (ExistingType == null || ExistingType.Members == null)
                return;

            if (retainObsoleteMembers)
                ObsoleteMembers = WeaverUtilities.GetList<MemberInfo>();

            for (int i = 0; i < ExistingType.Members.Count; i++)
            {
                var member = ExistingType.Members[i];
                var wasObsolete = member.IsObsolete();

                if (!retainObsoleteMembers && wasObsolete)
                {
                    shouldRebuild = true;
                    ScriptBuilder.LogRebuildReason(
                        $"{member.GetNameCS()} is marked as [Obsolete] while {nameof(retainObsoleteMembers)} is false.");
                }

                for (int j = 0; j < Elements.Count; j++)
                {
                    var memberBuilder = Elements[j];
                    if (memberBuilder.IsExistingMember(member, ref shouldRebuild))
                    {
                        if (wasObsolete)
                        {
                            shouldRebuild = true;
                            ScriptBuilder.LogRebuildReason(member.GetNameCS() + " is no longer [Obsolete].");
                        }

                        goto NextMember;
                    }
                }

                // Default constructors and static constructors are never obsolete.
                if (member is ConstructorInfo constructor &&
                    (constructor.IsPublic || constructor.IsStatic) &&
                    constructor.GetParameters().Length == 0)
                    goto NextMember;

                if (retainObsoleteMembers)
                {
                    ObsoleteMembers.Add(member);
                    if (!wasObsolete)
                    {
                        shouldRebuild = true;
                        ScriptBuilder.LogRebuildReason(member.GetNameCS() + " no longer exists, but isn't marked as [Obsolete].");
                    }
                }
                else
                {
                    shouldRebuild = true;
                    ScriptBuilder.LogRebuildReason(member.GetNameCS() + " is now [Obsolete].");
                }

                NextMember:
                continue;
            }

            if (retainObsoleteMembers && ObsoleteMembers.Count == 0)
            {
                ObsoleteMembers.Release();
                ObsoleteMembers = null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the <see cref="ElementBuilder.Name"/> and <see cref="MemberBuilder.Modifiers"/> match the `existingMember`.
        /// <para></para>
        /// If the member matches, this method returns true and the member can be accessed via <see cref="ExistingType"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            if (ExistingMember == existingMember)
                return true;

            if (Name != existingMember.Name)
                return false;

            var type = existingMember as Type;

            if (type == null || !CSharpProcedural.HasModifiers(type, Modifiers))
            {
                ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " is not a " + Modifiers.GetDeclaration() + " type.");
                shouldRebuild = true;

                if (type == null)
                    return true;
            }

            ExistingType = CachedTypeInfo.Get(type);
            return true;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building
        /************************************************************************************************************************/

        /// <summary>
        /// Appends the declaration of this type in C# code to the specified `text`.
        /// </summary>
        public override void AppendScript(StringBuilder text, int indent)
        {
#if WEAVER_DEBUG
            UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder().Indent(indent).Append("Building ").Append(this).ReleaseToString());
#endif

            AppendHeader(text, indent);

            text.Indent(indent);
            Modifiers.AppendDeclaration(text);
            text.Append("class ")
                .Append(Name);

            // Base Type.
            if (BaseType != null && BaseType != typeof(object))
                text.Append(" : ").Append(BaseType.GetNameCS());
            else if (BaseTypeBuilder != null)
                text.Append(" : ").Append(BaseTypeBuilder.FullName);
            else if (AppendBaseType != null)
            {
                text.Append(" : ");
                AppendBaseType(text);
            }

            text.AppendLineConst();

            text.OpenScope(ref indent);
            {
                this.AppendElements(text, indent);
                AppendObsoleteMembers(text, indent);
            }
            text.CloseScope(ref indent);
        }

        /************************************************************************************************************************/

        private void AppendObsoleteMembers(StringBuilder text, int indent)
        {
            if (ObsoleteMembers == null)
                return;

            if (Elements.Count > 0)
                text.AppendLineConst();

            CSharpProcedural.AppendRegion(text, indent, "Obsolete Members");
            if (ScriptBuilder.ObsoleteMembersAreEditorOnly)
                text.AppendLineConst("#if " + WeaverUtilities.UnityEditor);

            ObsoleteMembers.Sort(CompareMembers);
            for (int i = 0; i < ObsoleteMembers.Count; i++)
            {
#if WEAVER_DEBUG
                UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder()
                    .Indent(indent).Append("Rebuilding Obsolete Member: ").Append(ObsoleteMembers[i]).ReleaseToString());
#endif

                text.AppendLineConst();
                CSharpProcedural.AppendObsoleteDeclaration(text, indent, ObsoleteMembers[i], ScriptBuilder.ObsoleteAttributeMessage);
            }

            text.AppendLineConst();
            if (ScriptBuilder.ObsoleteMembersAreEditorOnly)
                text.AppendLineConst("#endif");
            CSharpProcedural.AppendEndRegion(text, indent);
        }

        /************************************************************************************************************************/

        private static readonly Comparison<MemberInfo> CompareMembers = (a, b) =>
        {
            // Unfortunately this puts methods before properties.

            var result = a.MemberType.CompareTo(b.MemberType);
            if (result != 0)
                return result;

            return a.Name.CompareTo(b.Name);
        };

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a description of this type and its <see cref="Elements"/>.
        /// </summary>
        public override void AppendDescription(StringBuilder text, int indent)
        {
            base.AppendDescription(text, indent);

            indent++;
            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].AppendDescription(text, indent);
            }

            if (ObsoleteMembers != null)
            {
                for (int i = 0; i < ObsoleteMembers.Count; i++)
                {
                    text.Indent(indent).Append("Obsolete Member: ").AppendLineConst(ObsoleteMembers[i]);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

