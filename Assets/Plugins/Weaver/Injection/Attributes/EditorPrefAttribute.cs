// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Prefs = UnityEditor.EditorPrefs;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="InjectionAttribute"/> which saves and loads the value of the attributed member in
    /// <see cref="UnityEditor.EditorPrefs"/>.
    /// </summary>
    public sealed class EditorPrefAttribute : PrefAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Creates a new <see cref="EditorPrefAttribute"/>.</summary>
        public EditorPrefAttribute()
        {
            EditorOnly = true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a value to be assigned to the attributed property.
        /// </summary>
        protected override object GetValueToInject()
        {
            if (Prefs.HasKey(Key))
                return GetPref(MemberType, Key);
            else if (OldKey != null && Prefs.HasKey(OldKey))
                return GetPref(MemberType, OldKey);
            else
                return DefaultValue;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// The full name of the prefs class to use when building the procedural injector script.
        /// </summary>
        protected override string PrefsTypeName => "UnityEditor.EditorPrefs";

        /************************************************************************************************************************/
        // These functions are copied directly from PrefAttribute and now target EditorPrefs using the Prefs alias.
        /************************************************************************************************************************/
        #region Getters and Setters
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, Func<string, object>>
            TypeToGetter = new Dictionary<Type, Func<string, object>>();

        private static Func<string, object> GetPrefGetter(Type type)
        {
            if (!TypeToGetter.TryGetValue(type, out var getter))
            {
                if (type == typeof(bool))
                {
                    getter = (key) => Prefs.GetInt(key) != 0;
                }
                else if (type == typeof(int))
                {
                    getter = (key) => Prefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    getter = (key) => Prefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    getter = (key) => Prefs.GetString(key);
                }
                else if (type.IsEnum)
                {
                    getter = (key) => Enum.ToObject(type, Prefs.GetInt(key));
                }

                TypeToGetter.Add(type, getter);
            }

            return getter;
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<Type, Action<string, object>>
            TypeToSetter = new Dictionary<Type, Action<string, object>>();

        private static Action<string, object> GetPrefSetter(Type type)
        {
            if (!TypeToSetter.TryGetValue(type, out var setter))
            {
                if (type == typeof(bool))
                {
                    setter = (key, value) => Prefs.SetInt(key, (bool)value ? 1 : 0);
                }
                else if (type == typeof(int))
                {
                    setter = (key, value) => Prefs.SetInt(key, (int)value);
                }
                else if (type == typeof(float))
                {
                    setter = (key, value) => Prefs.SetFloat(key, (float)value);
                }
                else if (type == typeof(string))
                {
                    setter = (key, value) => Prefs.SetString(key, (string)value);
                }
                else if (type.IsEnum)
                {
                    setter = (key, value) => Prefs.SetInt(key, (int)value);
                }

                TypeToSetter.Add(type, setter);
            }

            return setter;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the specified `type` can be saved as a pref.
        /// </summary>
        public static new bool IsSupportedPref(Type type)
        {
            return GetPrefGetter(type) != null;
        }

        /************************************************************************************************************************/

        private static object GetPref(Type type, string key)
        {
            var getter = GetPrefGetter(type);
            if (getter != null)
                return getter(key);
            else
                return null;
        }

        /************************************************************************************************************************/

        private static void SetPref(Type type, string key, object value)
        {
            GetPrefSetter(type)?.Invoke(key, value);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

