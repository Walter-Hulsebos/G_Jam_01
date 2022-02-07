// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Text;
using UnityEngine;

namespace Weaver
{
    /// <summary>A variety of utility methods relating to reflection.</summary>
    public static class ReflectionUtilities
    {
        /************************************************************************************************************************/
        #region Assemblies
        /************************************************************************************************************************/

        /// <summary>A variety of utility methods relating to the currently loaded assemblies and iterating through them.</summary>
        [SecurityCritical]// For AppDomain.AssemblyLoad.
        public static class Assemblies
        {
            /************************************************************************************************************************/

            /// <summary>A list of all currently loaded assemblies. Do not modify.</summary>
            public static readonly List<Assembly> All;

            private static readonly Dictionary<Assembly, Type[]> Types;
            private static readonly Dictionary<string, List<Assembly>> Dependants;

#if UNITY_EDITOR
            /// <summary>[Editor-Only] A reference to the assembly in which Unity compiles your runtime scripts.</summary>
            public static readonly Assembly UnityCSharpRuntime;
#endif

            /************************************************************************************************************************/
            #region Initialisation
            /************************************************************************************************************************/

            static Assemblies()
            {
#if UNITY_EDITOR
                WeaverUtilities.LogIfRestricted($"{nameof(ReflectionUtilities)}.{nameof(Assemblies)}");

                var CSharpRuntimePath =
                    Environment.CurrentDirectory.ReplaceSlashesBack() +
                    @"\Library\ScriptAssemblies\Assembly-CSharp.dll";
#endif

                var currentDomain = AppDomain.CurrentDomain;

                All = new List<Assembly>(currentDomain.GetAssemblies());

                var capacity = All.Count * 2;
                Types = new Dictionary<Assembly, Type[]>(capacity);
                Dependants = new Dictionary<string, List<Assembly>>(capacity);

                for (int i = 0; i < All.Count; i++)
                {
                    var assembly = All[i];

#if UNITY_EDITOR
                    if (UnityCSharpRuntime == null)
                    {
                        try
                        {
                            if (assembly.Location.ReplaceSlashesBack() == CSharpRuntimePath)
                            {
                                UnityCSharpRuntime = assembly;
                            }
                        }
                        catch { }// Calling assembly.Location on a dynamic assembly throws an exception because it has no location.
                    }
#endif

                    GatherReferences(assembly);
                }

                currentDomain.AssemblyLoad += OnAssemblyLoad;
            }

            /************************************************************************************************************************/

            private static void GatherReferences(Assembly assembly)
            {
                var references = assembly.GetReferencedAssemblies();

                for (int i = 0; i < references.Length; i++)
                {
                    var referenceName = references[i].Name;
                    if (!Dependants.TryGetValue(referenceName, out List<Assembly> dependants))
                    {
                        dependants = new List<Assembly>();
                        Dependants.Add(referenceName, dependants);
                    }

                    dependants.Add(assembly);
                }
            }

            /************************************************************************************************************************/

