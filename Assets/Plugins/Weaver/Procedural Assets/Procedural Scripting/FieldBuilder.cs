// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Manages the details for building a field in a procedural C# script.</summary>
    public class FieldBuilder : MemberBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The type of object this field holds. Corresponds to <see cref="FieldInfo.FieldType"/>.</summary>
        public TypeName FieldType { get; private set; }

        /// <summary>The initial value of the field.</summary>
        public object Value { get; private set; }

        /// <summary>
        /// Used to check if the specified <see cref="object"/> is equal to the <see cref="Value"/> of this field when
        /// determining if the script needs to be rebuilt. If null, the check will be skipped.
        /// </summary>
        public Func<object, bool> ValueEquals { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// A delegate used to append the initializer for a field.
        /// </summary>
        public delegate void AppendInitializerMethod(StringBuilder text, int indent, object value);

        /// <summary>
        /// A method which takes the following parameters and appends an appropriate field initializer:
        /// (<see cref="StringBuilder"/> text, <see cref="int"/> indent, <see cref="object"/> value).
        /// <para></para>
        /// By default, <see cref="CSharpProcedural.GetInitializer(object)"/> will be used.
        /// <para></para>
        /// Note that this delegate is called immediately after the field name and before the semicolon, so it must
        /// begin with " = " to assign a value and should not append a semicolon at the end.
        /// </summary>
        public AppendInitializerMethod AppendInitializer { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="FieldInfo"/> of the field with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public FieldInfo ExistingField { get; private set; }

        /// <summary>
        /// The <see cref="FieldInfo"/> of the field with the same type and name as this builder.
        /// This property is gathered by <see cref="IsExistingMember(MemberInfo, ref bool)"/>.
        /// </summary>
        public override MemberInfo ExistingMember => ExistingField;

        /************************************************************************************************************************/

        /// <summary>This is a <see cref="MemberTypes.Field"/>.</summary>
        public override MemberTypes MemberType => MemberTypes.Field;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly List<FieldBuilder> Pool = new List<FieldBuilder>();

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static FieldBuilder Get(TypeBuilder declaringType, string nameSource, TypeName fieldType)
        {
            var field = Pool.PopLastOrCreate();
            field.Initialize(declaringType, nameSource);
            field.FieldType = fieldType;
            return field;
        }

        /// <summary>
        /// Returns a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static FieldBuilder Get(TypeBuilder declaringType, string nameSource, Type fieldType)
            => Get(declaringType, nameSource, new TypeName(fieldType));

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a <see cref="FieldBuilder"/> from the object pool and initialize it with the specified parameters.
        /// </summary>
        public static FieldBuilder Get<T>(TypeBuilder declaringType, string nameSource, T value)
        {
            var field = Get(declaringType, nameSource, new TypeName(typeof(T)));
            field.Value = value;
            return field;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="FieldBuilder"/> with the default values.
        /// <para></para>
        /// Consider using one of the overloads of Get instead, in order to utilise object pooling to minimise memory
        /// allocation and garbage collection.
        /// </summary>
        public FieldBuilder()
            : base(AccessModifiers.Public)
        {
            ValueEquals = DefaultValueEquals;
            AppendInitializer = DefaultAppendInitializer;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();
            ExistingField = null;
            ValueEquals = DefaultValueEquals;
            AppendInitializer = DefaultAppendInitializer;
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
        /// The default delegate assigned to <see cref="AppendInitializer"/>.
        /// It simply calls <see cref="CSharpProcedural.GetInitializer"/> and appends the result if it isn't null.
        /// </summary>
        public static readonly AppendInitializerMethod
            DefaultAppendInitializer = (text, indent, value) =>
            {
                var initializer = CSharpProcedural.GetInitializer(value);
                if (initializer != null)
                    text.Append(" = ").Append(initializer);
            };

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the <see cref="ElementBuilder.Name"/>, <see cref="MemberBuilder.Modifiers"/>,
        /// <see cref="FieldType"/>, and <see cref="Value"/> match the `existingMember`.
        /// <para></para>
        /// If the member matches, this method returns true and the member can be accessed via <see cref="ExistingMember"/>.
        /// </summary>
        public override bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild)
        {
            if (!base.IsExistingMember(existingMember, ref shouldRebuild))
                return false;

            ExistingField = existingMember as FieldInfo;

            if (ExistingField == null || !CSharpProcedural.HasModifiers(ExistingField, Modifiers))
            {
                Parent.ScriptBuilder.LogRebuildReason(ExistingField.GetNameCS() + " is not a " + Modifiers.GetDeclaration() + " field.");
                shouldRebuild = true;
            }
            else if (ExistingField.FieldType != FieldType)
            {
                Parent.ScriptBuilder.LogRebuildReason($"{ExistingField.GetNameCS()} is not a {FieldType.FullName} field.");
                shouldRebuild = true;
            }
            else if (ValueEquals != null && ExistingField.IsStatic)
            {
                var value = ExistingField.GetValue(null);
                if (!ValueEquals(value))
                {
                    Parent.ScriptBuilder.LogRebuildReason("the value of " + ExistingField.GetNameCS() + " is not correct: '" + value + "' should be '" + Value + "'");
                    shouldRebuild = true;
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Uses <see cref="object.Equals(object, object)"/> to determine if the <see cref="Value"/> is equal to <paramref name="other"/>.
        /// </summary>
        public bool DefaultValueEquals(object other)
        {
            return Equals(Value, other);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the declaration of this field in C# code to the specified `text`.
        /// </summary>
        public override void AppendScript(StringBuilder text, int indent)
        {
#if WEAVER_DEBUG
            UnityEngine.Debug.Log(WeaverUtilities.GetStringBuilder().Indent(indent).Append("Building ").Append(this).ReleaseToString());
#endif

            AppendHeader(text, indent);

            text.Indent(indent);
            Modifiers.AppendDeclaration(text);
            FieldType.AppendFullName(text);
            text.Append(' ').Append(Name);

            AppendInitializer?.Invoke(text, indent, Value);

            text.AppendLineConst(";");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a summary of this field including its type, <see cref="ElementBuilder.NameSource"/>,
        /// <see cref="ElementBuilder.Name"/>, <see cref="ElementBuilder.FullName"/>, and <see cref="Value"/>.
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + ", Value=" + Value;
        }

        /************************************************************************************************************************/
    }
}

#endif

