// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using System.Reflection;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using Weaver.Editor;
using Weaver.Editor.Procedural;
using Weaver.Editor.Window;
#endif

namespace Weaver
{
    /// <summary>
    /// Base class for attributes which define behaviours for automatically injecting values into static properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class InjectionAttribute : Attribute
#if UNITY_EDITOR
        , IInjector, IHasCustomMenu, WeaverWindow.IItem
#endif
    {
        /************************************************************************************************************************/
        #region Constructor Properties
        /************************************************************************************************************************/

        /// <summary>
        /// If set to true, this attribute won't be used in builds. Must be set for attributes inside
        /// <c>#if UNITY_EDITOR</c> regions since that fact can't be detected automatically.
        /// </summary>
        public bool EditorOnly { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// A description of the purpose of the attributed property to be shown in the Unity Editor.
        /// </summary>
        public string Tooltip { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// If set to true, this attribute will not give any errors when it is unable to initialize with an appropriate value.
        /// </summary>
        public bool Optional { get; set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// The field this attribute is attached to. Null if attached to a property.
        /// </summary>
        internal FieldInfo Field { get; private set; }

        /// <summary>[Editor-Only]
        /// The property this attribute is attached to. Null if attached to a field.
        /// </summary>
        internal PropertyInfo Property { get; private set; }

        /// <summary>[Editor-Only]
        /// The <see cref="FieldInfo"/> or <see cref="PropertyInfo"/> of the attributed member.
        /// </summary>
        internal MemberInfo Member { get; private set; }

        /// <summary>[Editor-Only]
        /// The <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> of the attributed member.
        /// </summary>
        internal Type MemberType { get; private set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Indicates whether the value of attributed member can be read.</summary>
        internal bool HasGetter { get; private set; }

        /// <summary>[Editor-Only] Indicates whether the value of attributed member can be read publicly.</summary>
        internal bool HasPublicGetter { get; private set; }

        /// <summary>[Editor-Only] Indicates whether the value of attributed member can be written publicly.</summary>
        internal bool HasPublicSetter { get; private set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Validation
        /************************************************************************************************************************/

        internal bool Validate(FieldInfo field)
        {
            Field = field;
            Member = field;
            MemberType = field.FieldType;

            if (field.IsLiteral)
            {
                Debug.LogWarning(ToString() + ": field is const.");
                return false;
            }

            HasGetter = true;
            if (field.DeclaringType.IsVisible)
            {
                HasPublicGetter = field.IsPublic;
                HasPublicSetter = field.IsPublic && !field.IsInitOnly;
            }

            return TryInitialize();
        }

        /************************************************************************************************************************/

        internal bool Validate(PropertyInfo property)
        {
            Property = property;
            Member = property;
            MemberType = property.PropertyType;

            var setter = property.GetSetMethod(true);
            if (setter == null)
            {
                var backingFieldName = "<" + property.Name + ">k__BackingField";
                Field = property.DeclaringType.GetField(backingFieldName, ReflectionUtilities.StaticBindings);
                if (Field == null)
                {
                    Debug.LogWarning(ToString() + ": property has no setter.");
                    return false;
                }
            }

            var getter = property.GetGetMethod(true);

            HasGetter = getter != null;
            if (property.DeclaringType.IsVisible)
            {
                HasPublicGetter = getter != null && getter.IsPublic;
                HasPublicSetter = Field == null && setter.IsPublic;
            }

            return TryInitialize();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// </summary>
        protected abstract bool TryInitialize();

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Logs a message indicating that this attribute is invalid, followed by a `message` explaining why.
        /// Returns false.
        /// </summary>
        protected bool LogThisInvalidAttribute(string message)
        {
            Debug.LogWarning(ToString() + " is invalid: " + message);
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Uses <see cref="object.ReferenceEquals"/> instead of the default <see cref="Attribute.Equals(object)"/>.</summary>
        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        /// <summary>[Editor-Only] Uses <see cref="object.ReferenceEquals"/> instead of the default <see cref="Attribute.Equals(object)"/>.</summary>
        public static bool operator ==(InjectionAttribute a, object b) => ReferenceEquals(a, b);

        /// <summary>[Editor-Only] Uses <see cref="object.ReferenceEquals"/> instead of the default <see cref="Attribute.Equals(object)"/>.</summary>
        public static bool operator !=(InjectionAttribute a, object b) => !ReferenceEquals(a, b);

        /// <summary>[Editor-Only] Returns the hash code for this instance.</summary>
        public override int GetHashCode() => base.GetHashCode();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Injection
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the attributed member's value should be assigned in Edit Mode, otherwise it will only
        /// be assigned in Play Mode and on startup in a build.
        /// </summary>
        public abstract bool InEditMode { get; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a value to be assigned to the attributed property.
        /// </summary>
        protected abstract object GetValueToInject();

        /************************************************************************************************************************/

        void IInjector.Inject() => InjectValue();

        /// <summary>[Editor-Only]
        /// Assigns the appropriate value to the attributed property.
        /// </summary>
        protected internal virtual void InjectValue()
        {
            if (!this.CanInject())
                return;

            try
            {
                var value = GetValueToInject();

                if (value == null || value.Equals(null))
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode && !Optional)
                        Debug.LogWarning(GetMissingValueMessage());

                    if (MemberType.IsValueType)
                        return;

                    value = null;
                }
                else if (!MemberType.IsAssignableFrom(value.GetType()))
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        Debug.LogError("Incompatible value for " + this + ": " + value);

                    return;
                }

                SetValue(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a message indicating that no value has been assigned for this attribute.
        /// </summary>
        protected virtual string GetMissingValueMessage()
        {
            return "No value has been assigned for " + this;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Tries to get the value of the attributed member. Catches and logs any exceptions.
        /// </summary>
        public virtual object GetValue()
        {
            if (!HasGetter)
                return null;

            try
            {
                if (Field != null && Property == null)
                {
                    return Field.GetValue(null);
                }
                else
                {
                    return Property.GetValue(null, null);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Tries to set the value of the attributed member. Catches and logs any exceptions.
        /// </summary>
        public virtual void SetValue(object value)
        {
            //Debug.LogTemp("SetValue " + this + " = " + value);

            try
            {
                if (Field != null)
                {
                    Field.SetValue(null, value);
                }
                else
                {
                    Property.SetValue(null, value, null);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether this attribute should currently be visible in the <see cref="WeaverWindow"/>.
        /// </summary>
        public virtual bool ShouldShow => true;

        /************************************************************************************************************************/

        Type WeaverWindow.IItem.GetPanelType(out Type secondaryPanel)
        {
            return ShowInPanelAttribute.GetPanelType(this, out secondaryPanel);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a reusable <see cref="GUIContent"/> containing the name, tooltip, and icon of this attribute.
        /// </summary>
        protected GUIContent GetTempLabelContent()
        {
            return WeaverEditorUtilities.TempContent(Member.GetNameCS(CSharp.NameVerbosity.Basic), GetInspectorTooltip(), Icon);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// The cached tooltip of this attribute. Should only be read by <see cref="GetInspectorTooltip"/>.
        /// </summary>
        protected string _InspectorTooltip;

        /// <summary>[Editor-Only]
        /// Builds, caches, and returns a tooltip message for this attribute.
        /// </summary>
        protected virtual string GetInspectorTooltip()
        {
            if (_InspectorTooltip == null)
            {
                _InspectorTooltip = ToString();
                if (Tooltip != null)
                    _InspectorTooltip += " " + Tooltip;
            }

            return _InspectorTooltip;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="Texture"/> to use as an icon for this attribute.</summary>
        protected internal abstract Texture Icon { get; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the inspector GUI for this attribute.</summary>
        public virtual void DoInspectorGUI()
        {
            EditorGUILayout.LabelField(
                GetTempLabelContent(),
                new GUIContent(WeaverEditorUtilities.GetAttributeDisplayString(GetType())));

            CheckContextMenu();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Calls <see cref="CheckContextMenu(Rect)"/> with the last <see cref="GUILayout"/> rect.
        /// </summary>
        public void CheckContextMenu()
        {
            CheckContextMenu(GUILayoutUtility.GetLastRect());
        }

        /// <summary>[Editor-Only]
        /// If the current event is a <see cref="EventType.ContextClick"/> inside the `area`, this method builds a menu
        /// using <see cref="AddItemsToMenu"/> and shows it.
        /// </summary>
        public void CheckContextMenu(Rect area)
        {
            var currentEvent = Event.current;

            if (currentEvent.type == EventType.ContextClick &&
                area.Contains(currentEvent.mousePosition))
            {
                currentEvent.Use();

                var menu = new GenericMenu();
                AddItemsToMenu(menu);
                menu.ShowAsContext();
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Adds various functions for this attribute to the `menu`.
        /// </summary>
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddDisabledItem(new GUIContent(ToString()));

            // Open Source File.
            var filePath = UnityScripts.GetSourcePath(Member.DeclaringType, out var isScript);
            if (isScript || filePath.StartsWith(Application.dataPath))
            {
                var sourceType = isScript ? "Script" : "Assembly";
                var displayPath = filePath.Replace("/", " -> ");
                menu.AddItem(new GUIContent("Open " + sourceType + ":    " + displayPath), false, () =>
                {
                    if (!isScript)
                    {
                        var trim = Application.dataPath.Length - 6;// 6 = "Assets".Length.
                        filePath = filePath.Substring(trim, filePath.Length - trim);
                    }

                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(filePath));
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Unable to Locate Source Script"));
            }

            WeaverEditorUtilities.AddLinkToURL(menu, "Help/Asset Injection", "/docs/asset-injection/using-asset-injection");
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Cached return value of <see cref="ToString"/>.</summary>
        protected string _ToString;

        /// <summary>[Editor-Only] Returns a description of this attribute.</summary>
        public override string ToString()
        {
            if (_ToString == null)
            {
                var text = WeaverUtilities.GetStringBuilder();

                text.Append(WeaverEditorUtilities.GetAttributeDisplayString(GetType()))
                    .Append(" ")
                    .Append(Member.GetNameCS())
                    .Append(" (")
                    .Append(MemberType.GetNameCS())
                    .Append(")");

                var start = text.Length;
                {
                    var isFirst = true;
                    AppendDetails(text, ref isFirst);
                }
                if (start < text.Length)
                    text.Append(" }");

                _ToString = text.ReleaseToString();
            }

            return _ToString;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Appends any optional properties that have been set on this attribute.
        /// </summary>
        protected virtual void AppendDetails(StringBuilder text, ref bool isFirst)
        {
            if (EditorOnly)
                AppendDetail(text, ref isFirst, nameof(EditorOnly));

            if (Optional)
                AppendDetail(text, ref isFirst, nameof(Optional));
        }

        /// <summary>[Editor-Only]
        /// Appends an opening bracket or comma depending on `isFirst` followed by the specified `detail` and sets
        /// `isFirst` to false. Intended for use within <see cref="AppendDetails"/>.
        /// </summary>
        protected void AppendDetail(StringBuilder text, ref bool isFirst, string detail)
        {
            text.Append(isFirst ? " { " : ", ").Append(detail);
            isFirst = false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called during a build by <see cref="UnityEditor.Build.IPreprocessBuildWithReport.OnPreprocessBuild"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing unless overridden.
        /// </remarks>
        protected internal virtual void OnStartBuild() { }
        void IInjector.OnStartBuild() => OnStartBuild();

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        /// <remarks>
        /// Does nothing unless overridden.
        /// </remarks>
        protected internal virtual void GatherInjectorDetails(InjectorScriptBuilder builder) { }
        void IInjector.GatherInjectorDetails(InjectorScriptBuilder builder) => GatherInjectorDetails(builder);

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the values this attribute
        /// wants assigned it.
        /// </summary>
        /// <remarks>
        /// Does nothing unless overridden.
        /// </remarks>
        protected internal virtual void SetupInjectorValues(InjectorScriptBuilder builder) { }
        void IInjector.SetupInjectorValues(InjectorScriptBuilder builder) => SetupInjectorValues(builder);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

