// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] The details of a procedural method parameter.</summary>
    public readonly struct ParameterBuilder
    {
        /************************************************************************************************************************/

        /// <summary>The prefix keyword of the parameter (.</summary>
        /// <remarks>This value is not considered when determining if a script needs to be regenerated.</remarks>
        public readonly ParameterModifier Modifier;

        /// <summary>The name of the parameter type.</summary>
        public readonly TypeName Type;

        /// <summary>The C# name of this parameter.</summary>
        public readonly string Name;

        /// <summary>The default value of each of this parameter.</summary>
        /// <remarks>This value is not considered when determining if a script needs to be regenerated.</remarks>
        public readonly string DefaultValue;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ParameterBuilder"/>.</summary>
        public ParameterBuilder(ParameterModifier modifier, TypeName type, string name, string defaultValue = null)
        {
            Modifier = modifier;
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }

        /// <summary>Creates a new <see cref="ParameterBuilder"/>.</summary>
        public ParameterBuilder(TypeName type, string name, string defaultValue = null)
        {
            Modifier = ParameterModifier.None;
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }

        /************************************************************************************************************************/

        /// <summary>Is this a match for the specified `parameter`?</summary>
        public bool IsParameter(ParameterInfo parameter)
        {
            if (Type != parameter.ParameterType ||
                Name != parameter.Name)
                return false;

            switch (Modifier)
            {
                case ParameterModifier.None: return !parameter.IsIn && !parameter.IsOut;
                case ParameterModifier.In: return parameter.IsIn;
                case ParameterModifier.Out: return parameter.ParameterType.IsByRef && parameter.IsOut;
                case ParameterModifier.Ref: return parameter.ParameterType.IsByRef && !parameter.IsOut;
                case ParameterModifier.Params:
                case ParameterModifier.This:
                default:
                    return true;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Do all the `parameters` match the `builders`?</summary>
        public static bool AreParametersSame(ParameterInfo[] parameters, ParameterBuilder[] builders)
        {
            if (parameters.IsNullOrEmpty())
                return builders.IsNullOrEmpty();

            if (parameters.Length != builders.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!builders[i].IsParameter(parameters[i]))
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Appends the C# declaration of this parameter.</summary>
        public void AppendDeclaration(StringBuilder text)
        {
            var prefix = Modifier.GetKeyword();
            if (prefix != null)
                text.Append(prefix).Append(' ');

            Type.AppendFullName(text);
            text.Append(' ').Append(Name);

            if (DefaultValue != null)
                text.Append(" = ").Append(DefaultValue);
        }

        /************************************************************************************************************************/

        /// <summary>Appends the declaration of a set of method parameters.</summary>
        public static void AppendDeclaration(StringBuilder text, ParameterBuilder[] parameters)
        {
            if (parameters == null)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    text.Append(", ");

                parameters[i].AppendDeclaration(text);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

