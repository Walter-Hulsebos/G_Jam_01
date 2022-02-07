// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Manages the details for building a property in a procedural C# script.</summary>
    public class PropertyBuilder : MemberBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The type of object this property represents. Corresponds to <see cref="PropertyInfo.PropertyType"/>.</summary>
        public TypeName PropertyType { get; set; }

        /// <summary>The access modifiers of this property's getter. Matches those assigned to the property itself by default.</summary>
        public AccessModifiers GetterModifiers { get; set; }

        /// <summary>The access modifiers of this property's setter. Matches those assigned to the property itself by default.</summary>
        public AccessModifiers SetterModifiers { get; set; }

        /// <summary>The delegate used to build this property's getter. If null, this property has no getter.</summary>
        public AppendFunction GetterBuilder { get; set; }

        /// <summary>The delegate used to build this property's setter. If null, this property has no setter.</summary>
        public AppendFunction SetterBuilder { get; set; }

        /// <summary>
        /// If assigned, this delegate is used by <see cref="IsExistingMember(MemberInfo, ref bool)"/> to check the
        /// return value of the existing property's getter. Returning false indicates that the value is wrong and the
        /// script should be regenerated.
        /// </summary>
        public Func<object, bool> ReturnValueEquals { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="PropertyInfo"/> of the property with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public PropertyInfo ExistingProperty { get; private set; }

        /// <summary>
        /// The <see cref="PropertyInfo"/> of the property with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public override MemberInfo ExistingMember => ExistingProperty;

        /************************************************************************************************************************/

        /// <summary>This is a <see cref="MemberTypes.Property"/>.</summary>
        public override MemberTypes MemberType => MemberTypes.Property;

        /************************************************************************************************************************/

        private AccessModifiers _Modifiers;

        /// <summary>
        /// The access modifiers of this property. Setting this value applies to both the getter and setter.
        /// </summary>
        public override AccessModifiers Modifiers
        {
            get => _Modifiers;
            set => _Modifiers = GetterModifiers = SetterModifiers = value;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<PropertyBuilder> Pool = new List<PropertyBuilder>();

        /************************************************************************************************************************/

        /// <summary>Gets a <see cref="PropertyBuilder"/> from the object pool and initializes it.</summary>
        public static PropertyBuilder Get(TypeBuilder declaringType, string nameSource, TypeName propertyType,
            AppendFunction getterBuilder, AppendFunction setterBuilder)
        {
            var property = Pool.PopLastOrCreate();
            property.Initialize(declaringType, nameSource);
            property.PropertyType = propertyType;
            property.GetterBuilder = getterBuilder;
            property.SetterBuilder = setterBuilder;
            return property;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            ReturnValueEquals = null;
            ExistingProperty = null;
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

        /// <inheritdoc/>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            if (!base.IsExistingMember(existingMember, ref shouldRebuild))
                return false;

            ExistingProperty = existingMember as PropertyInfo;

            if (ExistingProperty == null)
            {
                Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " is not a property.");
                shouldRebuild = true;
            }
            else if (ExistingProperty.PropertyType != PropertyType)
            {
                Parent.ScriptBuilder.LogRebuildReason($"{existingMember.GetNameCS()} isn't a {PropertyType.FullName} property.");
                shouldRebuild = true;
            }
            else
            {
                var method = ExistingProperty.GetGetMethod(true);
                if (method == null)
                {
                    if (GetterBuilder != null)
                    {
                        Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " has no getter.");
                        shouldRebuild = true;
                    }
                }
                else if (!CSharpProcedural.HasModifiers(method, GetterModifiers))
                {
                    Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " getter is not: " + GetterModifiers.GetDeclaration());
                    shouldRebuild = true;
                }
                else if (ReturnValueEquals != null)
                {
                    try
                    {
                        var returnedValue = method.Invoke(null, null);
                        if (!ReturnValueEquals(returnedValue))
                        {
                            Parent.ScriptBuilder.LogRebuildReason("the return value of " + ExistingProperty.GetNameCS() + " is not correct.");
                            shouldRebuild = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                method = ExistingProperty.GetSetMethod(true);
                if (method == null)
                {
                    if (SetterBuilder != null)
                    {
                        Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " has no setter.");
                        shouldRebuild = true;
                    }
                }
                else if (!CSharpProcedural.HasModifiers(method, SetterModifiers))
                {
                    Parent.ScriptBuilder.LogRebuildReason(existingMember.GetNameCS() + " setter is not: " + SetterModifiers.GetDeclaration());
                    shouldRebuild = true;
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void AppendScript(StringBuilder text, int indent)
        {
#if WEAVER_DEBUG
            UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder().Indent(indent).Append("Building ").Append(this).ReleaseToString());
#endif

            AppendHeader(text, indent);

            text.Indent(indent);
            Modifiers.AppendDeclaration(text);
            PropertyType.AppendFullName(text);
            text.Append(' ').AppendLineConst(Name);
            text.OpenScope(ref indent);
            {
                if (GetterBuilder != null)
                {
                    text.Indent(indent);

                    if (GetterModifiers != Modifiers)
                        GetterModifiers.AppendDeclaration(text);

                    text.Append("get");
                    MethodBuilder.AppendBody(text, indent, GetterBuilder);
                }

                if (SetterBuilder != null)
                {
                    text.Indent(indent);

                    if (SetterModifiers != Modifiers)
                        SetterModifiers.AppendDeclaration(text);

                    text.Append("set");
                    MethodBuilder.AppendBody(text, indent, SetterBuilder);
                }
            }
            text.CloseScope(ref indent);
        }

        /************************************************************************************************************************/
    }
}

#endif

