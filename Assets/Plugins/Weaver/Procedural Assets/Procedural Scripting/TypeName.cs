// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

#if UNITY_EDITOR

using System;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] The name of a type.</summary>
    public readonly struct TypeName
    {
        /************************************************************************************************************************/

        /// <summary>The named <see cref="System.Type"/> (optional).</summary>
        public readonly Type Type;

        /// <summary>The <see cref="Type.Namespace"/>.</summary>
        public readonly string Namespace;

        /// <summary>The <see cref="System.Reflection.MemberInfo.Name"/>.</summary>
        public readonly string Name;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public TypeName(Type type)
        {
            Type = type;

            Name = CSharp.GetKeyword(type);
            if (Name != null)
            {
                Namespace = null;
            }
            else
            {
                Namespace = type.Namespace;
                Name = type.GetNameCS(CSharp.NameVerbosity.Nested);
            }
        }

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public static implicit operator TypeName(Type type) => new TypeName(type);

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public TypeName(TypeBuilder typeBuilder)
        {
            Type = null;
            Name = typeBuilder.Name;
            Namespace = typeBuilder.Namespace;
        }

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public static implicit operator TypeName(TypeBuilder typeBuilder) => new TypeName(typeBuilder);

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public TypeName(string name)
        {
            Type = null;
            Namespace = null;
            Name = name;
        }

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public static implicit operator TypeName(string name) => new TypeName(name);

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TypeName"/>.</summary>
        public TypeName(string nameSpace, string name)
        {
            Type = null;
            Namespace = nameSpace;
            Name = name;
        }

        /************************************************************************************************************************/

        /// <summary>Does `a` match `b`?</summary>
        public static bool operator ==(TypeName a, TypeName b) => a.Name == b.Name && a.Namespace == b.Namespace;

        /// <summary>Does `a` not match `b`?</summary>
        public static bool operator !=(TypeName a, TypeName b) => !(a == b);

        /// <summary>Does the `name` match the `type`?</summary>
        public static bool operator ==(TypeName name, Type type)
        {
            if (name.Type != null)
            {
                return name.Type == type;
            }
            else
            {
                return name.Name == type.Name && name.Namespace == type.Namespace;
            }
        }

        /// <summary>Does the `name` not match the `type`?</summary>
        public static bool operator !=(TypeName name, Type type) => !(name == type);

        /// <summary>Does the `name` match the `type`?</summary>
        public static bool operator ==(Type type, TypeName name) => name == type;

        /// <summary>Does the `name` not match the `type`?</summary>
        public static bool operator !=(Type type, TypeName name) => !(name == type);

        /// <summary>Does this name match the `obj`?</summary>
        public override bool Equals(object obj)
        {
            if (obj is TypeName name)
                return this == name;

            if (obj is Type type)
                return this == type;

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>Appends "<see cref="Namespace"/>.<see cref="Name"/>".</summary>
        public void AppendFullName(StringBuilder text)
        {
            if (Namespace != null)
                text.Append(Namespace).Append('.');

            text.Append(Name);
        }

        /// <summary>Returns "<see cref="Namespace"/>.<see cref="Name"/>".</summary>
        public string FullName => Namespace != null ? $"{Namespace}.{Name}" : Name;

        /// <summary>Returns "<see cref="Namespace"/>.<see cref="Name"/>".</summary>
        public override string ToString() => FullName;

        /************************************************************************************************************************/
    }
}

#endif

