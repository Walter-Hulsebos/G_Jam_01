// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

//#define LOG_NAMING_CONFLICT_DETAILS

using System;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Base class for building a particular type of member in a procedural C# script.</summary>
    public abstract class MemberBuilder : ElementBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="MemberInfo"/> of the member with the same type and name as this builder.
        /// This property is gathered during <see cref="PrepareToBuild(bool, ref bool)"/>.
        /// </summary>
        public abstract MemberInfo ExistingMember { get; }

        /// <summary>
        /// Returns true if this element is associated with an existing <see cref="MemberInfo"/>.
        /// </summary>
        public override bool HasExistingMember => ExistingMember != null;

        /************************************************************************************************************************/

        /// <summary>The access modifiers of this member.</summary>
        public virtual AccessModifiers Modifiers { get; set; }

        /// <summary>The default access modifiers for this member.</summary>
        public readonly AccessModifiers DefaultModifiers;

        /************************************************************************************************************************/

        /// <summary>The custom attribute types for this member.</summary>
        public Type[] Attributes { get; set; }

        /// <summary>Sets the <see cref="Attributes"/> array.</summary>
        public void SetAttributes(params Type[] attributes)
        {
            Attributes = attributes;
        }

        /// <summary>The methods used to build the constructor of each custom attribute for this member.</summary>
        public Action<StringBuilder>[] AttributeConstructorBuilders { get; set; }

        /// <summary>Sets the <see cref="AttributeConstructorBuilders"/> array.</summary>
        public void SetAttributeConstructorBuilders(params Action<StringBuilder>[] attributeConstructorBuilders)
        {
            AttributeConstructorBuilders = attributeConstructorBuilders;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation and Pooling
        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="MemberBuilder"/> with default values.
        /// <para></para>
        /// Consider using one of the overloads of Get instead, in order to utilise object pooling to minimise memory
        /// allocation and garbage collection.
        /// </summary>
        protected MemberBuilder(AccessModifiers defaultModifiers = AccessModifiers.Public | AccessModifiers.Static)
        {
            Modifiers = DefaultModifiers = defaultModifiers;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Reset all of the fields and properties of this member to their default values.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            Modifiers = DefaultModifiers;
            Attributes = null;
            AttributeConstructorBuilders = null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building
        /************************************************************************************************************************/

        internal override void PrepareToBuild(bool retainObsoleteMembers, ref bool shouldRebuild)
        {
            if (ExistingMember == null)
            {
                Parent.ScriptBuilder.LogRebuildReason("a new member should be added: " + this);
                shouldRebuild = true;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the <see cref="ElementBuilder.Name"/> of this member matches the name of the `existingMember`.
        /// <para></para>
        /// Overrides of this method should check other factors to ensure that the existing member matches this builder
        /// (such as field type and access modifiers) and if so, cache the member so it can be returned by <see cref="ExistingMember"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            return
                ExistingMember == null &&
                Name == existingMember.Name;
        }

        /************************************************************************************************************************/

        // Needs to account for [Obsolete] attributes.
        ///// <summary>
        ///// Returns true if the <see cref="ExistingMember"/> has custom attributes with the same types as the
        ///// <see cref="Attributes"/> array.
        ///// </summary>
        //public bool HasCorrectAttributes()
        //{
        //    var attributes = ExistingMember.GetCustomAttributes(true);
        //    if (Attributes == null)
        //        return attributes.Length == 0;

        //    if (attributes.Length != Attributes.Length)
        //        return false;

        //    for (int i = 0; i < attributes.Length; i++)
        //    {
        //        if (attributes[i].GetType() != Attributes[i])
        //            return false;
        //    }

        //    return true;
        //}

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a C# XML comment using the <see cref="ElementBuilder.CommentBuilder"/> followed by any
        /// <see cref="Attributes"/>.
        /// </summary>
        protected override void AppendHeader(StringBuilder text, int indent)
        {
            base.AppendHeader(text, indent);

            if (Attributes != null)
            {
                for (int i = 0; i < Attributes.Length; i++)
                {
                    text.Indent(indent)
                        .Append('[')
                        .Append(Attributes[i].GetNameCS());

                    if (AttributeConstructorBuilders != null)
                    {
                        text.Append('(');
                        AttributeConstructorBuilders[i](text);
                        text.Append(')');
                    }

                    text.AppendLineConst("]");
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

