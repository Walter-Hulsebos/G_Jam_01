// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only]
    /// Manages the details for building a constructor in a procedural C# script.
    /// </summary>
    public class ConstructorBuilder : MemberBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>This method's parameters. Corresponds to the <see cref="MethodBase.GetParameters"/>.</summary>
        public ParameterBuilder[] Parameters { get; set; }

        /// <summary>The delegate used to build the body of this method.</summary>
        public AppendFunction BodyBuilder { get; set; }

        /// <summary>
        /// Appends the parameters passed into the base constructor, I.E.
        /// "... : base(<see cref="AppendBaseParameters"/>)".
        /// </summary>
        /// <remarks>This value is not considered when determining if the script needs to be regenerated.</remarks>
        public Action<StringBuilder> AppendBaseParameters { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="ConstructorInfo"/> of the method with the same parameters as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public ConstructorInfo ExistingConstructor { get; private set; }

        /// <summary>
        /// The <see cref="ConstructorInfo"/> of the method with the same parameters as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public override MemberInfo ExistingMember => ExistingConstructor;

        /************************************************************************************************************************/

        /// <summary>This is a <see cref="MemberTypes.Constructor"/>.</summary>
        public override MemberTypes MemberType => MemberTypes.Constructor;

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the default method to use to build XML comments for this member. Called once by the constructor.
        /// </summary>
        protected override Action<StringBuilder> GetDefaultCommentBuilder()
        {
            return (comment) => comment.Append("Creates a new <see cref=\"").Append(Parent.Name).Append("\"/>.");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="ConstructorBuilder"/> with default values.
        /// <para></para>
        /// Consider using one of the overloads of Get instead, in order to utilise object pooling to minimise memory
        /// allocation and garbage collection.
        /// </summary>
        public ConstructorBuilder()
            : base(AccessModifiers.Public)
        { }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<ConstructorBuilder> Pool = new List<ConstructorBuilder>();

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="ConstructorBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static ConstructorBuilder Get(TypeBuilder declaringType,
            ParameterBuilder[] parameters, AppendFunction bodyBuilder)
        {
            var constructor = Pool.PopLastOrCreate();
            constructor.Initialize(declaringType, GetName(parameters));
            constructor.Parameters = parameters;
            constructor.BodyBuilder = bodyBuilder;
            return constructor;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="ConstructorBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static ConstructorBuilder Get(TypeBuilder declaringType, AppendFunction bodyBuilder)
        {
            var constructor = Pool.PopLastOrCreate();
            constructor.Initialize(declaringType, GetName(null));
            constructor.BodyBuilder = bodyBuilder;
            return constructor;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            ExistingConstructor = null;
            Parameters = null;
            BodyBuilder = null;
        }

        /// <inheritdoc/>
        public override void ReleaseToPool()
        {
            Reset();
            Pool.Add(this);
        }

        /************************************************************************************************************************/

        private static string GetName(ParameterBuilder[] parameters)
        {
            if (parameters.IsNullOrEmpty())
                return "ctor";

            var name = WeaverUtilities.GetStringBuilder();
            name.Append("ctor_");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    name.Append('_');

                name.Append(CSharpProcedural.ValidateMemberName(parameters[i].Type.FullName, true));
            }
            return name.ReleaseToString();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the <see cref="ElementBuilder.Name"/>, <see cref="MemberBuilder.Modifiers"/>, and parameters match
        /// the `existingMember`.
        /// <para></para>
        /// If the member matches, this method returns true and the member can be accessed via <see cref="ExistingMember"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            if (ExistingMember != null)
                return false;

            ExistingConstructor = existingMember as ConstructorInfo;
            if (ExistingConstructor == null)
                return false;

            var parameters = ExistingConstructor.GetParameters();
            if (!ParameterBuilder.AreParametersSame(parameters, Parameters))
            {
                ExistingConstructor = null;
                return false;
            }

            if (!CSharpProcedural.HasModifiers(ExistingConstructor, Modifiers))
            {
                Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " is not a " + Modifiers.GetDeclaration() + " constructor.");
                shouldRebuild = true;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the declaration of this method in C# code to the specified `text`.
        /// </summary>
        public override void AppendScript(StringBuilder text, int indent)
        {
#if WEAVER_DEBUG
            UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder().Indent(indent).Append("Building ").Append(this).ReleaseToString());
#endif

            AppendHeader(text, indent);

            text.Indent(indent);
            Modifiers.AppendDeclaration(text);
            text.Append(Parent.Name)
                .Append('(');

            ParameterBuilder.AppendDeclaration(text, Parameters);

            text.Append(')');

            if (AppendBaseParameters != null)
            {
                text.AppendLineConst()
                    .Indent(indent + 1)
                    .Append(": base(");
                AppendBaseParameters(text);
                text.Append(')');
            }

            MethodBuilder.AppendBody(text, indent, BodyBuilder);
        }

        /************************************************************************************************************************/
    }
}

#endif

