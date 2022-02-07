// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Weaver.Editor;
using Weaver.Editor.Procedural;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="Attribute"/> for static parameterless methods to have Weaver invoke them once the static
    /// dependency injection is complete.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInjectionCompleteAttribute : Attribute
#if UNITY_EDITOR
        , IInjector
#endif
    {
        /************************************************************************************************************************/

        /// <summary>
        /// If set to true, this attribute won't be used in builds. Must be set for attributes inside #if UNITY_EDITOR
        /// regions since that fact can't be detected automatically.
        /// </summary>
        public bool EditorOnly { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the attributed method should be invoked in Edit Mode, otherwise it will only be invoked
        /// in Play Mode and on startup in a build.
        /// </summary>
        public bool InEditMode { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines the order in which the attributed method should be executed in relation to other injection
        /// events. Methods are executed from lowest <see cref="ExecutionTime"/> to highest.
        /// </summary>
        public int ExecutionTime { get; set; }

        /************************************************************************************************************************/

        /// <summary>Compares the <see cref="ExecutionTime"/> of each attribute.</summary>
        public static int CompareExecutionTime(OnInjectionCompleteAttribute a, OnInjectionCompleteAttribute b)
            => a.ExecutionTime.CompareTo(b.ExecutionTime);

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The method this attribute is attached to.</summary>
        private MethodInfo _Method;

        /************************************************************************************************************************/

        internal bool TryInitialize(MethodInfo method)
        {
            _Method = method;

            if (method.GetParameters().Length != 0)
            {
                Debug.LogWarning(ToString() + ": the attributed method must have no parameters.");
                return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        void IInjector.Inject() => Invoke();

        /// <summary>[Editor-Only]
        /// Invokes the attributed member. Catches and logs any exceptions.
        /// </summary>
        public void Invoke()
        {
            if (!InEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            try
            {
                _Method.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a description of this attribute and the attributed method.</summary>
        public override string ToString() => $"{WeaverEditorUtilities.GetAttributeDisplayString(GetType())} {_Method.GetNameCS()}";

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the GUI for this attribute in the inspector.</summary>
        public void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                _Method.GetNameCS(),
                GetType().GetNameCS());
        }

        /************************************************************************************************************************/

        void IInjector.OnStartBuild() { }

        void IInjector.GatherInjectorDetails(InjectorScriptBuilder builder)
        {
            builder.AddToMethod("Awake", (text, indent) =>
            {
                builder.AppendTry(text, ref indent, this);

                if (_Method.DeclaringType.IsVisible)
                {
                    if (_Method.IsPublic)
                    {
                        text.Indent(indent)
                            .Append(_Method.DeclaringType.GetNameCS())
                            .Append('.')
                            .Append(_Method.Name)
                            .AppendLineConst("();");
                    }
                    else
                    {
                        text.Indent(indent)
                            .Append("typeof(")
                            .Append(_Method.DeclaringType.GetNameCS())
                            .Append(").InvokeMember(\"")
                            .Append(_Method.Name)
                            .Append("\", ")
                            .Append(builder.GetStaticBindingsFieldName())
                            .AppendLineConst(" | System.Reflection.BindingFlags.InvokeMethod, null, null, null);");

                    }
                }
                else
                {
                    Debug.LogWarning("Non-public types aren't yet supported by Weaver injection attributes.");
                }

                builder.AppendCatch(text, ref indent);
            });
        }

        void IInjector.SetupInjectorValues(InjectorScriptBuilder builder) { }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

