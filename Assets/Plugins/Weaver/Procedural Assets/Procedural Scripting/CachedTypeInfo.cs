// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only]
    /// Encapsulates information about a <see cref="Type"/> for easy access and efficient reuse.
    /// </summary>
    public sealed class CachedTypeInfo
    {
        /************************************************************************************************************************/
        #region Pooling
        /************************************************************************************************************************/

        private static readonly Dictionary<string, CachedTypeInfo>
            FullNameToInfo = new Dictionary<string, CachedTypeInfo>();

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a <see cref="CachedTypeInfo"/> which encapsulates the specified `type` and caches it for efficient reuse.
        /// </summary>
        public static CachedTypeInfo Get(Type type)
        {
            if (!FullNameToInfo.TryGetValue(type.FullName, out CachedTypeInfo info))
            {
                info = new CachedTypeInfo(type);
                FullNameToInfo.Add(type.FullName, info);
            }

            return info;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a <see cref="CachedTypeInfo"/> which encapsulates the <see cref="Type"/> with the specified `fullname`
        /// and caches it for efficient reuse.
        /// </summary>
        public static CachedTypeInfo Get(string fullname)
        {
            if (!FullNameToInfo.TryGetValue(fullname, out CachedTypeInfo info))
            {
                var type = ReflectionUtilities.Assemblies.FindType(fullname);
                if (type != null)
                    info = new CachedTypeInfo(type);

                FullNameToInfo.Add(fullname, info);
            }

            return info;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Tries to find and return the info for a type with the specified `fullName`. Otherwise searches for a type
        /// with the `name` in any namespace.
        /// </summary>
        public static CachedTypeInfo FindExistingType(string name, string fullName, ref bool shouldRebuild)
        {
            var assembly = ReflectionUtilities.Assemblies.UnityCSharpRuntime;
            if (assembly == null)
                return null;

            // Try with the current namespace.
            var type = assembly.GetType(fullName);
            if (type != null)
                return Get(type);

            shouldRebuild = true;

            // If there this builder is in a namespace, try in the global namespace.
            if (name != fullName)
            {
                type = assembly.GetType(name);
                if (type != null)
                    return Get(type);
            }

            // Otherwise try in any namespace.
            var types = ReflectionUtilities.Assemblies.GetTypes(assembly);
            for (int i = 0; i < types.Length; i++)
            {
                type = types[i];
                if (type.Name == name)
                    return Get(type);
            }

            return null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>The encapsulated <see cref="Type"/>.</summary>
        public readonly Type Type;

        /// <summary>
        /// The members of the encapsulated <see cref="Type"/>.
        /// This list will be null if there are no members.
        /// </summary>
        public readonly List<MemberInfo> Members;

        /// <summary>
        /// The <see cref="CachedTypeInfo"/> for the types nested in the encapsulated <see cref="Type"/>.
        /// This list will be null if there are no nested types.
        /// </summary>
        public readonly List<CachedTypeInfo> NestedTypes;

        /************************************************************************************************************************/

        private CachedTypeInfo(Type type)
        {
            Type = type;

            var members = type.GetMembers(ReflectionUtilities.AnyAccessBindings | BindingFlags.DeclaredOnly);

            WeaverUtilities.GetList(out Members);

            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                switch (member.MemberType)
                {
                    default:
                    case MemberTypes.Event:
                    case MemberTypes.TypeInfo:
                    case MemberTypes.Custom:
                        break;

                    case MemberTypes.Constructor:
                    case MemberTypes.Field:
                    case MemberTypes.Property:
                        Members.Add(member);
                        break;

                    case MemberTypes.Method:
                        // Ignore property accessors and operators.
                        if ((member as MethodInfo).IsSpecialName)
                            break;

                        Members.Add(member);
                        break;

                    case MemberTypes.NestedType:
                        Members.Add(member);

                        if (NestedTypes == null)
                            WeaverUtilities.GetList(out NestedTypes);

                        NestedTypes.Add(Get(member as Type));
                        break;
                }
            }

            if (Members != null && Members.Count == 0)
                WeaverUtilities.Release(ref Members);

            if (NestedTypes != null && NestedTypes.Count == 0)
                WeaverUtilities.Release(ref NestedTypes);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the nested type with the specified `name` (if one exists).
        /// </summary>
        public CachedTypeInfo GetNestedType(string name)
        {
            if (NestedTypes == null)
                return null;

            for (int i = 0; i < NestedTypes.Count; i++)
            {
                var type = NestedTypes[i];
                if (type.Type.Name == name)
                    return type;
            }

            return null;
        }

        /************************************************************************************************************************/

        private bool
            _HasCheckedForObsoleteMembers,
            _HasAnyObsoleteMembers;

        /// <summary>
        /// Returns true if any of the members in the encapsulated type or any nested type are marked with an
        /// [<see cref="ObsoleteAttribute"/>].
        /// </summary>
        public bool HasAnyObsoleteMembers()
        {
            if (!_HasCheckedForObsoleteMembers)
            {
                _HasCheckedForObsoleteMembers = true;

                if (Type.IsObsolete())
                {
                    _HasAnyObsoleteMembers = true;
                }
                else if (Members != null)
                {
                    for (int i = 0; i < Members.Count; i++)
                    {
                        if (Members[i].IsObsolete())
                            _HasAnyObsoleteMembers = true;
                    }

                    if (NestedTypes != null)
                    {
                        for (int i = 0; i < NestedTypes.Count; i++)
                        {
                            if (NestedTypes[i].HasAnyObsoleteMembers())
                                _HasAnyObsoleteMembers = true;
                        }
                    }
                }
            }

            return _HasAnyObsoleteMembers;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a description of the encapsulated <see cref="Type"/> and its <see cref="Members"/>.
        /// </summary>
        public string GetDescription()
        {
            var text = WeaverUtilities.GetStringBuilder();
            AppendDescription(text, 0);
            return text.ReleaseToString();
        }

        /// <summary>
        /// Appends a description of the encapsulated <see cref="Type"/> and its <see cref="Members"/>.
        /// </summary>
        public void AppendDescription(StringBuilder text, int indent)
        {
            text.Indent(indent).Append("Type: ").AppendLineConst(Type.GetNameCS());

            if (Members != null)
            {
                for (int i = 0; i < Members.Count; i++)
                {
                    text.Indent(indent).AppendLineConst(Members[i]);
                }

                if (NestedTypes != null)
                {
                    for (int i = 0; i < NestedTypes.Count; i++)
                    {
                        NestedTypes[i].AppendDescription(text, indent + 1);
                    }
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns <see cref="Type"/>.ToString().
        /// </summary>
        public override string ToString()
        {
            return Type.ToString();
        }

        /************************************************************************************************************************/
    }
}

#endif

