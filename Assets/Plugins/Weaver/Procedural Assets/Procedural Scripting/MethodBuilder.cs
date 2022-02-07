// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Manages the details for building a method in a procedural C# script.</summary>
    public class MethodBuilder : MemberBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The type of object this method returns. Corresponds to <see cref="MethodInfo.ReturnType"/>.</summary>
        public TypeName ReturnType { get; set; }

        /************************************************************************************************************************/

        /// <summary>This method's parameters. Corresponds to the <see cref="MethodBase.GetParameters"/>.</summary>
        public ParameterBuilder[] Parameters { get; set; }

        /// <summary>
        /// This delegate is used to build the body of this method.
        /// </summary>
        public AppendFunction BodyBuilder { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="MethodInfo"/> of the method with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public MethodInfo ExistingMethod { get; private set; }

        /// <summary>
        /// The <see cref="MethodInfo"/> of the method with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public override MemberInfo ExistingMember => ExistingMethod;

        /************************************************************************************************************************/

        /// <summary>This is a <see cref="MemberTypes.Method"/>.</summary>
        public override MemberTypes MemberType => MemberTypes.Method;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<MethodBuilder> Pool = new List<MethodBuilder>();

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static MethodBuilder Get(TypeBuilder declaringType, string nameSource, TypeName returnType,
            ParameterBuilder[] parameters, AppendFunction bodyBuilder)
        {
            var method = Pool.PopLastOrCreate();
            method.Initialize(declaringType, nameSource);
            method.ReturnType = returnType;
            method.Parameters = parameters;
            method.BodyBuilder = bodyBuilder;
            return method;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static MethodBuilder Get(TypeBuilder declaringType, string nameSource, TypeName returnType, AppendFunction bodyBuilder)
        {
            var method = Pool.PopLastOrCreate();
            method.Initialize(declaringType, nameSource);
            method.ReturnType = returnType;
            method.BodyBuilder = bodyBuilder;
            return method;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Get a <see cref="MethodBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static MethodBuilder Get(TypeBuilder declaringType, string nameSource, AppendFunction bodyBuilder)
        {
            return Get(declaringType, nameSource, typeof(void), bodyBuilder);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            ExistingMethod = null;
            ReturnType = default;
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
        #endregion
        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the <see cref="ElementBuilder.Name"/>, <see cref="MemberBuilder.Modifiers"/>,
        /// <see cref="ReturnType"/>, and parameters match the `existingMember`.
        /// <para></para>
        /// If the member matches, this method returns true and the member can be accessed via <see cref="ExistingMember"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            if (!base.IsExistingMember(existingMember, ref shouldRebuild))
                return false;

            ExistingMethod = existingMember as MethodInfo;

            if (ExistingMethod == null || !CSharpProcedural.HasModifiers(ExistingMethod, Modifiers))
            {
                Parent.ScriptBuilder.LogRebuildReason($"{existingMember.GetNameCS()} is not a {Modifiers.GetDeclaration()} method.");
                shouldRebuild = true;
            }
            else if (ExistingMethod.ReturnType != ReturnType)
            {
                Parent.ScriptBuilder.LogRebuildReason($"{existingMember.GetNameCS()} is doesn't return {ReturnType}.");
                shouldRebuild = true;
            }
            else
            {
                var parameters = ExistingMethod.GetParameters();

                if (!ParameterBuilder.AreParametersSame(parameters, Parameters))
                {
                    Parent.ScriptBuilder.LogRebuildReason($"{existingMember.GetNameCS()} parameters have changed.");
                    shouldRebuild = true;
                }
                else if (!Parameters.IsNullOrEmpty())
                {
                    var buildExtension = Parameters[0].Modifier == ParameterModifier.This;
                    if (buildExtension != ExistingMethod.IsDefined(typeof(ExtensionAttribute), true))
                    {
                        Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() +
                            (buildExtension ? " should be an extension method." : " shouldn't be an extension method."));
                        shouldRebuild = true;
                    }
                }
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
            text.Append(ReturnType)
                .Append(' ')
                .Append(Name)
                .Append('(');

            ParameterBuilder.AppendDeclaration(text, Parameters);

            text.Append(')');
            AppendBody(text, indent, BodyBuilder);
        }

        /************************************************************************************************************************/

        /// <summary>Appends a method body.</summary>
        /// <remarks>
        /// If the body starts with whitespace, it is surrounded by braces and given multiple lines:
        /// <para></para><code>
        /// ... Method(...)
        /// {
        /// Body
        /// }
        /// </code>
        /// Otherwise it is appended as an expression-bodied method:
        /// <para></para><code>
        /// ... Method(...) => Body;
        /// </code>
        /// </remarks>
        public static void AppendBody(StringBuilder text, int indent, AppendFunction appendBody)
        {
            if (appendBody == null)
            {
                text.AppendLineConst(" { }");
                return;
            }

            var start = text.Length;
            appendBody(text, indent + 1);

            if (text.Length == start)
            {
                text.AppendLineConst(" { }");
                return;
            }

            var firstCharacter = text[start];
            if (char.IsWhiteSpace(firstCharacter) || firstCharacter == '#')
            {
                text.Insert(start, WeaverUtilities.NewLine);
                text.Insert(start, '{');
                for (int i = 0; i < indent; i++)
                    text.Insert(start, WeaverUtilities.Tab);
                text.Insert(start, WeaverUtilities.NewLine);

                if (text[text.Length - 1] != '\n')
                    text.AppendLineConst();

                indent++;
                text.CloseScope(ref indent);
            }
            else
            {
                text.Insert(start, " => ");

                if (text[text.Length - 1] != '\n')
                    text.AppendLineConst();
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

