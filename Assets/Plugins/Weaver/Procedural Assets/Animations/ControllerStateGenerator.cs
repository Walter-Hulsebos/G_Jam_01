// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Weaver.Editor.Procedural.Scripting;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Internal] [Editor-Only]
    /// A utility for <see href="https://kybernetik.com.au/animancer">Animancer</see> which generates classes that
    /// inherit from <see href="https://kybernetik.com.au/animancer/api/Animancer/ControllerState">ControllerState</see>
    /// to wrap the parameters of a specific <see cref="AnimatorController"/> asset.
    /// </summary>
    /// <remarks>
    /// This adds the <c>Generate Controller State Script</c> function to the Context Menu for Animator Controller
    /// assets (usable via the Cog icon in the top right of the Inspector).
    /// </remarks>
    internal sealed class ControllerStateGenerator : ScriptBuilder
    {
        /************************************************************************************************************************/

        private const string
            ControllerStateFullName = "Animancer.ControllerState",
            ParameterIDName = "ParameterID";

        private static readonly Type
            ControllerState,
            ParameterID;

        static ControllerStateGenerator()
        {
            ControllerState = Type.GetType(ControllerStateFullName + ", Animancer");
            if (ControllerState == null)
            {
                ControllerState = Type.GetType(ControllerStateFullName + ", Animancer.Lite");
                if (ControllerState == null)
                    return;
            }

            ParameterID = ControllerState.GetNestedType(ParameterIDName);
        }

        /************************************************************************************************************************/

        private const string MenuPath = "CONTEXT/" + nameof(AnimatorController) + "/Generate Controller State Script";

        [MenuItem(MenuPath)]
        private static void Generate(MenuCommand command) => Generate((AnimatorController)command.context);

        [MenuItem(MenuPath, validate = true)]
        private static bool ValidateGenerate(MenuCommand command)
        {
            var controller = (AnimatorController)command.context;
            if (controller.parameters.Length > 0)
                return true;

            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine.states.Length > 0)
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        public static void Generate(AnimatorController controller)
        {
            if (ControllerState == null)
            {
                switch (EditorUtility.DisplayDialogComplex("Animancer is Required",
                        "This function generates a script for use with Animancer which is not currently in your project." +
                        "\n\nYou can continue without Animancer, but the generated script will cause compiler errors.",
                        "Generate", "Cancel", "Get Animancer"))
                {
                    case 0: break;
                    case 1: return;
                    case 2: Application.OpenURL("https://kybernetik.com.au/animancer/docs/download"); return;
                }
            }

            var className = CSharpProcedural.ValidateMemberName(controller.name) + "State";

            var path = AskWhereToSave(controller, className);
            if (string.IsNullOrEmpty(path))
                return;

            className = Path.GetFileNameWithoutExtension(path);

            var text = WeaverUtilities.GetStringBuilder();
            text.AppendLineConst("#pragma warning disable // All.");
            text.AppendLineConst();

            var builder = CreateScriptBuilder(controller, className);
            builder.PrepareToBuild(false, false);
            builder.AppendScript(text, 0);

            File.WriteAllText(path, text.ReleaseToString());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /************************************************************************************************************************/

        internal const int
            HashConstantsRegion = 0,
            ParameterWrappersRegion = 1,
            TransitionRegion = 2,
            UnityEditorSymbol = 0;

        /// <summary>Creates a new <see cref="ControllerStateGenerator"/>.</summary>
        public ControllerStateGenerator()
        {
            Regions = new[]
            {
                "Hash Constants",
                "Parameter Wrappers",
                "Transition",
            };

            CompilationSymbols = new[]
            {
                WeaverUtilities.UnityEditor,
            };
        }

        /************************************************************************************************************************/

        private static string AskWhereToSave(AnimatorController controller, string className)
        {
            var path = AssetDatabase.GetAssetPath(controller);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else
            {
                path = Path.GetDirectoryName(path);
            }

            return EditorUtility.SaveFilePanelInProject(
                "Generate Controller State Script",
                className,
                "cs",
                "Where would you like to save the generated script?",
                path);
        }

        /************************************************************************************************************************/

        private static ScriptBuilder CreateScriptBuilder(AnimatorController controller, string className)
        {
            var builder = new ControllerStateGenerator();

            //builder.SetName("NamespaceName");

            var parameters = controller.parameters;

            var stateType = builder.AddType(className);
            stateType.Modifiers = AccessModifiers.Public | AccessModifiers.Sealed;

            if (ControllerState != null)
                stateType.BaseType = ControllerState;
            else
                stateType.AppendBaseType = (text) => text.Append(ControllerStateFullName);

            stateType.CommentBuilder = (text) => text.Append("An <see cref=\"").Append(ControllerStateFullName)
                .Append("\"/> for the '").Append(controller.name).Append("' Animator Controller.");

            var hashToField = AddHashConstants(stateType, controller, parameters);
            AddConstructor(stateType, parameters, hashToField);
            AddParameterWrappers(stateType, parameters, hashToField);
            AddParameterCollectionInfo(stateType, parameters, hashToField);
            AddTransitionType(stateType);

            return builder;
        }

        /************************************************************************************************************************/

        private static Dictionary<int, FieldBuilder> AddHashConstants(TypeBuilder stateType,
            AnimatorController controller, AnimatorControllerParameter[] parameters)
        {
            var hashToField = new Dictionary<int, FieldBuilder>();

            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    AddHashConstant(stateType, hashToField, state.state.name, state.state.nameHash);
                }
            }

            foreach (var parameter in parameters)
            {
                AddHashConstant(stateType, hashToField, parameter.name, parameter.nameHash);
            }

            return hashToField;
        }

        private static void AddHashConstant(TypeBuilder stateType, Dictionary<int, FieldBuilder> hashToField,
            string name, int hash)
        {
            var field = AnimationsScriptBuilder.AddHashConstant(
                stateType, hashToField, name + "Hash", hash, HashConstantsRegion);
            field.CommentBuilder = (text) => text.Append(name);
        }

        /************************************************************************************************************************/

        private static readonly ParameterBuilder[] ConstructorParameters =
        {
            new ParameterBuilder(typeof(RuntimeAnimatorController), "controller"),
            new ParameterBuilder(typeof(bool), "keepStateOnStop", "false"),
        };

        private static void AddConstructor(TypeBuilder stateType, AnimatorControllerParameter[] parameters,
            Dictionary<int, FieldBuilder> hashToField)
        {
            var constructor = stateType.AddConstructor(ConstructorParameters,
                (text, indent) =>
                {
                    text.AppendLineConst("#if " + WeaverUtilities.UnityEditor);

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var field = hashToField[parameter.nameHash];

                        text.Indent(indent)
                            .Append($"new {ControllerStateFullName}.{ParameterIDName}(");
                        CSharpProcedural.AppendStringLiteral(text, parameter.name);
                        text.Append(", ")
                            .Append(field.Name)
                            .Append(").ValidateHasParameter(controller, ")
                            .Append(typeof(AnimatorControllerParameterType).GetNameCS())
                            .Append('.')
                            .Append(parameter.type)
                            .AppendLineConst(");");
                    }

                    text.AppendLineConst("#endif");
                });
            constructor.RegionIndex = ParameterWrappersRegion;
            constructor.AppendBaseParameters = (text) => text.Append("controller, keepStateOnStop");
        }

        /************************************************************************************************************************/

        private static void AddParameterWrappers(TypeBuilder stateType, AnimatorControllerParameter[] parameters,
            Dictionary<int, FieldBuilder> hashToField)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var hashField = hashToField[parameter.nameHash];

                Type parameterType;
                string parameterTypeName;
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float: parameterType = typeof(float); parameterTypeName = "Float"; break;
                    case AnimatorControllerParameterType.Int: parameterType = typeof(int); parameterTypeName = "Integer"; break;
                    case AnimatorControllerParameterType.Bool: parameterType = typeof(bool); parameterTypeName = "Bool"; break;

                    case AnimatorControllerParameterType.Trigger:
                        var method = stateType.AddMethod("Set" + parameter.name,
                            (text, indent) => text.Append("Playable.SetTrigger(").Append(hashField.Name).Append(");"));
                        method.RegionIndex = ParameterWrappersRegion;
                        method.Modifiers = AccessModifiers.Public;
                        method.CommentBuilder = (text) =>
                            text.Append("Sets the '").Append(parameter.name).Append("' trigger in the Animator Controller.");

                        method = stateType.AddMethod("Reset" + parameter.name,
                            (text, indent) => text.Append("Playable.ResetTrigger(").Append(hashField.Name).Append(");"));
                        method.RegionIndex = ParameterWrappersRegion;
                        method.Modifiers = AccessModifiers.Public;
                        method.CommentBuilder = (text) =>
                            text.Append("Resets the '").Append(parameter.name).Append("' trigger in the Animator Controller.");
                        continue;

                    default:
                        throw new ArgumentException($"Unsupported {nameof(AnimatorControllerParameterType)}: {parameter.type}");
                }

                var property = stateType.AddProperty(parameter.name, parameterType,
                    (text, indent) =>
                    {
                        text.Append("Playable.Get")
                            .Append(parameterTypeName)
                            .Append("(")
                            .Append(hashField.Name)
                            .Append(");")
                            .AppendLineConst();
                    },
                    (text, indent) =>
                    {
                        text.Append("Playable.Set")
                            .Append(parameterTypeName)
                            .Append("(")
                            .Append(hashField.Name)
                            .Append(", value);")
                            .AppendLineConst();
                    });

                property.RegionIndex = ParameterWrappersRegion;
                property.Modifiers = AccessModifiers.Public;
                property.CommentBuilder = (text) =>
                    text.Append("The value of the '").Append(parameter.name).Append("' parameter in the Animator Controller.");
            }
        }

        /************************************************************************************************************************/

        private static readonly ParameterBuilder[]
            GetParameterHashParameters = { new ParameterBuilder(typeof(int), "index") };

        private static void AddParameterCollectionInfo(TypeBuilder stateType, AnimatorControllerParameter[] parameters,
            Dictionary<int, FieldBuilder> hashToField)
        {
            var parameterCount = stateType.AddProperty("ParameterCount", typeof(int),
                (text, indent) => text.Append(parameters.Length).AppendLineConst(";"),
                null);
            parameterCount.RegionIndex = ParameterWrappersRegion;
            parameterCount.Modifiers = AccessModifiers.Public | AccessModifiers.Override;

            var getParameterHash = stateType.AddMethod("GetParameterHash", typeof(int), GetParameterHashParameters,
                (text, indent) =>
                {
                    text.Indent(indent).AppendLineConst("switch (index)");
                    text.OpenScope(ref indent);

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var hashField = hashToField[parameter.nameHash];
                        text.Indent(indent)
                            .Append("case ")
                            .Append(i)
                            .Append(": return ")
                            .Append(hashField.Name)
                            .Append(";// ")
                            .Append(parameter.name)
                            .AppendLineConst('.');
                    }

                    text.Indent(indent)
                        .Append("default: throw new ")
                        .Append(typeof(ArgumentOutOfRangeException).GetNameCS())
                        .AppendLineConst("(nameof(index));");

                    text.CloseScope(ref indent);
                });
            getParameterHash.RegionIndex = ParameterWrappersRegion;
            getParameterHash.Modifiers = AccessModifiers.Public | AccessModifiers.Override;
        }

        /************************************************************************************************************************/

        private static void AddTransitionType(TypeBuilder stateType)
        {
            var transitionType = stateType.AddNestedType("Transition");
            transitionType.RegionIndex = TransitionRegion;
            transitionType.Modifiers = AccessModifiers.Public | AccessModifiers.New;
            transitionType.AppendBaseType = (text) => text.Append("Transition<").Append(stateType.Name).Append('>');
            transitionType.CommentBuilder = (text) => text
                .Append("A serializable <see cref=\"Animancer.ITransition\"/> which can create a <see cref=\"")
                .Append(stateType.Name)
                .Append("\"/> when passed into <see cref=\"Animancer.AnimancerPlayable.Play(Animancer.ITransition)\"/>.");
            transitionType.Attributes = new Type[] { typeof(SerializableAttribute) };

            var createState = transitionType.AddMethod("CreateState", stateType, (text, indent) =>
            {
                text.Append("State = new ").Append(stateType.Name).Append("(Controller, KeepStateOnStop);");
            });
            createState.Modifiers = AccessModifiers.Public | AccessModifiers.Override;
            createState.CommentBuilder = (text) => text
                .Append("Creates and returns a new <see cref=\"")
                .Append(stateType.Name)
                .Append("\"/>.");

            var drawerType = transitionType.AddNestedType("Drawer");
            drawerType.CompilationSymbolIndex = UnityEditorSymbol;
            drawerType.Modifiers = AccessModifiers.Public | AccessModifiers.New;
            drawerType.AppendBaseType = (text) => text.Append(ControllerStateFullName).Append(".Transition.Drawer");
            drawerType.CommentBuilder = (text) => text.Append("[Editor-Only] Draws the Inspector GUI for a <see cref=\"Transition\"/>.");
            drawerType.Attributes = new Type[] { typeof(CustomPropertyDrawer) };
            drawerType.AttributeConstructorBuilders = new Action<StringBuilder>[]
            {
                (text) => text.Append("typeof(Transition), true"),
            };
        }

        /************************************************************************************************************************/

        /// <summary>No logging.</summary>
        public override void LogRebuildReason(string reason) { }

        /************************************************************************************************************************/
    }
}

#endif

