// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

namespace Weaver
{
    /// <summary>A trinary logic value: true, false, or unspecified.</summary>
    public enum OptionalBool
    {
        /************************************************************************************************************************/

        /// <summary>Use the default setting.</summary>
        Unspecified,

        /// <summary>True, regardless of the default setting.</summary>
        True,

        /// <summary>False, regardless of the default setting.</summary>
        False,

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/

    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="bool"/> value corresponding to the specified `optional` value.
        /// </summary>
        public static bool ToBool(this OptionalBool optional, bool defaultValue = false)
        {
            switch (optional)
            {
                case OptionalBool.True: return true;
                case OptionalBool.False: return false;
                default:
                case OptionalBool.Unspecified: return defaultValue;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="OptionalBool"/> value corresponding to the specified `value`.
        /// </summary>
        public static OptionalBool ToOptionalBool(bool value)
        {
            return value ? OptionalBool.True : OptionalBool.False;
        }

        /************************************************************************************************************************/
    }
}

