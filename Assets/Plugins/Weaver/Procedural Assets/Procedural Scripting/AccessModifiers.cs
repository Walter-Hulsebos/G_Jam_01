// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] The C# access modifiers of a member.</summary>
    /// <remarks>Public/Internal/Protected/Private are sorted from least to most restrictive.</remarks>
    [Flags]
    public enum AccessModifiers
    {
        None = 0,

        Public = 1 << 0,
        Internal = 1 << 1,
        Protected = 1 << 2,
        Private = 1 << 3,

        Const = 1 << 4,
        Static = 1 << 5,
        Readonly = 1 << 6,

        Sealed = 1 << 7,
        Abstract = 1 << 8,
        Virtual = 1 << 9,
        Override = 1 << 10,
        New = 1 << 11,
        Partial = 1 << 12,
    }

    public static partial class CSharpProcedural
    {
        /************************************************************************************************************************/
        #region AccessModifiers Extensions
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if `modifiers` contains all of the flags specified in `contains`.
        /// </summary>
        public static bool Contains(this AccessModifiers modifiers, AccessModifiers contains)
        {
            return (modifiers & contains) == contains;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the C# declaration of the specified access `modifiers` to the `text`.
        /// </summary>
        public static void AppendDeclaration(this AccessModifiers modifiers, StringBuilder text)
        {
            if (modifiers.Contains(AccessModifiers.Public))
                text.Append("public ");
            else if (modifiers.Contains(AccessModifiers.Private))
                text.Append("private ");
            else if (modifiers.Contains(AccessModifiers.Protected))
                text.Append("protected ");
            else if (modifiers.Contains(AccessModifiers.Internal))
                text.Append("internal ");
            else
                text.Append("public ");

            if (modifiers.Contains(AccessModifiers.Const))
                text.Append("const ");
            else
            {
                if (modifiers.Contains(AccessModifiers.Static))
                    text.Append("static ");

                if (modifiers.Contains(AccessModifiers.Readonly))
                    text.Append("readonly ");
            }

            if (modifiers.Contains(AccessModifiers.Sealed))
                text.Append("sealed ");
            else if (modifiers.Contains(AccessModifiers.Abstract))
                text.Append("abstract ");
            else if (modifiers.Contains(AccessModifiers.Virtual))
                text.Append("virtual ");
            else if (modifiers.Contains(AccessModifiers.Override))
                text.Append("override ");
            else if (modifiers.Contains(AccessModifiers.New))
                text.Append("new ");

            if (modifiers.Contains(AccessModifiers.Partial))
                text.Append("partial ");
        }

        /// <summary>
        /// Returns the C# declaration of the specified access `modifiers`.
        /// </summary>
        public static string GetDeclaration(this AccessModifiers modifiers)
        {
            var text = WeaverUtilities.GetStringBuilder();
            AppendDeclaration(modifiers, text);
            return text.ReleaseToString();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `type` has the specified access `modifiers`.
        /// </summary>
        public static bool HasModifiers(Type type, AccessModifiers modifiers)
        {
            if (modifiers.Contains(AccessModifiers.Public))
            {
                if (!type.IsPublic && !type.IsNestedPublic)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Private))
            {
                if (!type.IsNestedPrivate)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Protected))
            {
                if (modifiers.Contains(AccessModifiers.Internal))
                {
                    if (!type.IsNestedFamORAssem)
                        return false;
                }
                else
                {
                    if (!type.IsNestedFamily)
                        return false;
                }
            }
            else if (modifiers.Contains(AccessModifiers.Internal))
            {
                if (!type.IsVisible)
                    return false;
            }

            if (modifiers.Contains(AccessModifiers.Abstract))
            {
                if (!type.IsAbstract || type.IsSealed)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Virtual))
            {
                if (type.IsAbstract || type.IsSealed)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Static))
            {
                if (!type.IsAbstract || !type.IsSealed)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Sealed))
            {
                if (!type.IsSealed)
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `field` has the specified access `modifiers`.
        /// </summary>
        public static bool HasModifiers(FieldInfo field, AccessModifiers modifiers)
        {
            if (modifiers.Contains(AccessModifiers.Public))
            {
                if (!field.IsPublic)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Private))
            {
                if (!field.IsPrivate)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Protected))
            {
                if (modifiers.Contains(AccessModifiers.Internal))
                {
                    if (!field.IsFamilyAndAssembly)
                        return false;
                }
                else
                {
                    if (!field.IsFamily)
                        return false;
                }
            }
            else if (modifiers.Contains(AccessModifiers.Internal))
            {
                if (!field.IsAssembly)
                    return false;
            }

            if (modifiers.Contains(AccessModifiers.Const))
                return field.IsLiteral;

            if (field.IsStatic != modifiers.Contains(AccessModifiers.Static) ||
                field.IsInitOnly != modifiers.Contains(AccessModifiers.Readonly))
                return false;

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `method` has the specified access `modifiers`.
        /// </summary>
        public static bool HasModifiers(MethodBase method, AccessModifiers modifiers)
        {
            if (modifiers == AccessModifiers.None)
            {
                return method == null;
            }

            if (modifiers.Contains(AccessModifiers.Public))
            {
                if (!method.IsPublic)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Private))
            {
                if (!method.IsPrivate)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Protected))
            {
                if (modifiers.Contains(AccessModifiers.Internal))
                {
                    if (!method.IsFamilyAndAssembly)
                        return false;
                }
                else
                {
                    if (!method.IsFamily)
                        return false;
                }
            }
            else if (modifiers.Contains(AccessModifiers.Internal))
            {
                if (!method.IsAssembly)
                    return false;
            }

            if (modifiers.Contains(AccessModifiers.Static))
            {
                if (!method.IsStatic)
                    return false;
            }

            if (modifiers.Contains(AccessModifiers.Abstract))
            {
                if (!method.IsAbstract)
                    return false;
            }
            else if (modifiers.Contains(AccessModifiers.Virtual))
            {
                if (!method.IsVirtual)
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `property` has the specified access `modifiers`.
        /// </summary>
        public static bool HasModifiers(PropertyInfo property, AccessModifiers getterModifiers, AccessModifiers setterModifiers)
        {
            return
                HasModifiers(property.GetGetMethod(true), getterModifiers) &&
                HasModifiers(property.GetSetMethod(true), setterModifiers);
        }

        /// <summary>
        /// Returns true if the specified `property` has the specified access `modifiers`.
        /// </summary>
        public static bool HasModifiers(PropertyInfo property, AccessModifiers modifiers)
        {
            return HasModifiers(property, modifiers);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the restrictiveness modifier (public/private/protected/internal) of the specified `method`.
        /// </summary>
        public static AccessModifiers GetRestrictivenessModifier(MethodInfo method)
        {
            if (method == null)
                return AccessModifiers.None;
            if (method.IsPublic)
                return AccessModifiers.Public;
            if (method.IsPrivate)
                return AccessModifiers.Private;
            if (method.IsFamily)
                return AccessModifiers.Protected;
            if (method.IsAssembly)
                return AccessModifiers.Internal;
            if (method.IsFamilyAndAssembly)
                return AccessModifiers.Protected | AccessModifiers.Internal;

            throw new Exception("Unable to determine the restrictiveness of " + method.GetNameCS());
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

