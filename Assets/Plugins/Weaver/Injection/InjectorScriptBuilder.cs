// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only]
    /// A system for handling the build process so that members with <see cref="InjectionAttribute"/>s can be
    /// initialized efficiently in a runtime build.
    /// </summary>
    public sealed class InjectorScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        private const string
            TypeName = "Injector",
            ObjectsFieldName = "_Objects",
            LocalObjectName = "obj";

        private readonly List<Object> Objects = new List<Object>();

        private static bool _JustStartedBuild;

        private bool _HasStaticBindingsField;

        /// <summary>
        /// An event triggered when the system is finished gathering the details of this procedural script.
        /// </summary>
        public event Action OnGatheringComplete;

        /************************************************************************************************************************/
        #region Procedural Asset
        /************************************************************************************************************************/

        [AssetReference(FileName = TypeName, DisableAutoFind = true, Optional = true,
            Tooltip = "This script is automatically generated when building your project")]
        [ProceduralAsset(AutoGenerateOnBuild = true, AutoGenerateOnSave = true,
            CheckShouldShow = nameof(ShouldShowScript),
            CheckShouldGenerate = nameof(ShouldGenerateScript))]
        private static MonoScript Script { get; set; }

        /************************************************************************************************************************/

        private static bool ShouldShowScript => Instance.Enabled;

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => WeaverSettings.Injection.enableRuntimeInjection;

        private static bool ShouldGenerateScript => Instance.ShouldBuild();

        /************************************************************************************************************************/

        [ScriptGenerator.Alias(nameof(Weaver))]
        private static void GenerateScript(StringBuilder text)
        {
            if (!Instance.ShouldBuild())
            {
                text.Length = 0;
                return;
            }

            // Don't log the save message when building since this script will always be generated at that time.
            if (WeaverEditorUtilities.IsBuilding)
                ScriptGenerator.DisableSaveMessage();

            _JustStartedBuild = true;

            Instance.BuildScript(text);
        }

        /************************************************************************************************************************/

        private const int
            RuntimeSymbolIndex = 0,
            UnityEditorSymbolIndex = 1;

        internal static readonly InjectorScriptBuilder Instance = new InjectorScriptBuilder();

        private InjectorScriptBuilder()
            : base(GenerateScript)
        {
            CompilationSymbols = new[]
            {
                    "! UNITY_EDITOR // Runtime.",
                    "UNITY_EDITOR // Editor."
                };
        }

        /************************************************************************************************************************/

        /// <summary>This script doesn't need to be accessed directly so it is always in the <see cref="Weaver"/> namespace.</summary>
        public override string Namespace => nameof(Weaver);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Script Details
        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether this procedural script should be rebuilt.
        /// </summary>
        public override bool ShouldBuild()
        {
            base.ShouldBuild();

            if (!Enabled)
                return false;

            // Regenerate the script if:
            return
                WeaverEditorUtilities.IsBuilding ||// We are building.
                WeaverEditorUtilities.ForceGenerate ||// Or we are being forced to generate.
                Script == null;// Or the script doesn't exist.
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<string, List<AppendFunction>>
            MethodNameToContents = new Dictionary<string, List<AppendFunction>>();

        /************************************************************************************************************************/

        /// <summary>Gathers the member details and configures the <see cref="SimpleScriptBuilder.RootType"/>.</summary>
        protected override void GatherScriptDetails()
        {
            Objects.Clear();
            MethodNameToContents.Clear();
            _HasStaticBindingsField = false;
            OnGatheringComplete = null;

            // Configure the root type.
            RootType.Modifiers = AccessModifiers.Internal | AccessModifiers.Sealed;
            RootType.BaseType = typeof(MonoBehaviour);
            RootType.SetAttributes(
                typeof(DefaultExecutionOrder),
                typeof(AddComponentMenu));
            RootType.SetAttributeConstructorBuilders(
                (text) => text.Append("-20000"),
                (text) => text.Append("\"\""));

            // Add the object references field.
            var field = RootType.AddField(ObjectsFieldName, typeof(Object[]));
            field.CommentBuilder = null;
            field.SetAttributes(typeof(SerializeField));
            field.Modifiers = AccessModifiers.Private;

            // Configure the timer.
            CreateTimerField();

            // Gather the details of all injectors.
            foreach (var injector in InjectorManager.AllInjectors)
                if (!injector.EditorOnly)
                    injector.GatherInjectorDetails(this);

            OnGatheringComplete?.Invoke();

            // Add the dummy property.
            var dummy = RootType.AddProperty("Dummy", typeof(bool), null, (text, indent) => text.Append(""));
            dummy.SetAttributes(typeof(AssetReferenceAttribute));
            dummy.Modifiers = AccessModifiers.Private;
            dummy.CompilationSymbolIndex = UnityEditorSymbolIndex;
            dummy.CommentBuilder =
                (text) => text.Append("This property is here to give a compile error if you delete Weaver so you delete this script as well.");

            // The method builders each have their contents list now so we can clear the dictionary.
            MethodNameToContents.Clear();
            Objects.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds the `contents` to the list of functions that will be called to build the specified `method`.
        /// </summary>
        public MethodBuilder AddToMethod(string method, InjectionAttribute attribute, AppendFunction contents)
        {
            return AddToMethod(method, (text, indent) =>
            {
                AppendTry(text, ref indent, attribute);
                contents(text, indent);
                AppendCatch(text, ref indent);
            });
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds the `contents` to the list of functions that will be called to build the specified `method`.
        /// </summary>
        public MethodBuilder AddToMethod(string method, AppendFunction contents)
        {
            if (!MethodNameToContents.TryGetValue(method, out var methodContents))
            {
                methodContents = new List<AppendFunction>
                {
                    contents
                };

                MethodNameToContents.Add(method, methodContents);

                var methodBuilder = RootType.AddMethod(method, (text, indent) =>
                {
                    AppendResetTimer(text, indent);

                    for (int i = 0; i < methodContents.Count; i++)
                    {
                        if (i > 0)
                            text.AppendLineConst();

                        methodContents[i](text, indent);
                    }

                    AppendStopTimer(text, indent, true, method);
                });
                methodBuilder.CommentBuilder = null;
                methodBuilder.Modifiers = AccessModifiers.Private;
                methodBuilder.CompilationSymbolIndex = RuntimeSymbolIndex;
                return methodBuilder;
            }
            else
            {
                methodContents.Add(contents);
                return null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether a method with the specified name has already been created.
        /// </summary>
        public bool HasMethod(string method)
        {
            return MethodNameToContents.ContainsKey(method);
        }

        /************************************************************************************************************************/
        #region Timer
        /************************************************************************************************************************/

        private const string StopwatchFieldName = "_Stopwatch";

        private void CreateTimerField()
        {
            if (!WeaverSettings.Injection.timeInjectorCode)
                return;

            // If the use of Stopwatch is removed, also remove the reference to System.dll from this project.

            var field = RootType.AddField(StopwatchFieldName, typeof(System.Diagnostics.Stopwatch));
            field.CommentBuilder = null;
            field.Modifiers = AccessModifiers.Private | AccessModifiers.Static | AccessModifiers.Readonly;
            field.AppendInitializer = (text, indent, value) => text.Append(" = new System.Diagnostics.Stopwatch()");
            field.ValueEquals = null;
            field.CompilationSymbolIndex = RuntimeSymbolIndex;
        }

        /************************************************************************************************************************/

        private static void AppendResetTimer(StringBuilder text, int indent)
        {
            if (!WeaverSettings.Injection.timeInjectorCode)
                return;

            text.Indent(indent).AppendLineConst(StopwatchFieldName + ".Reset();");
            text.Indent(indent).AppendLineConst(StopwatchFieldName + ".Start();");
            text.AppendLineConst();
        }

        /************************************************************************************************************************/

        private static void AppendStopTimer(StringBuilder text, int indent, bool prefixBlankLine, string methodName)
        {
            if (!WeaverSettings.Injection.timeInjectorCode)
                return;

            if (prefixBlankLine)
                text.AppendLineConst();

            text.Indent(indent)
                .AppendLineConst(StopwatchFieldName + ".Stop();");
            text.Indent(indent)
                .Append("UnityEngine.Debug.Log(\"Weaver." + TypeName + ".")
                .Append(methodName)
                .AppendLineConst(" completed in \" + " + StopwatchFieldName + ".ElapsedMilliseconds + \"ms.\");");
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Try/Catch
        /************************************************************************************************************************/

        private int _ScriptLengthBeforeAttribute = -1;
        private int _ScriptLengthOnAttribute;

        /************************************************************************************************************************/

        /// <summary>Appends <c>try {</c> with a comment for the `context`.</summary>
        /// <remarks>Must be followed by a call to <see cref="AppendCatch"/>.</remarks>
        public void AppendTry(StringBuilder text, ref int indent, object context)
        {
            Debug.Assert(_ScriptLengthBeforeAttribute == -1,
                $"{nameof(AppendTry)} must be followed by a call to {nameof(AppendCatch)}.");

            _ScriptLengthBeforeAttribute = text.Length;

            if (context != null)
                text.Indent(indent)
                    .Append("// ")
                    .Append(context)
                    .AppendLineConst(".");

            if (!WeaverSettings.Injection.catchInjectorExceptions)
            {
                text.OpenScope(ref indent);
            }
            else
            {
                text.Indent(indent).AppendLineConst("try");
                text.OpenScope(ref indent);
            }

            _ScriptLengthOnAttribute = text.Length;
        }

        /************************************************************************************************************************/

        /// <summary>Appends a <c>catch</c> block.</summary>
        /// <remarks>Must follow a call to <see cref="AppendTry"/>.</remarks>
        public void AppendCatch(StringBuilder text, ref int indent)
        {
            Debug.Assert(_ScriptLengthBeforeAttribute != -1,
                $"{nameof(AppendCatch)} can only be called after {nameof(AppendTry)}.");

            // If nothing was built by the attribute, remove its opening comment and scope.
            if (text.Length == _ScriptLengthOnAttribute)
            {
                indent--;
                text.Length = _ScriptLengthBeforeAttribute;
            }
            else
            {
                text.CloseScope(ref indent);

                if (WeaverSettings.Injection.catchInjectorExceptions)
                    text.Indent(indent).AppendLineConst(
                        "catch (System.Exception exception) { UnityEngine.Debug.LogException(exception); }");
            }

            _ScriptLengthBeforeAttribute = -1;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Get Serialized Reference
        /************************************************************************************************************************/

        /// <summary>
        /// Appends C# code which retrieves the appropriate value from a serialized field and assigns it to the target
        /// property.
        /// </summary>
        public string AppendGetSerializedReference(StringBuilder text, int indent, string fieldName = LocalObjectName)
        {
            AppendGetSerializedReference(text, indent, fieldName, null);
            return fieldName;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends C# code which retrieves the appropriate value from a serialized field and assigns it to the target
        /// property.
        /// </summary>
        public string AppendGetSerializedReference(StringBuilder text, int indent, Type castTo, string fieldName = LocalObjectName)
        {
            AppendGetSerializedReference(text, indent, fieldName, castTo.GetNameCS());
            return fieldName;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends C# code which retrieves the appropriate value from a serialized field and assigns it to the target
        /// property.
        /// </summary>
        public void AppendGetSerializedReference(StringBuilder text, int indent, string fieldName, string castTo)
        {
            text.Indent(indent);
            text.Append("var ");
            text.Append(fieldName);
            text.Append(" = " + ObjectsFieldName + "[");
            text.Append(Objects.Count);
            text.Append("]");
            if (castTo != null)
                text.Append(" as ").Append(castTo);
            text.AppendLineConst(";");

            // We don't know what order this script will be generated in relation to other procedural assets, so
            // other assets might not be able to be assigned yet. Instead we just add empty elements to the list
            // and then refill it with the proper values during OnPostProcessScene to assign to the scene object.

            Objects.Add(null);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends C# code which retrieves the appropriate value from a serialized field and assigns it to the target
        /// property.
        /// </summary>
        public string AppendGetSerializedReference(StringBuilder text, int indent, Type type, bool dontDestroyOnLoad, string fieldName = LocalObjectName)
        {
            AppendGetSerializedReference(text, indent, type, fieldName);

            if (dontDestroyOnLoad && IsPrefabType(type))
                AppendDontDestroyOnLoad(text, indent, LocalObjectName);

            return fieldName;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the `type` is <see cref="GameObject"/> or is derived from <see cref="Component"/>.
        /// </summary>
        public static bool IsPrefabType(Type type)
        {
            return
                typeof(GameObject) == type ||
                typeof(Component).IsAssignableFrom(type);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>
        /// Appends a call to <see cref="Object.DontDestroyOnLoad"/>.
        /// </summary>
        public void AppendDontDestroyOnLoad(StringBuilder text, int indent, string objectName)
        {
            text.Indent(indent).Append("DontDestroyOnLoad(").Append(objectName).AppendLineConst(");");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends a call to <see cref="Debug.Log(object)"/>.
        /// </summary>
        public void AppendLog(StringBuilder text, int indent, string message)
        {
            text.Indent(indent).Append("UnityEngine.Debug.Log(\"").Append(message).AppendLineConst("\");");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends code to get the <see cref="MemberInfo"/> of the specified `attribute`.
        /// </summary>
        public bool AppendGetMember(StringBuilder text, InjectionAttribute attribute, bool setter)
        {
            bool isField;

            text.Append("typeof(");
            text.Append(attribute.Member.DeclaringType.GetNameCS());

            if (attribute.Field != null && (attribute.Property == null || setter))
            {
                text.Append(").GetField(\"");
                text.Append(attribute.Field.Name);
                isField = true;
            }
            else
            {
                text.Append(").GetProperty(\"");
                text.Append(attribute.Member.Name);
                isField = false;
            }

            text.Append("\", ")
                .Append(GetStaticBindingsFieldName())
                .Append(')');

            return isField;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the name of a constant containing <see cref="BindingFlags.Public"/> |
        /// <see cref="BindingFlags.NonPublic"/> | <see cref="BindingFlags.Static"/> and ensures that it will be built
        /// into the script.
        /// </summary>
        public string GetStaticBindingsFieldName()
        {
            const string Name = "StaticBindings";

            if (!_HasStaticBindingsField)
            {
                _HasStaticBindingsField = true;

                var field = RootType.AddField(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                field.CommentBuilder = null;
                field.Modifiers = AccessModifiers.Private | AccessModifiers.Const;
                field.CompilationSymbolIndex = RuntimeSymbolIndex;
                field.AppendInitializer = (text, indent, value) => text.Append(
                    " = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static");
            }

            return Name;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends code to set the attributed member to the specified `value`.
        /// </summary>
        public void AppendSetValue(StringBuilder text, int indent, InjectionAttribute attribute, string value = LocalObjectName)
        {
            if (WeaverSettings.Injection.logInjectorCode)
                AppendLog(text, indent, "Weaver Injector Set " + attribute);

            text.Indent(indent);

            if (attribute.HasPublicSetter)
            {
                text.Append(attribute.Member.GetNameCS())
                    .Append(" = ")
                    .Append(value)
                    .Append(" as ")
                    .Append(attribute.MemberType.GetNameCS())
                    .AppendLineConst(";");
            }
            else
            {
                var isField = AppendGetMember(text, attribute, true);
                text.Append(".SetValue(null, ");
                text.Append(value);
                text.AppendLineConst(isField ? ");" : ", null);");
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends code to get the value of the attributed member.
        /// </summary>
        public void AppendGetValue(StringBuilder text, int indent, InjectionAttribute attribute, string objectName = LocalObjectName)
        {
            if (WeaverSettings.Injection.logInjectorCode)
                AppendLog(text, indent, "Weaver Injector Get " + attribute);

            text.Indent(indent)
                .Append("var ")
                .Append(objectName)
                .Append(" = ");
            AppendGetValue(text, attribute);
            text.AppendLineConst(";");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends code to get the value of the attributed member.
        /// </summary>
        public void AppendGetValue(StringBuilder text, InjectionAttribute attribute)
        {
            if (attribute.HasPublicGetter)
            {
                text.Append(attribute.Member.GetNameCS());
            }
            else
            {
                text.Append('(')
                    .Append(attribute.MemberType.GetNameCS())
                    .Append(')');

                var isField = AppendGetMember(text, attribute, false);
                text.Append(isField ? ".GetValue(null)" : ".GetValue(null, null)");
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>
        /// Called by Unity when building a scene. Creates a <see cref="GameObject"/> in the first scene being built
        /// with the procedurally generated Injector script on it and assigns references to all the
        /// <see cref="AssetInjectionAttribute"/> objects so that script can initialize them at runtime.
        /// <para></para>
        /// This method also gets triggered when loading a scene in Play Mode, but only after the scene is
        /// initialized, so we do nothing here and use a [RuntimeInitializeOnLoad] attribute in <see cref="Startup"/>.
        /// </summary>
        [PostProcessScene]
        private static void OnPostProcessScene()
        {
            // Only run this method in the first scene in a build (the scene that gets loaded on startup).

            if (!_JustStartedBuild ||
                !BuildPipeline.isBuildingPlayer)
                return;

            _JustStartedBuild = false;
            Instance.Objects.Clear();

            if (Script == null)
                throw new MissingReferenceException($"{TypeName} script hasn't been generated and assigned to the {nameof(WeaverSettings)}");

            var initializerType = Script.GetClass();
            if (initializerType == null)
                throw new Exception($"Unable to get procedurally generated {nameof(Weaver)}.{TypeName}" +
                    $" class from {AssetDatabase.GetAssetPath(Script)}");

            WeaverEditorUtilities.ClearAssetPathCache();

            var initializerComponent = new GameObject(TypeName).AddComponent(initializerType);

            foreach (var injector in InjectorManager.AllInjectors)
                if (!injector.EditorOnly)
                    injector.SetupInjectorValues(Instance);

            var objects = Instance.Objects.ToArray();

            var objectsField = initializerType.GetField(ObjectsFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            objectsField.SetValue(initializerComponent, objects);

            Instance.Objects.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `obj` to the list of references for the injector script to serialize.</summary>
        /// <exception cref="ArgumentNullException">The `obj` is <c>null</c>.</exception>
        public void AddObjectReference(Object obj, object context)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", context?.ToString());

            Objects.Add(obj);
        }

        /************************************************************************************************************************/
    }
}

#endif

