// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Prefs = UnityEngine.PlayerPrefs;

#if UNITY_EDITOR
using UnityEditor;
using Weaver.Editor;
using Weaver.Editor.Procedural;
using Weaver.Editor.Procedural.Scripting;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="InjectionAttribute"/> which saves and loads the value of the attributed member in
    /// <see cref="PlayerPrefs"/>.
    /// </summary>
    public class PrefAttribute : InjectionAttribute
    {
        /************************************************************************************************************************/

        /// <summary>
        /// The name that will be used to identify the saved value.
        /// If not set, it will use the attributed member's Namespace.DeclaringType.Name.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The <see cref="Key"/> that the pref was previously saved with.
        /// </summary>
        public string OldKey { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// A callback which is invoked by <see cref="SaveAll"/>.
        /// </summary>
        public static event Action OnSave;

        /// <summary>
        /// Writes the values of all prefs to their persistent storage.
        /// </summary>
        public static void SaveAll()
        {
#if UNITY_EDITOR
            var i = 0;

            TryAgain:
            try
            {
                for (; i < InjectorManager.AllInjectionAttributes.Count; i++)
                {
                    if (InjectorManager.AllInjectionAttributes[i] is PrefAttribute attribute)
                    {
                        var value = attribute.GetValue();
                        SetPref(attribute.MemberType, attribute.Key, value);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Error Saving Pref: (see the following error message)");
                Debug.LogException(exception);
                i++;
                goto TryAgain;
            }
#endif

            // At runtime the Injector script registers its delegate to OnSave.

            OnSave?.Invoke();
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// The initial value of the attributed member before it was injected.
        /// </summary>
        public object DefaultValue { get; private set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// <para></para>
        /// Specifically, this method ensures that the <see cref="InjectionAttribute.MemberType"/> is a supported pref
        /// type.
        /// </summary>
        protected override bool TryInitialize()
        {
            Type = GetPrefType(MemberType);
            if (Type == PrefType.Unsupported)
                return LogThisInvalidAttribute("isn't a supported Pref type.");

            if (Key == null)
                Key = Member.GetNameCS();

            DefaultValue = GetValue();

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns true. Prefs should always have their saved value.
        /// </summary>
        public override bool InEditMode => true;

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

        static PrefAttribute()
        {
            // Before unloading assemblies, save all prefs.
            AssemblyReloadEvents.beforeAssemblyReload += SaveAll;
            EditorApplication.quitting += SaveAll;
        }

        /************************************************************************************************************************/
        #region Pref Types
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="PrefType"/> of the attributed member.</summary>
        public PrefType Type { get; private set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Denotes the way a particular pref will be saved and loaded.</summary>
        public enum PrefType
        {
            Unsupported,
            Float,
            Int,
            String,
            Bool,
            Enum,
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns the <see cref="PrefType"/> associated with the specified `type`.
        /// </summary>
        public static PrefType GetPrefType(Type type)
        {
            if (type == typeof(bool))
            {
                return PrefType.Bool;
            }
            else if (type == typeof(int))
            {
                return PrefType.Int;
            }
            else if (type == typeof(float))
            {
                return PrefType.Float;
            }
            else if (type == typeof(string))
            {
                return PrefType.String;
            }
            else if (type.IsEnum)
            {
                return PrefType.Enum;
            }

            return PrefType.Unsupported;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Prefs Interface
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Indicates whether a value has been saved using the specified `key`.</summary>
        protected virtual bool HasKey(string key)
        {
            return Prefs.HasKey(key);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns the float value saved with the specified `key`.</summary>
        protected virtual float GetFloat(string key)
        {
            return Prefs.GetFloat(key);
        }

        /// <summary>[Editor-Only] Returns the int value saved with the specified `key`.</summary>
        protected virtual int GetInt(string key)
        {
            return Prefs.GetInt(key);
        }

        /// <summary>[Editor-Only] Returns the string value saved with the specified `key`.</summary>
        protected virtual string GetString(string key)
        {
            return Prefs.GetString(key);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Saves the specified float `value` with the specified `key`.</summary>
        protected virtual void SetFloat(string key, float value)
        {
            Prefs.SetFloat(key, value);
        }

        /// <summary>[Editor-Only] Saves the specified int `value` with the specified `key`.</summary>
        protected virtual void SetInt(string key, int value)
        {
            Prefs.SetInt(key, value);
        }

        /// <summary>[Editor-Only] Saves the specified string `value` with the specified `key`.</summary>
        protected virtual void SetString(string key, string value)
        {
            Prefs.SetString(key, value);
        }

        /************************************************************************************************************************/
        #endregion
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
        public static bool IsSupportedPref(Type type)
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
        #region Script Building

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        protected internal override void GatherInjectorDetails(InjectorScriptBuilder builder)
        {
            const string MethodName = "OnApplicationStart";

            // The method itself must exist in the editor with the [RuntimeInitializeOnLoadMethod] attribute for the
            // build system to ensure it gets called on startup. So we just declare a second empty copy for the editor.

            if (!builder.HasMethod(MethodName))
            {
                var startMethod = builder.AddToMethod(MethodName, (text, indent) =>
                {
                    text.AppendLineConst("#if ! UNITY_EDITOR // Runtime.")
                        .AppendLineConst()
                        .Indent(indent).AppendLineConst("UnityEngine.Application.quitting += SavePrefs;")
                        .Indent(indent).Append(typeof(PrefAttribute).GetNameCS()).AppendLineConst(".OnSave += SavePrefs;");
                });

                startMethod.CompilationSymbolIndex = -1;
                startMethod.Modifiers = AccessModifiers.Private | AccessModifiers.Static;
                startMethod.SetAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute));
                startMethod.SetAttributeConstructorBuilders((text) =>
                {
                    WeaverUtilities.AppendFriendlyFullName(text, RuntimeInitializeLoadType.BeforeSceneLoad);
                });

                builder.OnGatheringComplete += () =>
                {
                    builder.AddToMethod(MethodName, (text, indent) =>
                    {
                        text.AppendLineConst("#endif");
                    });
                };
            }

            builder.AddToMethod(MethodName, this, (text, indent) =>
            {
                text.Indent(indent)
                    .Append("var value = ");

                if (OldKey != null)
                {
                    text.Append(PrefsTypeName)
                        .Append(".HasKey(\"")
                        .Append(Key)
                        .Append("\") ? ");

                    AppendPrefGetter(text, MemberType, Key, null);

                    text.Append(" : ");

                    AppendPrefGetter(text, MemberType, OldKey, DefaultValue);
                }
                else
                {
                    AppendPrefGetter(text, MemberType, Key, DefaultValue);
                }

                text.AppendLineConst(";");

                builder.AppendSetValue(text, indent, this, "value");
            });

            var saveMethod = builder.AddToMethod("SavePrefs", this, (text, indent) =>
            {
                builder.AppendGetValue(text, indent, this, "value");

                text.Indent(indent);
                AppendPrefSetter(text, MemberType, Key, "value");
            });

            if (saveMethod != null)
            {
                saveMethod.Modifiers = Weaver.Editor.Procedural.Scripting.AccessModifiers.Private | Weaver.Editor.Procedural.Scripting.AccessModifiers.Static;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// The full name of the prefs class to use when building the procedural injector script.
        /// </summary>
        protected virtual string PrefsTypeName => "UnityEngine.PlayerPrefs";

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Appends C# code to get the value of this pref from persistent storage.
        /// </summary>
        public void AppendPrefGetter(StringBuilder script, Type type, string key, object defaultValue)
        {
            if (type == typeof(bool))
            {
                if (defaultValue != null)
                    defaultValue = (bool)defaultValue ? 1 : 0;

                script.Append('(');
                AppendBasicPlayerPrefGetter(script, "Int", key, defaultValue);
                script.Append(" != 0)");
            }
            else if (type == typeof(int))
            {
                AppendBasicPlayerPrefGetter(script, "Int", key, defaultValue);
            }
            else if (type == typeof(float))
            {
                AppendBasicPlayerPrefGetter(script, "Float", key, defaultValue);
            }
            else if (type == typeof(string))
            {
                AppendBasicPlayerPrefGetter(script, "String", key, defaultValue);
            }
            else if (type.IsEnum)
            {
                script.Append('(').Append(type.GetNameCS()).Append(')');
                AppendBasicPlayerPrefGetter(script, "Int", key, defaultValue);
            }
        }

        /************************************************************************************************************************/

        private void AppendBasicPlayerPrefGetter(StringBuilder script, string type, string key, object defaultValue)
        {
            script.Append(PrefsTypeName)
                .Append(".Get")
                .Append(type)
                .Append("(\"")
                .Append(key)
                .Append("\"");

            var defaultValueInitializer = CSharpProcedural.GetInitializer(defaultValue);
            if (defaultValueInitializer != null)
                script.Append(", ").Append(defaultValueInitializer);

            script.Append(")");
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Appends C# code to save the value of this pref to persistent storage.
        /// </summary>
        public void AppendPrefSetter(StringBuilder script, Type type, string key, string valueName)
        {
            if (type == typeof(bool))
            {
                AppendBasicPlayerPrefSetter(script, "Int", key, valueName + " ? 1 : 0");
            }
            else if (type == typeof(int))
            {
                AppendBasicPlayerPrefSetter(script, "Int", key, valueName);
            }
            else if (type == typeof(float))
            {
                AppendBasicPlayerPrefSetter(script, "Float", key, valueName);
            }
            else if (type == typeof(string))
            {
                AppendBasicPlayerPrefSetter(script, "String", key, valueName);
            }
            else if (type.IsEnum)
            {
                AppendBasicPlayerPrefSetter(script, "Int", key, "(int)" + valueName);
            }
        }

        /************************************************************************************************************************/

        private void AppendBasicPlayerPrefSetter(StringBuilder script, string type, string key, string valueName)
        {
            script.Append(PrefsTypeName)
                .Append(".Set")
                .Append(type)
                .Append("(\"")
                .Append(key)
                .Append("\", ")
                .Append(valueName)
                .AppendLineConst(");");
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the inspector GUI for this attribute.</summary>
        public override void DoInspectorGUI()
        {
            GUILayout.BeginHorizontal();

            var label = GetTempLabelContent();

            EditorGUI.BeginChangeCheck();
            var value = GetValue();

            if (MemberType == typeof(bool))
            {
                value = EditorGUILayout.Toggle(label, (bool)value);
            }
            else if (MemberType == typeof(int))
            {
                value = EditorGUILayout.IntField(label, (int)value);
            }
            else if (MemberType == typeof(float))
            {
                value = EditorGUILayout.FloatField(label, (float)value);
            }
            else if (MemberType == typeof(string))
            {
                value = EditorGUILayout.TextField(label, (string)value);
            }

            if (EditorGUI.EndChangeCheck())
                SetValue(value);

            if (GUILayout.Button(WeaverEditorUtilities.TempContent("R", "Reset to default"), GUIStyles.SmallButtonStyle))
                SetValue(DefaultValue);

            GUILayout.EndHorizontal();

            CheckContextMenu();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="Texture"/> to use as an icon for this attribute.</summary>
        protected internal override Texture Icon => Icons.Pref;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Appends any optional properties that have been set on this attribute.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, ref bool isFirst)
        {
            base.AppendDetails(text, ref isFirst);

            if (Key != Member.GetNameCS())
                AppendDetail(text, ref isFirst, nameof(Key) + "=" + Key);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Adds various functions for this attribute to the `menu`.
        /// </summary>
        public override void AddItemsToMenu(GenericMenu menu)
        {
            base.AddItemsToMenu(menu);

            // Key (if different from the member name already shown).
            if (Key != Member.GetNameCS())
                menu.AddDisabledItem(new GUIContent("Key: " + Key));

            // Reset to Default.
            menu.AddItem(new GUIContent("Reset to Default"), false, () => SetValue(DefaultValue));

            WeaverEditorUtilities.AddLinkToURL(menu, "Help/Injetion Attribute Types", "/docs/asset-injection/injection-attribute-types");
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

