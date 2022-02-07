// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

//#define UNIT_TEST

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Weaver
{
    /// <summary>A variety of methods relating to C# code.</summary>
    public static partial class CSharp
    {
        /************************************************************************************************************************/

#if UNITY_EDITOR
        static CSharp()
        {
            WeaverUtilities.LogIfRestricted(nameof(CSharp));
        }
#endif

        /************************************************************************************************************************/
        #region Type Keywords
        /************************************************************************************************************************/

        private static Dictionary<Type, string> _TypeKeywords;

        private static void InitializeTypeKeywords()
        {
            if (_TypeKeywords != null)
                return;

            _TypeKeywords = new Dictionary<Type, string>
            {
                { typeof(void), "void" },
                { typeof(object), "object" },
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(char), "char" },
                { typeof(string), "string" },
                { typeof(short), "short" },
                { typeof(int), "int" },
                { typeof(long), "long" },
                { typeof(ushort), "ushort" },
                { typeof(uint), "uint" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
            };
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the specified `type` is associated with a C# keyword such as <see cref="int"/>, <see cref="float"/>, <see cref="string"/>, etc.</summary>
        public static bool HasKeyword(Type type)
        {
            InitializeTypeKeywords();
            return _TypeKeywords.ContainsKey(type);
        }

        /// <summary>
        /// Returns the C# keyword associated with the specified `type` such as <see cref="int"/>, <see cref="float"/>, <see cref="string"/>, etc.
        /// Returns null if there is no associated keyword.
        /// </summary>
        public static string GetKeyword(Type type)
        {
            InitializeTypeKeywords();
            _TypeKeywords.TryGetValue(type, out var keyword);
            return keyword;
        }

        /************************************************************************************************************************/

        private static HashSet<string> _ReservedKeywords;

        /// <summary>
        /// Returns true if the `word` is reserved by the C# language.
        /// </summary>
        public static bool IsReservedKeyword(string word)
        {
            if (_ReservedKeywords == null)
            {
                _ReservedKeywords = new HashSet<string>
                {
                    "abstract",
                    "as",
                    "base",
                    "bool",
                    "break",
                    "byte",
                    "case",
                    "catch",
                    "char",
                    "checked",
                    "class",
                    "const",
                    "continue",
                    "decimal",
                    "default",
                    "delegate",
                    "do",
                    "double",
                    "else",
                    "enum",
                    "event",
                    "explicit",
                    "extern",
                    "false",
                    "finally",
                    "fixed",
                    "float",
                    "for",
                    "foreach",
                    "goto",
                    "if",
                    "implicit",
                    "in",
                    "int",
                    "interface",
                    "internal",
                    "is",
                    "lock",
                    "long",
                    "namespace",
                    "new",
                    "null",
                    "object",
                    "operator",
                    "out",
                    "override",
                    "params",
                    "private",
                    "protected",
                    "public",
                    "readonly",
                    "ref",
                    "return",
                    "sbyte",
                    "sealed",
                    "short",
                    "sizeof",
                    "stackalloc",
                    "static",
                    "string",
                    "struct",
                    "switch",
                    "this",
                    "throw",
                    "true",
                    "try",
                    "typeof",
                    "uint",
                    "ulong",
                    "unchecked",
                    "unsafe",
                    "ushort",
                    "using",
                    "static",
                    "void",
                    "volatile",
                    "while"
                };
            }

            return _ReservedKeywords.Contains(word);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Type Names
        /************************************************************************************************************************/

        /// <summary>Specifies how detailed the name returned by <see cref="GetNameCS(Type, NameVerbosity)"/> should be.</summary>
        public enum NameVerbosity
        {
            /// <summary>Similar to <see cref="MemberInfo.Name"/>.</summary>
            Basic,

            /// <summary>Similar to <see cref="Basic"/> but includes nested types.</summary>
            Nested,

            /// <summary>Similar to <see cref="Type.FullName"/>.</summary>
            Full,

            /// <summary>Similar to <see cref="Full"/>, with the "global::" prefix.</summary>
            Global,
        }

        private const int NameVerbosityCount = 4;

        /************************************************************************************************************************/

        private static Dictionary<Type, string>[] _TypeToName;

        private static Dictionary<Type, string> GetTypeToName(NameVerbosity verbosity)
        {
            if (_TypeToName == null)
                _TypeToName = new Dictionary<Type, string>[NameVerbosityCount];

            var index = (int)verbosity;
            var names = _TypeToName[index];
            if (names == null)
            {
                InitializeTypeKeywords();
                names = new Dictionary<Type, string>(_TypeKeywords);
                _TypeToName[index] = names;
            }
            return names;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the name of a `type` as it would appear in C# code. Results are cached.</summary>
        /// <example>
        /// <c>typeof(List&lt;float&gt;).FullName</c> would give:
        /// <c>System.Collections.Generic.List`1[[System.Single, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]</c>
        /// This method would instead return a different result depending on the specified `verbosity`:
        /// <list type="bullet">
        /// <item><see cref="NameVerbosity.Basic"/> would return <c>"List&lt;float&gt;"</c>.</item>
        /// <item><see cref="NameVerbosity.Nested"/> is like <see cref="NameVerbosity.Basic"/> but also includes nested types.</item>
        /// <item><see cref="NameVerbosity.Full"/> would return <c>"System.Collections.Generic.List&lt;float&gt;"</c>.</item>
        /// <item><see cref="NameVerbosity.Global"/> would return <c>"global::System.Collections.Generic.List&lt;float&gt;"</c>.</item>
        /// </list>
        /// </example>
        public static string GetNameCS(this Type type, NameVerbosity verbosity = NameVerbosity.Full)
        {
            if (type == null)
                return "";

            // Check if we have already got the name for that type.
            var names = GetTypeToName(verbosity);
            if (names.TryGetValue(type, out string name))
                return name;

            var text = WeaverUtilities.GetStringBuilder();

            if (type.IsArray)// Array = TypeName[].
            {
                var element = type;
                while (true)
                {
                    element = element.GetElementType();
                    if (element.IsArray)
                        continue;

                    text.Append(element.GetNameCS(verbosity));
                    break;
                }

                element = type;
                while (element.IsArray)
                {
                    text.Append('[');
                    var dimensions = element.GetArrayRank();
                    while (dimensions-- > 1)
                        text.Append(',');
                    text.Append(']');

                    element = element.GetElementType();
                }

                goto Return;
            }

            if (type.IsPointer)// Pointer = TypeName*.
            {
                text.Append(type.GetElementType().GetNameCS(verbosity));
                text.Append('*');

                goto Return;
            }

            if (type.IsGenericParameter)// Generic Parameter = TypeName (for unspecified generic parameters).
            {
                text.Append(type.Name);
                goto Return;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)// Nullable = TypeName?.
            {
                text.Append(underlyingType.GetNameCS(verbosity));
                text.Append('?');

                goto Return;
            }

            // Other Type = Namespace.NestedTypes.TypeName<GenericArguments>.

            if (verbosity == NameVerbosity.Global && !HasKeyword(type))// Global prefix.
            {
                text.Append("global::");
            }

            if (verbosity > NameVerbosity.Nested && type.Namespace != null)// Namespace.
            {
                text.Append(type.Namespace);
                text.Append('.');
            }

            var skipGenericArguments = 0;
            var arguments = type.GetGenericArguments();

            if (type.DeclaringType != null)// Account for Nested Types.
            {
                if (verbosity > NameVerbosity.Basic)
                {
                    // Count the nesting level.
                    var nesting = 1;
                    var declaringType = type.DeclaringType;
                    while (declaringType.DeclaringType != null)
                    {
                        declaringType = declaringType.DeclaringType;
                        nesting++;
                    }

                    // Append the name of each outer type, starting from the outside.
                    // For each nesting level starting from the outside, walk out to it from the specified 'type'.
                    // This avoids the need to make a list of types in the nest or to insert type names instead of appending them.
                    while (nesting-- > 0)
                    {
                        declaringType = type;
                        for (int i = nesting; i >= 0; i--)
                            declaringType = declaringType.DeclaringType;

                        // Nested Type Name and Generic Arguments.
                        skipGenericArguments = AppendNameAndGenericArguments(text, declaringType, arguments, verbosity, skipGenericArguments);
                        text.Append('.');
                    }
                }
                else if (type.IsGenericType)
                {
                    skipGenericArguments = type.DeclaringType.GetGenericArguments().Length;
                }
            }

            // Type Name and Generic Arguments.
            AppendNameAndGenericArguments(text, type, arguments, verbosity, skipGenericArguments);

            Return:// Cache and return the name.

            name = text.ReleaseToString();
            names.Add(type, name);
            return name;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the name and generic arguments of `type` (after skipping the specified number).
        /// Returns the index of the last argument.
        /// </summary>
        public static int AppendNameAndGenericArguments(StringBuilder text, Type type, Type[] arguments,
            NameVerbosity verbosity = NameVerbosity.Full,
            int skipGenericArguments = 0)
        {
            var name = type.Name;
            text.Append(name);

            if (!type.IsGenericType ||
                skipGenericArguments >= arguments.Length)
                goto Return;

            var backQuote = name.LastIndexOf('`');
            if (backQuote < 0)
                goto Return;

            text.Length -= name.Length - backQuote;

            text.Append('<');

            var argumentCount = type.GetGenericArguments().Length;
            var firstArgument = arguments[skipGenericArguments];
            skipGenericArguments++;

            if (firstArgument.IsGenericParameter)
            {
                while (skipGenericArguments < argumentCount)
                {
                    text.Append(',');
                    skipGenericArguments++;
                }
            }
            else
            {
                text.Append(firstArgument.GetNameCS(verbosity));

                while (skipGenericArguments < argumentCount)
                {
                    text.Append(", ");
                    text.Append(arguments[skipGenericArguments].GetNameCS(verbosity));
                    skipGenericArguments++;
                }
            }

            text.Append('>');

            Return:
            return skipGenericArguments;
        }

        /// <summary>
        /// Appends the name and generic arguments of `type` (after skipping the specified number).
        /// Returns the index of the last argument.
        /// </summary>
        public static int AppendNameAndGenericArguments(StringBuilder text, Type type,
            NameVerbosity verbosity = NameVerbosity.Full,
            int skipGenericArguments = 0)
            => AppendNameAndGenericArguments(text, type, type.GetGenericArguments(), verbosity, skipGenericArguments);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Names
        /************************************************************************************************************************/

        /// <summary>
        /// Returns the full name of a `member` as it would appear in C# code.
        /// <para></para>
        /// For example, passing the <see cref="MethodInfo"/> of this method in as its own parameter would return "<see cref="CSharp"/>.GetNameCS".
        /// <para></para>
        /// Note that when `member` is a <see cref="Type"/>, this method calls <see cref="GetNameCS(Type, NameVerbosity)"/> instead.
        /// </summary>
        public static string GetNameCS(this MemberInfo member, NameVerbosity declaringTypeNameVerbosity = NameVerbosity.Full)
        {
            if (member == null)
                return "null";

            if (member is Type type)
                return type.GetNameCS(declaringTypeNameVerbosity);

            // Check if we have already got the name for that member.
            var names = GetMemberToName(declaringTypeNameVerbosity);
            if (names.TryGetValue(member, out string name))
                return name;

            // Otherwise build a new one.
            var text = WeaverUtilities.GetStringBuilder();

            if (member.DeclaringType != null)
            {
                text.Append(member.DeclaringType.GetNameCS(declaringTypeNameVerbosity));
                text.Append('.');
            }

            text.Append(member.Name);

            name = text.ReleaseToString();
            names.Add(member, name);
            return name;
        }

        /************************************************************************************************************************/

        private static Dictionary<MemberInfo, string>[] _MemberToName;

        private static Dictionary<MemberInfo, string> GetMemberToName(NameVerbosity declaringTypeNameVerbosity)
        {
            if (_MemberToName == null)
                _MemberToName = new Dictionary<MemberInfo, string>[NameVerbosityCount];

            var index = (int)declaringTypeNameVerbosity;
            var names = _MemberToName[index];
            if (names == null)
            {
                names = new Dictionary<MemberInfo, string>();
                _MemberToName[index] = names;
            }
            return names;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Compares the declaring type namespaces, then declaring type names, then member names.
        /// </summary>
        public static int CompareNamespaceTypeMember(MemberInfo a, MemberInfo b)
        {
            var result = string.Compare(a.DeclaringType.Namespace, b.DeclaringType.Namespace);
            if (result != 0)
                return result;

            result = string.Compare(a.DeclaringType.FullName, b.DeclaringType.FullName);
            if (result != 0)
                return result;

            return string.Compare(a.Name, b.Name);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Friendly Method Names
        /************************************************************************************************************************/

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS(Delegate method)
        {
            return GetNameCS(method.Method);
        }

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS(Action method)
        {
            return GetNameCS(method.Method);
        }

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS<T>(Action<T> method)
        {
            return GetNameCS(method.Method);
        }

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS<TResult>(Func<TResult> method)
        {
            return GetNameCS(method.Method);
        }

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS<T, TResult>(Func<T, TResult> method)
        {
            return GetNameCS(method.Method);
        }

        /// <summary>Returns the full name of a  `method` as it would appear in C# code. See: <see cref="GetNameCS(MemberInfo, NameVerbosity)"/>.</summary>
        public static string GetNameCS<T1, T2, TResult>(Func<T1, T2, TResult> method)
        {
            return GetNameCS(method.Method);
        }

        /************************************************************************************************************************/

        /// <summary>Appends the signature of `method` as it would appear in C# code.</summary>
        public static void AppendSignature(MethodInfo method, StringBuilder text, NameVerbosity verbosity = NameVerbosity.Full,
            bool returnType = true, bool declaringType = true, bool parameterTypes = true, bool parameterDetails = true)
        {
            if (method == null)
            {
                text.Append("null");
                return;
            }

            if (returnType)
            {
                text.Append(method.ReturnType.GetNameCS(verbosity));
                text.Append(' ');
            }

            if (declaringType)
            {
                text.Append(method.DeclaringType.GetNameCS(verbosity));
                text.Append('.');
            }

            text.Append(method.Name);

            if (method.IsGenericMethod)
            {
                text.Append('<');

                var genericArguments = method.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    text.Append(genericArguments[i].GetNameCS(verbosity));
                    text.Append(", ");
                }

                text.Length -= 2;
                text.Append('>');
            }

            text.Append('(');
            {
                if (parameterTypes)
                {
                    if (method.IsDefined(typeof(ExtensionAttribute), false))
                        text.Append("this ");

                    AppendParameters(text, method.GetParameters(), parameterDetails);
                }
            }
            text.Append(')');
        }

        /// <summary>Returns the signature of `method` as it would appear in C# code.</summary>
        public static string GetSignature(this MethodInfo method, NameVerbosity verbosity = NameVerbosity.Full,
            bool returnType = true, bool declaringType = true, bool parameters = true, bool parameterDetails = true)
        {
            if (method == null)
                return "null";

            var text = WeaverUtilities.GetStringBuilder();
            AppendSignature(method, text, verbosity, returnType, declaringType, parameters, parameterDetails);
            return text.ReleaseToString();
        }

        /************************************************************************************************************************/

        /// <summary>Appends the signature of a method with the specified details as it would appear in C# code.</summary>
        public static void AppendMethodSignature(StringBuilder text,
            Type returnType, Type declaringType, string methodName, Type[] genericArguments, Type[] parameterTypes, NameVerbosity verbosity = NameVerbosity.Full)
        {
            if (returnType != null)
            {
                text.Append(returnType.Name);
                text.Append(' ');
            }

            if (declaringType != null)
            {
                text.Append(declaringType.GetNameCS(verbosity));
                text.Append('.');
            }

            text.Append(methodName);

            if (genericArguments != null)
            {
                text.Append('<');

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0)
                        text.Append(", ");

                    text.Append(genericArguments[i].GetNameCS(verbosity));
                }

                text.Append('>');
            }

            text.Append('(');
            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (i > 0)
                        text.Append(", ");

                    text.Append(parameterTypes[i].GetNameCS(verbosity));
                }
            }
            text.Append(')');
        }

        /************************************************************************************************************************/

        /// <summary>Appends the signature of `method` as it would appear in C# code.</summary>
        public static void AppendParameters(StringBuilder text, ParameterInfo[] parameters, bool parameterDetails = true, NameVerbosity verbosity = NameVerbosity.Full)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    text.Append(", ");

                var parameter = parameters[i];

                if (parameter.IsOut)
                    text.Append("out ");
                else if (parameter.ParameterType.IsByRef)
                    text.Append("ref ");

                text.Append(parameter.ParameterType.GetNameCS(verbosity));

                if (parameterDetails)
                {
                    text.Append(' ');
                    text.Append(parameter.Name);

                    if (parameter.IsOptional)
                    {
                    }
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

