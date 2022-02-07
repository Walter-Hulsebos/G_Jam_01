// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] A variety of methods relating to procedurally generating C# code.</summary>
    public partial class CSharpProcedural
    {
        /************************************************************************************************************************/
        #region Common Constructs
        /************************************************************************************************************************/

        /// <summary><c>text.Indent(indent++).AppendLineConst("{");</c></summary>
        public static void OpenScope(this StringBuilder text, ref int indent)
        {
            text.Indent(indent++).AppendLineConst("{");
        }

        /// <summary><c>text.Indent(--indent).AppendLineConst("}");</c></summary>
        public static void CloseScope(this StringBuilder text, ref int indent)
        {
            text.Indent(--indent).AppendLineConst("}");
        }

        /************************************************************************************************************************/

        /// <summary>Appends closing brackets and new lines until the nestCount reaches 0.</summary>
        public static void CloseScopeFully(this StringBuilder text, int nestCount)
        {
            while (nestCount-- > 0)
                text.Indent(nestCount).AppendLineConst("}");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the opening of a for loop: for (int i = 0; i &lt; length; i++).
        /// </summary>
        public static void AppendForLoop(StringBuilder text, int indent, string length, string iterator = "i")
        {
            text.Indent(indent);
            text.Append("for (int ");
            text.Append(iterator);
            text.Append(" = 0; ");
            text.Append(iterator);
            text.Append(" < ");
            text.Append(length);
            text.Append("; ");
            text.Append(iterator);
            text.AppendLineConst("++)");
        }

        /************************************************************************************************************************/

        /// <summary>Appends "null" for classes or "default(T)" for structs.</summary>
        public static void AppendDefault<T>(StringBuilder text)
        {
            AppendDefault(text, typeof(T));
        }

        /// <summary>Appends "null" for classes or "default(type)" for structs.</summary>
        public static void AppendDefault(StringBuilder text, Type type)
        {
            if (!type.IsValueType)
            {
                text.Append("null");
            }
            else
            {
                text.Append("default(");
                text.Append(type.GetNameCS());
                text.Append(")");
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indents the text by the specified amount and appends #region regionName.
        /// </summary>
        public static void AppendRegion(StringBuilder text, int indent, string regionName)
        {
            text.Indent(indent).Append("#region ").AppendLineConst(regionName);
        }

        /// <summary>
        /// Indents the text by the specified amount and appends #endregion.
        /// </summary>
        public static void AppendEndRegion(StringBuilder text, int indent)
        {
            text.Indent(indent).AppendLineConst("#endregion");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends `comment` inside summary tags as appropriate for a C# XML comment.
        /// </summary>
        public static void AppendSingleLineXmlComment(StringBuilder text, int indent, string comment)
        {
            text.Indent(indent).Append("/// <summary>").Append(comment).AppendLineConst("</summary>");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a call to the specified `methodName`. If the method is non-public, the object is casted to the
        /// `interfaceType` to call an explicitly implemented method.
        /// </summary>
        public static void AppendInterfaceMethodCall(StringBuilder text, Type objectType, Type interfaceType, string methodName, string objectName)
        {
            var method = objectType.GetMethod(methodName, ReflectionUtilities.PublicInstanceBindings);
            if (method == null)
            {
                text.Append("((").Append(interfaceType.GetNameCS()).Append(")")
                    .Append(objectName)
                    .Append(')');
            }
            else
            {
                text.Append(objectName);
            }

            text.Append('.')
                .Append(methodName);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string representation of the `value` as a C# literal: either "true" or "false" instead of the
        /// uppercase forms returned by <see cref="bool.ToString()"/>.
        /// </summary>
        public static string ToStringCS(this bool value) => value ? "true" : "false";

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Naming
        /************************************************************************************************************************/

        /// <summary>
        /// Appends the full name of the given type with underscores instead of any characters that wouldn't be valid
        /// in a symbol name.
        /// </summary>
        public static void AppendUnderscoredFullName(StringBuilder text, Type type)
        {
            var i = text.Length;

            text.Append(type.GetNameCS());

            while (i < text.Length)
            {
                var c = text[i];

                // Replace array brackets "[]" with the word "Array".
                // If the rank is higher than 1, append it after the word.
                if (c == '[')
                {
                    var j = i;
                    var rank = 1;

                    while (true)
                    {
                        j++;
                        if (j >= text.Length)
                            goto ValidateChar;

                        c = text[j];
                        if (c == ',')
                        {
                            rank++;
                        }
                        else if (c == ']')
                        {
                            var length = text.Length;
                            text.Remove(i, j - i + 1);
                            text.Insert(i, "_Array");
                            if (rank > 1)
                                text.Insert(i + 6, rank);
                            i += text.Length - length;

                            goto NextChar;
                        }
                        else break;
                    }
                }

                ValidateChar:
                // Replace any invalid characters with underscores.
                if (!IsValidInMemberName(c))
                {
                    text[i] = '_';
                }

                NextChar:
                i++;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Checks if the specified char can be used in a C# symbol name.</summary>
        public static bool IsValidInMemberName(char c) => char.IsLetterOrDigit(c) || c == '_';

        /************************************************************************************************************************/

        /// <summary>
        /// Converts the specified string into a valid member name by skipping any invalid characters or replacing them with underscores.
        /// </summary>
        public static string ValidateMemberName(string name, bool replaceWithUnderscores = false)
        {
            return ValidateMemberName(name, 0, name.Length, replaceWithUnderscores);
        }

        /// <summary>
        /// Converts the specified sub-string into a valid member name by skipping any invalid characters or replacing them with underscores.
        /// </summary>
        public static string ValidateMemberName(string name, int startIndex, int endIndex, bool replaceWithUnderscores = false)
        {
            if (name.Length <= startIndex)
                return name;

            var text = WeaverUtilities.GetStringBuilder();

            var c = name[startIndex];
            if (c == '_')
            {
                text.Append('_');
                startIndex++;
            }
            else if (!char.IsLetter(c))
            {
                text.Append('_');
                if (replaceWithUnderscores)
                    startIndex++;
            }

            for (; startIndex < endIndex; startIndex++)
            {
                c = name[startIndex];
                if (IsValidInMemberName(c))
                    text.Append(c);
                else if (replaceWithUnderscores)
                    text.Append('_');
            }

            return text.ReleaseToString();
        }

        /************************************************************************************************************************/

        /// <summary>Appends the `literal` with escape characters inserted as necessary.</summary>
        public static void AppendStringLiteral(StringBuilder text, string literal)
        {
            if (literal == null)
            {
                text.Append("null");
                return;
            }

            text.Append('"');
            for (int i = 0; i < literal.Length; i++)
            {
                var c = literal[i];
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    default: text.Append(c); break;
                }
            }
            text.Append('"');
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Obsolete Member Declaration
        /************************************************************************************************************************/

        private static Dictionary<Type, MethodInfo>
            _CustomInitializerMethods;

        /// <summary>
        /// Tries to return a string which could be used as a C# field initializer to give the field the specified `value`.
        /// Returns null if the 'value is null or if unable to determine the appropriate field initializer.
        /// <para></para>
        /// If the value is a custom type with a static string GetInitializer(T value) method, that method will be called to determine the result.
        /// </summary>
        public static string GetInitializer(object value)
        {
            if (value == null)
                return null;

            // Common Types.
            if (value is int intValue)
                return intValue.ToString();
            if (value is float floatValue)
                return floatValue + "f";
            if (value is string stringValue)
                return '"' + stringValue + '"';

            // Uncommon Types.
            if (value is long longValue)
                return longValue + "L";
            if (value is double doubleValue)
                return doubleValue.ToString();

            // Unhandled Types.
            var type = value.GetType();

            // Check the type for a custom GetInitializer method.
            if (_CustomInitializerMethods == null)
                _CustomInitializerMethods = new Dictionary<Type, MethodInfo>();

            if (!_CustomInitializerMethods.TryGetValue(type, out var customInitializer))
            {
                const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                customInitializer = type.GetMethod("GetInitializer", Bindings, null, ReflectionUtilities.OneType(type), null);
                if (customInitializer != null && customInitializer.ReturnType != typeof(string))
                    customInitializer = null;

                _CustomInitializerMethods.Add(type, customInitializer);
            }

            if (customInitializer != null)
                return (string)customInitializer.Invoke(null, ReflectionUtilities.OneObject(value));

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a member with a signature matching the specified `member` and an [Obsolete] attribute.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, MemberInfo member, string obsoleteMessage = "", bool error = false)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    AppendObsoleteDeclaration(text, indent, member as ConstructorInfo, obsoleteMessage, error);
                    break;
                case MemberTypes.Field:
                    AppendObsoleteDeclaration(text, indent, member as FieldInfo, obsoleteMessage, error);
                    break;
                case MemberTypes.Method:
                    AppendObsoleteDeclaration(text, indent, member as MethodInfo, obsoleteMessage, error);
                    break;
                case MemberTypes.Property:
                    AppendObsoleteDeclaration(text, indent, member as PropertyInfo, obsoleteMessage, error);
                    break;
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    AppendObsoleteDeclaration(text, indent, member as Type, obsoleteMessage, error);
                    break;
                default:
                    AppendUnhandledMember(text, indent, member);
                    break;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a type with a signature matching the specified `type` and an [Obsolete] attribute.
        /// <para></para>
        /// Also calls <see cref="AppendObsoleteDeclaration(StringBuilder, int, MemberInfo, string, bool)"/> for each of the type's members.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, Type type, string obsoleteMessage = "", bool error = false)
        {
            AppendObsoleteAttribute(text, indent, obsoleteMessage, error);

            text.Indent(indent);

            // Access Modifiers.
            if (type.IsPublic || type.IsNestedPublic)
                text.Append("public ");
            else if (type.IsNestedPrivate)
                text.Append("private ");
            else if (type.IsNestedFamily)
                text.Append("protected ");
            else if (type.IsVisible)
                text.Append("internal ");
            else if (type.IsNestedFamORAssem)
                text.Append("protected internal ");

            if (type.IsAbstract)
            {
                if (type.IsSealed)
                    text.Append("static ");
                else
                    text.Append("abstract ");
            }
            else
            {
                if (type.IsSealed)
                    text.Append("sealed ");
            }

            // Type.
            if (type.IsClass)
                text.Append("class ");
            else if (type.IsValueType)
            {
                if (type.IsEnum)
                    text.Append("enum ");
                else
                    text.Append("struct ");
            }
            else if (type.IsInterface)
                text.Append("interface ");

            // Name and Generic Arguments.
            CSharp.AppendNameAndGenericArguments(text, type, CSharp.NameVerbosity.Basic);

            // Base Type.
            if (type.BaseType != typeof(object))
            {
                text.Append(" : ");
                text.Append(type.BaseType.GetNameCS(CSharp.NameVerbosity.Global));
            }

            text.AppendLineConst();

            // Members.
            text.OpenScope(ref indent);
            {
                const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

                var members = type.GetMembers(Bindings);
                for (int i = 0; i < members.Length; i++)
                {
                    if (i > 0)
                        text.AppendLineConst();

                    AppendObsoleteDeclaration(text, indent, members[i], obsoleteMessage, error);
                }
            }
            text.CloseScope(ref indent);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a constructor with a signature matching the specified `constructor` and an [Obsolete] attribute.
        /// <para></para>
        /// The body of the constructor will simply throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, ConstructorInfo constructor, string obsoleteMessage = "", bool error = false)
        {
            if (constructor.IsStatic)
            {
                text.Indent(indent).AppendLineConst("// Obsolete Static Constructor. ");
                return;
            }

            AppendObsoleteAttribute(text, indent, obsoleteMessage, error);

            text.Indent(indent);

            if (constructor.IsPublic)
                text.Append("public ");
            else if (constructor.IsPrivate)
                text.Append("private ");
            else if (constructor.IsFamily)
                text.Append("protected ");
            else if (constructor.IsAssembly)
                text.Append("internal ");
            else if (constructor.IsFamilyAndAssembly)
                text.Append("protected internal ");

            CSharp.AppendNameAndGenericArguments(text, constructor.DeclaringType, CSharp.NameVerbosity.Basic, int.MaxValue);
            text.Append('(');
            CSharp.AppendParameters(text, constructor.GetParameters(), true, CSharp.NameVerbosity.Global);
            text.Append(')');
            AppendObsoleteMethodBody(text, constructor, obsoleteMessage);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a field with a signature matching the specified `field` and an [Obsolete] attribute.
        /// <para></para>
        /// If the field is static and <see cref="GetInitializer(object)"/> is able to determine the initializer for
        /// its current value, that initializer will also be included.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, FieldInfo field, string obsoleteMessage = "", bool error = false)
        {
            AppendObsoleteAttribute(text, indent, obsoleteMessage, error);

            text.Indent(indent);

            if (field.IsPublic)
                text.Append("public ");
            else if (field.IsPrivate)
                text.Append("private ");
            else if (field.IsFamily)
                text.Append("protected ");
            else if (field.IsAssembly)
                text.Append("internal ");

            if (field.IsStatic)
            {
                if (field.IsLiteral)
                {
                    text.Append("const ");
                }
                else
                {
                    text.Append("static ");

                    if (field.IsInitOnly)
                        text.Append("readonly ");
                }
            }
            else
            {
                if (field.IsInitOnly)
                    text.Append("readonly ");
            }

            text.Append(field.FieldType.GetNameCS(CSharp.NameVerbosity.Global));
            text.Append(' ');
            text.Append(field.Name);

            if (field.IsStatic)
            {
                var initializer = GetInitializer(field.GetValue(null));
                if (initializer != null)
                    text.Append(" = ").Append(initializer);
            }

            text.AppendLineConst(";");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a property with a signature matching the specified `property` and an [Obsolete] attribute.
        /// <para></para>
        /// The body of the property will simply throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, PropertyInfo property, string obsoleteMessage = "", bool error = false)
        {
            var getter = property.GetGetMethod(true);
            var setter = property.GetSetMethod(true);

            AccessModifiers
                getterRestrictiveness,
                setterRestrictiveness,
                leastRestrictive;

            bool isStatic;

            if (getter != null)
            {
                isStatic = getter.IsStatic;
                getterRestrictiveness = GetRestrictivenessModifier(getter);

                if (setter != null)
                {
                    setterRestrictiveness = GetRestrictivenessModifier(setter);
                    leastRestrictive = getterRestrictiveness < setterRestrictiveness ? getterRestrictiveness : setterRestrictiveness;
                }
                else
                {
                    setterRestrictiveness = AccessModifiers.None;
                    leastRestrictive = getterRestrictiveness;
                }
            }
            else
            {
                isStatic = setter.IsStatic;
                getterRestrictiveness = AccessModifiers.None;
                setterRestrictiveness = GetRestrictivenessModifier(setter);
                leastRestrictive = setterRestrictiveness;
            }

            AppendObsoleteAttribute(text, indent, obsoleteMessage, error);

            text.Indent(indent);
            leastRestrictive.AppendDeclaration(text);
            if (isStatic)
                text.Append("static ");

            text.Append(property.PropertyType.GetNameCS(CSharp.NameVerbosity.Global));
            text.Append(' ');
            text.AppendLineConst(property.Name);
            text.OpenScope(ref indent);
            {
                if (getter != null)
                {
                    text.Indent(indent);

                    if (getterRestrictiveness != leastRestrictive)
                        getterRestrictiveness.AppendDeclaration(text);

                    text.Append("get");
                    AppendObsoleteMethodBody(text, getter, obsoleteMessage);
                }

                if (setter != null)
                {
                    text.Indent(indent);

                    if (setterRestrictiveness != leastRestrictive)
                        setterRestrictiveness.AppendDeclaration(text);

                    text.Append("set");
                    AppendObsoleteMethodBody(text, setter, obsoleteMessage);
                }
            }
            text.CloseScope(ref indent);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of a method with a signature matching the specified `method` and an [Obsolete] attribute.
        /// <para></para>
        /// The body of the method will simply throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public static void AppendObsoleteDeclaration(StringBuilder text, int indent, MethodInfo method, string obsoleteMessage = "", bool error = false)
        {
            if (method.IsSpecialName)
            {
                text.Indent(indent).Append("// Obsolete Special Method: ").AppendLineConst(method);
                return;
            }

            AppendObsoleteAttribute(text, indent, obsoleteMessage, error);

            text.Indent(indent);

            if (method.IsPublic)
                text.Append("public ");
            else if (method.IsPrivate)
                text.Append("private ");
            else if (method.IsFamily)
                text.Append("protected ");
            else if (method.IsAssembly)
                text.Append("internal ");

            if (method.IsStatic)
                text.Append("static ");
            else if (method.IsVirtual)
                text.Append("virtual ");
            else if (method.IsAbstract)
                text.Append("abstract ");

            CSharp.AppendSignature(method, text, CSharp.NameVerbosity.Global, true, false, true, true);
            AppendObsoleteMethodBody(text, method, obsoleteMessage);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends an [<see cref="ObsoleteAttribute"/>] with the specified message in its constructor.
        /// <para></para>
        /// If `message` is null, this method does nothing.
        /// </summary>
        public static void AppendObsoleteAttribute(StringBuilder text, int indent, string message = "", bool error = false)
        {
            if (message == null)
                return;

            text.Indent(indent).Append("[System.Obsolete(");

            if (message != "")
            {
                text.Append('"').Append(message).Append('"');

                if (error)
                {
                    text.Append(", true");
                }
            }

            text.AppendLineConst(")]");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends and logs a message indicating that the specified `member` was not handled.
        /// </summary>
        public static void AppendUnhandledMember(StringBuilder text, int indent, MemberInfo member)
        {
            text.Indent(indent).Append("// Unhandled Member: ").AppendLineConst(member);
            Debug.LogWarning("Unhandled Member: " + member);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a method body which throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public static void AppendObsoleteMethodBody(StringBuilder text, MemberInfo member, string obsoleteMessage = null)
        {
            text.Append(" { throw new System.NotImplementedException(\"").Append(member.GetNameCS()).Append(" is obsolete.");
            if (!string.IsNullOrEmpty(obsoleteMessage))
                text.Append(' ').Append(obsoleteMessage);

            text.AppendLineConst("\"); }");
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