            private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
            {
                if (Dependants.ContainsKey(args.LoadedAssembly.GetName().Name))
                    return;

                GatherReferences(args.LoadedAssembly);

                All.Add(args.LoadedAssembly);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Logs all currently loaded assemblies and any others which are dependant on them.
            /// </summary>
            public static void LogAllDependants()
            {
                var text = WeaverUtilities.GetStringBuilder();

                text.AppendLine("The following is a list of all currently loaded assemblies, with each one being followed by all other assemblies which are dependant on it:");

                foreach (var assembly in Dependants)
                {
                    text.AppendLine();
                    text.AppendLine(assembly.Key);
                    for (int i = 0; i < assembly.Value.Count; i++)
                    {
                        text.Append(" - ").AppendLine(assembly.Value[i].FullName);
                    }
                }

                Debug.Log(text.ReleaseToString());
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Methods
            /************************************************************************************************************************/

            /// <summary>
            /// Returns an array of all types in the specified assembly.
            /// The array is cached to avoid garbage collection.
            /// </summary>
            public static Type[] GetTypes(Assembly assembly)
            {
                if (!Types.TryGetValue(assembly, out Type[] types))
                {
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        types = Type.EmptyTypes;
                    }

                    Types.Add(assembly, types);
                }
                return types;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls the specified method once for each type in the specified assembly.
            /// </summary>
            public static void ForEachType(Assembly assembly, Action<Type> method)
            {
                var types = GetTypes(assembly);
                for (int j = 0; j < types.Length; j++)
                    method(types[j]);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls the specified method once for each loaded assembly that references the specified assembly (including itself).
            /// </summary>
            public static void ForEachDependantAssembly(Assembly assembly, Action<Assembly> method)
            {
                method(assembly);

                if (Dependants.TryGetValue(assembly.GetName().Name, out List<Assembly> dependants))
                {
                    for (int i = 0; i < dependants.Count; i++)
                        method(dependants[i]);
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls the specified `method` once for each type in each loaded assembly that references the specified `assembly`.
            /// </summary>
            public static void ForEachTypeInDependantAssemblies(Assembly assembly, Action<Type> method)
            {
                ForEachType(assembly, method);

                if (Dependants.TryGetValue(assembly.GetName().Name, out List<Assembly> dependants))
                {
                    for (int i = 0; i < dependants.Count; i++)
                        ForEachType(dependants[i], method);
                }
            }

            /// <summary>
            /// Calls the specified `method` once for each type in each loaded assembly that references the assembly in
            /// which the `method` is declared.
            /// </summary>
            public static void ForEachTypeInDependantAssemblies(Action<Type> method)
            {
                ForEachTypeInDependantAssemblies(method.Method.DeclaringType.Assembly, method);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Tries to find a type with the specified name in any currently loaded assembly.
            /// </summary>
            public static Type FindType(string name, bool throwOnError = false, bool ignoreCase = false)
            {
                for (int i = 0; i < All.Count; i++)
                {
                    var type = All[i].GetType(name, throwOnError, ignoreCase);
                    if (type != null)
                        return type;
                }

                return null;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Details
        /************************************************************************************************************************/

        /// <summary>Checks if `type` is descended from `generic` (where `generic` is a generic type definition).</summary>
        public static bool IsSubclassOfGenericDefinition(this Type type, Type generic, out Type[] genericArguments)
        {
            while (type != null && type != typeof(object))
            {
                var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericTypeDefinition == generic)
                {
                    genericArguments = type.GetGenericArguments();
                    return true;
                }
                type = type.BaseType;
            }
            genericArguments = null;
            return false;
        }

        /// <summary>Checks if `type` is descended from `generic` (where `generic` is a generic type definition).</summary>
        public static bool IsSubclassOfGenericDefinition(this Type type, Type generic)
        {
            while (type != null)
            {
                var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericTypeDefinition == generic)
                {
                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If `type` implements the `interfaceType` this method returns the generic arguments it uses for that interface.
        /// </summary>
        public static Type[] GetGenericInterfaceArguments(Type type, Type interfaceType)
        {
            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var interfaceI = interfaces[i];
                if (!interfaceI.IsGenericType ||
                    interfaceI.GetGenericTypeDefinition() != interfaceType)
                    continue;

                return interfaceI.GetGenericArguments();
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>Checks if `target` has an [<see cref="ObsoleteAttribute"/>].</summary>
        public static bool IsObsolete(this ICustomAttributeProvider target, bool inherited = false)
        {
            return target.IsDefined(typeof(ObsoleteAttribute), inherited);
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, HashSet<Type>> _PrimitiveImplicitCasts;

        /// <summary>
        /// Returns true if `from` can be implicitly cast to `to`.
        /// </summary>
        public static bool HasImplicitCast(Type from, Type to)
        {
            if (_PrimitiveImplicitCasts == null)
            {
                _PrimitiveImplicitCasts = new Dictionary<Type, HashSet<Type>>
                {
                    {
                        typeof(long),
                        new HashSet<Type>
                        {
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(int),
                        new HashSet<Type>
                        {
                            typeof(long),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(short),
                        new HashSet<Type>
                        {
                            typeof(int),
                            typeof(long),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(sbyte),
                        new HashSet<Type>
                        {
                            typeof(short),
                            typeof(int),
                            typeof(long),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(ulong),
                        new HashSet<Type>
                        {
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(uint),
                        new HashSet<Type>
                        {
                            typeof(long),
                            typeof(ulong),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(ushort),
                        new HashSet<Type>
                        {
                            typeof(int),
                            typeof(uint),
                            typeof(long),
                            typeof(ulong),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(byte),
                        new HashSet<Type>
                        {
                            typeof(short),
                            typeof(ushort),
                            typeof(int),
                            typeof(uint),
                            typeof(long),
                            typeof(ulong),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    {
                        typeof(char),
                        new HashSet<Type>
                        {
                            typeof(ushort),
                            typeof(int),
                            typeof(uint),
                            typeof(long),
                            typeof(ulong),
                            typeof(float),
                            typeof(double),
                            typeof(decimal)
                        }
                    },
                    //{
                    //    typeof(bool),
                    //    new HashSet<Type>()
                    //},
                    //{
                    //    typeof(decimal),
                    //    new HashSet<Type>()
                    //},
                    {
                        typeof(float),
                        new HashSet<Type>
                        {
                            typeof(double)
                        }
                    },
                    //{
                    //    typeof(double),
                    //    new HashSet<Type>()
                    //},
                    //{
                    //    typeof(IntPtr),
                    //    new HashSet<Type>()
                    //},
                    //{
                    //    typeof(UIntPtr),
                    //    new HashSet<Type>()
                    //},
                };
            }

            if (_PrimitiveImplicitCasts.TryGetValue(from, out var casts))
            {
                return casts.Contains(to);
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// All built-in types except <see cref="object"/> can be const.
        /// </summary>
        public static bool CanBeConst(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(float) ||
                type == typeof(int) ||
                type == typeof(string) ||
                type == typeof(double) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(char) ||
                type == typeof(decimal) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(short) ||
                type == typeof(ushort);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Gathering
        /************************************************************************************************************************/

        /// <summary><see cref="BindingFlags"/> for any access modifiers.</summary>
        public const BindingFlags AnyAccessBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary><see cref="BindingFlags"/> for instance access modifiers.</summary>
        public const BindingFlags InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary><see cref="BindingFlags"/> for static access modifiers.</summary>
        public const BindingFlags StaticBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary><see cref="BindingFlags"/> for public and static.</summary>
        public const BindingFlags PublicStaticBindings = BindingFlags.Public | BindingFlags.Static;

        /// <summary><see cref="BindingFlags"/> for public and instance.</summary>
        public const BindingFlags PublicInstanceBindings = BindingFlags.Public | BindingFlags.Instance;

        /************************************************************************************************************************/

        /// <summary>Checks if `bindings` contains all the flags in `flags`.</summary>
        public static bool HasFlags(this BindingFlags bindings, BindingFlags flags)
        {
            return (bindings & flags) == flags;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls the specified method once for each type in every currently loaded assembly.
        /// <para></para>
        /// Most situations where you want to do something for each type are in reference to something specific.
        /// For example: when searching for every type with a certain attribute, you only need to look in assemblies
        /// that reference the assembly containing the attribute so you should instead use
        /// <see cref="ReflectionUtilities"/>.<see cref="Assemblies.ForEachTypeInDependantAssemblies(Assembly, Action{Type})"/>.
        /// </summary>
        public static void ForEachType(Action<Type> method)
        {
            for (int i = 0; i < Assemblies.All.Count; i++)
                Assemblies.ForEachType(Assemblies.All[i], method);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets a single custom attribute of type T and casts it.
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider target, bool inherit = false)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            if (!target.IsDefined(attributeType, inherit))
                return null;
            else
                return (TAttribute)target.GetCustomAttributes(attributeType, inherit)[0];
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls `method` once for each attribute of the specified type on `target`.
        /// </summary>
        public static bool ForEachCustomAttribute<TAttribute>(this ICustomAttributeProvider target, Action<TAttribute> method, bool inherit = false)
            where TAttribute : Attribute
        {
            var type = typeof(TAttribute);
            if (target.IsDefined(type, inherit))
            {
                var attributes = target.GetCustomAttributes(type, inherit);
                for (int i = 0; i < attributes.Length; i++)
                    method((TAttribute)attributes[i]);
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets all non-abstract types in the currently loaded assemblies which derive from the specified base type
        /// (starting with the base type itself if it isn't abstract).
        /// </summary>
        public static List<Type> GetDerivedTypes(this Type baseType, bool includeBase = true)
        {
            var derivedTypes = WeaverUtilities.GetList<Type>();
            baseType.GetDerivedTypes(derivedTypes, includeBase);
            return derivedTypes;
        }

        /// <summary>
        /// Gets all non-abstract types in the currently loaded assemblies which derive from the specified base type
        /// (starting with the base type itself if it isn't abstract).
        /// </summary>
        public static void GetDerivedTypes(this Type baseType, ICollection<Type> derivedTypes, bool includeBase = true)
        {
            if (includeBase && !baseType.IsAbstract)
                derivedTypes.Add(baseType);

            if (!baseType.ContainsGenericParameters)
            {
                Assemblies.ForEachTypeInDependantAssemblies(baseType.Assembly, (type) =>
                {
                    if (!type.IsAbstract && baseType.IsAssignableFrom(type))
                    {
                        derivedTypes.Add(type);
                    }
                });
            }
            else// If the type has unspecified generic parameters, we need to compare its entire heirarchy individually.
            {
                Assemblies.ForEachTypeInDependantAssemblies(baseType.Assembly, (type) =>
                {
                    if (type.IsAbstract)
                        return;

                    var originalType = type;

                    while (true)
                    {
                        if (type == null)
                            break;

                        if (type.IsGenericType)
                            type = type.GetGenericTypeDefinition();

                        if (type == baseType)
                        {
                            derivedTypes.Add(originalType);
                            break;
                        }
                        else
                        {
                            type = type.BaseType;
                        }
                    }
                });
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gets all types with the specified attribute all currently loaded assemblies.</summary>
        public static void GetAttributedTypes<TAttribute>(List<TAttribute> attributes, List<Type> types, bool includeAbstract = false)
            where TAttribute : Attribute
        {
            Assemblies.ForEachTypeInDependantAssemblies(typeof(TAttribute).Assembly, (type) =>
            {
                if ((!includeAbstract && type.IsAbstract) ||
                    !type.IsDefined(typeof(TAttribute), true))
                    return;

                var typeAttributes = type.GetCustomAttributes(typeof(TAttribute), true);
                for (int i = 0; i < typeAttributes.Length; i++)
                {
                    attributes.Add((TAttribute)typeAttributes[i]);
                    types.Add(type);
                }
            });
        }

        /************************************************************************************************************************/

        /// <summary>Gets all fields with the specified attribute in `type`.</summary>
        public static void GetAttributedFields<TAttribute>(Type type, BindingFlags bindingFlags,
            List<TAttribute> attributes, List<FieldInfo> fields)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allFields = type.GetFields(bindingFlags);

            for (int iField = 0; iField < allFields.Length; iField++)
            {
                var field = allFields[iField];
                if (!field.IsDefined(attributeType, true))
                    continue;

                var methodAttributes = field.GetCustomAttributes(attributeType, true);
                for (int iAttribute = 0; iAttribute < methodAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)methodAttributes[iAttribute]);
                    fields.Add(field);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gets all properties with the specified attribute in `type`.</summary>
        public static void GetAttributedProperties<TAttribute>(Type type, BindingFlags bindingFlags,
            List<TAttribute> attributes, List<PropertyInfo> properties)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allProperties = type.GetProperties(bindingFlags);

            for (int iProperty = 0; iProperty < allProperties.Length; iProperty++)
            {
                var property = allProperties[iProperty];
                if (!property.IsDefined(attributeType, true))
                    continue;

                var methodAttributes = property.GetCustomAttributes(attributeType, true);
                for (int iAttribute = 0; iAttribute < methodAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)methodAttributes[iAttribute]);
                    properties.Add(property);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gets all methods with the specified attribute in every type in the currently loaded assemblies.</summary>
        public static void GetAttributedMethods<TAttribute>(BindingFlags bindingFlags,
            List<TAttribute> attributes, List<MethodInfo> methods)
            where TAttribute : Attribute
        {
            Assemblies.ForEachTypeInDependantAssemblies(typeof(TAttribute).Assembly, (type) =>
            {
                type.GetAttributedMethods(bindingFlags, attributes, methods);
            });
        }

        /// <summary>Gets all methods with the specified attribute in `type`.</summary>
        public static void GetAttributedMethods<TAttribute>(this Type type, BindingFlags bindingFlags,
            List<TAttribute> attributes, List<MethodInfo> methods)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allMethods = type.GetMethods(bindingFlags);

            for (int iMethod = 0; iMethod < allMethods.Length; iMethod++)
            {
                var method = allMethods[iMethod];

                if (!method.IsDefined(attributeType, true))
                    continue;

                var methodAttributes = method.GetCustomAttributes(attributeType, true);
                for (int iAttribute = 0; iAttribute < methodAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)methodAttributes[iAttribute]);
                    methods.Add(method);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a delegate of the specified type from `method`.
        /// </summary>
        public static T GetDelegate<T>(this MethodInfo method) where T : class
        {
            if (method != null)
                return Delegate.CreateDelegate(typeof(T), method) as T;
            else
                return null;
        }

        /// <summary>
        /// Creates a delegate of the specified type from `method`.
        /// </summary>
        public static void GetDelegate<T>(this MethodInfo method, out T del) where T : class
        {
            del = method.GetDelegate<T>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Tries to find a field/property/method in the `declaringType` with the `name` and creates a delegate to get
        /// its value.
        /// <para></para>
        /// If a member is found but can't be converted to an appropriate delegate, the `error` message describes the
        /// problem.
        /// </summary>
        public static Func<T> GetMemberFunc<T>(Type declaringType, string name, object obj, out string error, BindingFlags bindingFlags)
        {
            if (string.IsNullOrEmpty(name))
            {
                error = null;
                return null;
            }

            var type = declaringType;
            TryFindMember:

            var field = type.GetField(name, bindingFlags);
            if (field == null)
            {
                // Continue.
            }
            else if (field.FieldType != typeof(T))
            {
                error = $"Found '{name}' field in {type.GetNameCS()}, but it is not a {typeof(T).GetNameCS()} field.";
                return null;
            }
            else
            {
                error = null;
                return () => (T)field.GetValue(obj);
            }

            MethodInfo method;

            var property = type.GetProperty(name, bindingFlags);
            if (property == null)
            {
                // Continue.
            }
            else if (property.PropertyType != typeof(T))
            {
                error = $"Found '{name}' property in {type.GetNameCS()}, but it is not a {typeof(T).GetNameCS()} property.";
                return null;
            }
            else
            {
                method = property.GetGetMethod(true);
                if (method == null)
                {
                    error = $"Found '{name}' property in {type.GetNameCS()}, but it has no getter.";
                    return null;
                }
                else
                {
                    goto CreateDelegate;
                }
            }

            method = type.GetMethod(name, bindingFlags, null, Type.EmptyTypes, null);
            if (method == null)
            {
                // Continue.
            }
            else if (method.ReturnType != typeof(T))
            {
                error = $"Found '{name}' method in {type.GetNameCS()}, but it does not return {typeof(T).GetNameCS()}.";
                return null;
            }
            else
            {
                goto CreateDelegate;
            }

            type = type.BaseType;
            if (type != null)
                goto TryFindMember;

            error = $"No '{name}' field/property/method was found in {declaringType.GetNameCS()} with the BindingFlags {bindingFlags}.";
            return null;

            CreateDelegate:
            error = null;
            return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), obj, method);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Object Arrays
        /************************************************************************************************************************/

        [ThreadStatic]
        private static object[] _OneObject, _TwoObjects;

        [ThreadStatic]
        private static Type[] _OneType, _TwoTypes;

        /************************************************************************************************************************/

        /// <summary>
        /// Returns <see cref="object"/>[1] { obj }.
        /// <para></para>
        /// The array is kept in a field marked with [<see cref="ThreadStaticAttribute"/>], so it is thread safe
        /// but cannot be used recursively within a single thread.
        /// </summary>
        public static object[] OneObject(object obj)
        {
            if (_OneObject == null)
                _OneObject = new object[1];

            _OneObject[0] = obj;
            return _OneObject;
        }

        /// <summary>
        /// Returns <see cref="object"/>[2] { obj0, obj1 }.
        /// <para></para>
        /// The array is kept in a field marked with [<see cref="ThreadStaticAttribute"/>], so it is thread safe
        /// but cannot be used recursively within a single thread.
        /// </summary>
        public static object[] TwoObjects(object obj0, object obj1)
        {
            if (_TwoObjects == null)
                _TwoObjects = new object[2];

            _TwoObjects[0] = obj0;
            _TwoObjects[1] = obj1;
            return _TwoObjects;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns <see cref="Type"/>[1] { type }.
        /// <para></para>
        /// The array is kept in a field marked with [<see cref="ThreadStaticAttribute"/>], so it is thread safe
        /// but cannot be used recursively within a single thread.
        /// </summary>
        public static Type[] OneType(Type type)
        {
            if (_OneType == null)
                _OneType = new Type[1];

            _OneType[0] = type;
            return _OneType;
        }

        /// <summary>
        /// Returns <see cref="Type"/>[2] { type0, type1 }.
        /// <para></para>
        /// The array is kept in a field marked with [<see cref="ThreadStaticAttribute"/>], so it is thread safe
        /// but cannot be used recursively within a single thread.
        /// </summary>
        public static Type[] TwoTypes(Type type0, Type type1)
        {
            if (_TwoTypes == null)
                _TwoTypes = new Type[2];

            _TwoTypes[0] = type0;
            _TwoTypes[1] = type1;
            return _TwoTypes;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Strings
        /************************************************************************************************************************/

        /// <summary>Builds a string containing the values of each of the specified object's fields.</summary>
        public static string DeepToString(object obj)
        {
            var text = WeaverUtilities.GetStringBuilder();
            AppendDeepToString(text, 0, obj);
            return text.ReleaseToString();
        }

        /// <summary>Builds a string containing the values of each of the specified object's fields.</summary>
        public static void AppendDeepToString(StringBuilder text, int indent, object obj, int maxDepth = 1)
        {
            if (obj == null)
            {
                text.AppendLineConst("null");
                return;
            }

            text.AppendLineConst(obj.ToString());

            // Circular references (or even self references) could cause infinite recursion.
            if (indent++ >= maxDepth)
                return;

            var fields = obj.GetType().GetFields(InstanceBindings);
            foreach (var field in fields)
            {
                text.Indent(indent);
                text.Append(field.Name);
                text.Append(" = ");

                var value = field.GetValue(obj);
                if (value is string)
                {
                    text.AppendLineConst(value);
                }
                else if (value is IEnumerable enumerable)
                {
                    WeaverUtilities.AppendDeepToString(text, enumerable.GetEnumerator(), ", ");
                    text.AppendLineConst();
                }
                else
                {
                    AppendDeepToString(text, indent, value, maxDepth);
                }
            }

            text.AppendLineConst();
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, string> _TypeToQualifiedName;

        /// <summary>Returns "<see cref="Type.FullName"/>, <see cref="AssemblyName.Name"/>".</summary>
        /// <remarks>This is similar to <see cref="Type.AssemblyQualifiedName"/>, but without the assembly version.</remarks>
        public static string GetQualifiedName(Type type)
        {
            if (_TypeToQualifiedName == null)
                _TypeToQualifiedName = new Dictionary<Type, string>();

            if (!_TypeToQualifiedName.TryGetValue(type, out var name))
            {
                name = $"{type.FullName}, {type.Assembly.GetName().Name}";
                _TypeToQualifiedName.Add(type, name);
            }

            return name;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a description of the access modifier associated with `bindings`</summary>
        public static string ToAccessModifier(this BindingFlags bindings)
        {
            string modifier;

            if (bindings.HasFlags(BindingFlags.Public))
            {
                if (!bindings.HasFlags(BindingFlags.NonPublic))
                    modifier = "public ";
                else
                    modifier = null;
            }
            else if (bindings.HasFlags(BindingFlags.NonPublic))
            {
                modifier = "non-public ";
            }
            else modifier = null;

            if (bindings.HasFlags(BindingFlags.Static))
            {
                if (!bindings.HasFlags(BindingFlags.Instance))
                {
                    if (modifier == null)
                        return "static ";
                    else
                        return modifier + "static ";
                }
            }

            return modifier;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns null, "override" or "new" as appropriate for a method called `methodName` in a child of `baseType`.
        /// </summary>
        public static string GetInheritanceModifier(Type baseType, string methodName, params Type[] parameterTypes)
        {
            if (baseType == null) return null;

            var method = baseType.GetMethod(methodName, AnyAccessBindings, null, parameterTypes, null);

            if (method == null) return null;

            if (method.IsVirtual || method.IsAbstract)
                return "override";
            else
                return "new";
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

