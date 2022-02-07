// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Scans your project for all <see cref="AnimatorController"/> assets to gather the hashes of all states and
    /// parameters then procedurally generates a script containing constants wrapper methods for each of them.
    /// </summary>
    public sealed class AnimationsScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/
        #region Gather Animation Details
        /************************************************************************************************************************/

        private static readonly Dictionary<int, FieldBuilder>
            HashToField = new Dictionary<int, FieldBuilder>();
        private static readonly Dictionary<int, AnimatorControllerParameterType>
            HashToParameter = new Dictionary<int, AnimatorControllerParameterType>();

        /************************************************************************************************************************/

        internal static AnimationsScriptBuilder Instance { get; private set; }

        /************************************************************************************************************************/

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => WeaverSettings.Animations.enabled;

        /************************************************************************************************************************/

        internal const int
            HashConstantsRegion = 0,
            ParameterWrappersRegion = 1;

        /// <summary>Creates a new <see cref="AnimationsScriptBuilder"/>.</summary>
        public AnimationsScriptBuilder(Action<StringBuilder> generatorMethod)
            : base(generatorMethod)
        {
            Instance = this;

            Regions = new[]
            {
                "Hash Constants",
                "Parameter Wrappers",
            };
        }

        /************************************************************************************************************************/

        /// <summary>Gather the animation assets in the project and build the script structure.</summary>
        protected override void GatherScriptDetails()
        {
            HashToField.Clear();
            HashToParameter.Clear();

            AddHashToStringMethod();
            AddStateGetNameMethod();

            var controllers = GatherAnimatorControllers();
            foreach (var controller in controllers.Values)
            {
                if (controller == null)
                    continue;

                GatherStates(controller);
                GatherParameters(controller);
            }
        }

        /************************************************************************************************************************/

        private void GatherStates(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    AddHashConstant(state.state.name, state.state.nameHash);
                }
            }
        }

        /************************************************************************************************************************/

        private FieldBuilder AddHashConstant(string nameSource, int hash)
            => AddHashConstant(RootType, HashToField, nameSource, hash, HashConstantsRegion);

        /// <summary>Adds a <c>const int</c> field containing the `hash` value.</summary>
        public static FieldBuilder AddHashConstant(
            TypeBuilder type, Dictionary<int, FieldBuilder> hashToField, string nameSource, int hash, int region)
        {
            // If the hash is already occupied by a different string, warn about a hash collision.
            if (hashToField.TryGetValue(hash, out var existingField))
            {
                if (existingField.NameSource != nameSource)
                {
                    Debug.LogWarning(
                        $"Hash collision: '<I>{existingField.NameSource}</I>' and '<I>{nameSource}</I>' both have the hash value {hash}");
                }

                // Even if it's occupied by the same string, do nothing.
                return existingField;
            }

            // Store the hash and make a field for it.
            var field = type.AddField(nameSource, hash);
            field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
            field.RegionIndex = region;
            hashToField.Add(hash, field);
            return field;
        }

        /************************************************************************************************************************/
        #region Gather Animator Controllers
        /************************************************************************************************************************/

        private static readonly Dictionary<string, AnimatorController>
            GuidToAnimatorController = new Dictionary<string, AnimatorController>();

        private static IEnumerator _PreGatherAnimatorControllersEnumerator;
        private static EditorApplication.CallbackFunction _PreGatherAnimatorControllers;

        /************************************************************************************************************************/

        private static Dictionary<string, AnimatorController> GatherAnimatorControllers()
        {
            if (_PreGatherAnimatorControllersEnumerator != null)
            {
                while (_PreGatherAnimatorControllersEnumerator.MoveNext()) { }
            }

            var guids = AssetDatabase.FindAssets("t:" + nameof(AnimatorController));
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                if (!GuidToAnimatorController.ContainsKey(guid))
                    LoadAnimatorController(guid);
            }

            return GuidToAnimatorController;
        }

        /************************************************************************************************************************/

        static AnimationsScriptBuilder()
        {
            _PreGatherAnimatorControllersEnumerator = PreGatherAnimatorControllers();
            _PreGatherAnimatorControllers = WeaverEditorUtilities.EditorStartCoroutine(_PreGatherAnimatorControllersEnumerator);
        }

        /************************************************************************************************************************/

        private static IEnumerator PreGatherAnimatorControllers()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(AnimatorController));
            for (int i = 0; i < guids.Length; i++)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    break;

                yield return null;
                LoadAnimatorController(guids[i]);
            }

            EditorApplication.update -= _PreGatherAnimatorControllers;
            _PreGatherAnimatorControllersEnumerator = null;
            _PreGatherAnimatorControllers = null;
        }

        /************************************************************************************************************************/

        private static void LoadAnimatorController(string guid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            GuidToAnimatorController.Add(guid, controller);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Hash to String Method
        /************************************************************************************************************************/

        private const string
            HashToStringMethodName = "HashToString";
        private static readonly ParameterBuilder[]
            HashToStringParameters = { new ParameterBuilder(typeof(int), "hash") };
        private static Func<int, string>
            _CompiledHashToStringMethod;

        /************************************************************************************************************************/

        private void AddHashToStringMethod()
        {
            var method = RootType.AddMethod(HashToStringMethodName, typeof(string), HashToStringParameters,
                (text, indent) =>
                {
                    string[] memberNames, nameSources;

                    var stringCount = HashToField.Count;
                    var obsoleteFieldCount = 0;
                    if (RootType.ObsoleteMembers != null)
                    {
                        memberNames = new string[HashToField.Count + RootType.ObsoleteMembers.Count];
                        nameSources = new string[memberNames.Length];

                        GetCompiledHashToStringMethod();

                        // Gather obsolete fields.
                        for (int i = 0; i < RootType.ObsoleteMembers.Count; i++)
                        {
                            if (RootType.ObsoleteMembers[i] is FieldInfo field &&
                                field.IsLiteral &&
                                field.FieldType == typeof(int))
                            {
                                memberNames[obsoleteFieldCount] = field.Name;
                                nameSources[obsoleteFieldCount] = _CompiledHashToStringMethod((int)field.GetValue(null));
                                obsoleteFieldCount++;
                            }
                        }

                        stringCount += obsoleteFieldCount;

                    }
                    else
                    {
                        memberNames = new string[stringCount];
                        nameSources = new string[memberNames.Length];
                    }

                    // Gather current fields.
                    var index = obsoleteFieldCount;
                    foreach (var field in HashToField.Values)
                    {
                        memberNames[index] = field.Name;
                        nameSources[index] = field.NameSource;
                        index++;
                    }

                    // Build all the gathered members into a switch.
                    text.Indent(indent).AppendLineConst("switch (hash)");
                    text.OpenScope(ref indent);

                    if (obsoleteFieldCount-- > 0)
                    {
                        text.AppendLineConst("#if " + WeaverUtilities.UnityEditor);
                        text.AppendLineConst("#pragma warning disable CS0618 // Type or member is obsolete.");
                    }

                    for (int i = 0; i < stringCount; i++)
                    {
                        text.Indent(indent)
                            .Append("case ")
                            .Append(memberNames[i])
                            .Append(": return ");
                        CSharpProcedural.AppendStringLiteral(text, nameSources[i]);
                        text.AppendLineConst(';');

                        if (i == obsoleteFieldCount)
                        {
                            text.AppendLineConst("#pragma warning restore CS0618 // Type or member is obsolete.");
                            text.AppendLineConst("#endif");
                        }
                    }

                    text.Indent(indent).AppendLineConst("default: return null;");

                    text.CloseScope(ref indent);
                });

            method.RegionIndex = HashConstantsRegion;

            method.CommentBuilder = (text) =>
            {
                text.Append("Get the state or parameter name associated with the specified 'hash' value.");
            };
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls the HashToString method in the currently compiled Animations class.
        /// </summary>
        public string GetCompiledHashToString(int hash)
        {
            GetCompiledHashToStringMethod();
            return _CompiledHashToStringMethod(hash);
        }

        /************************************************************************************************************************/

        private void GetCompiledHashToStringMethod()
        {
            if (_CompiledHashToStringMethod != null)
                return;

            for (int i = 0; i < ExistingType.Members.Count; i++)
            {
                var member = ExistingType.Members[i];
                if (member.Name == HashToStringMethodName)
                {
                    if (member is MethodInfo method)
                    {
                        _CompiledHashToStringMethod = method.GetDelegate<Func<int, string>>();
                        return;
                    }
                    else break;
                }
            }

            _CompiledHashToStringMethod = (hash) => "null";
        }

        /************************************************************************************************************************/

        private const string
            StateGetNameMethodName = "GetName";
        private static readonly ParameterBuilder[]
            StateGetNameParameters = { new ParameterBuilder(Scripting.ParameterModifier.This, typeof(AnimatorStateInfo), "state") };

        private void AddStateGetNameMethod()
        {
            var method = RootType.AddMethod(StateGetNameMethodName, typeof(string), StateGetNameParameters, (text, indent) =>
            {
                text.AppendLineConst(HashToStringMethodName + "(state.shortNameHash);");
            });

            method.RegionIndex = HashConstantsRegion;

            method.CommentBuilder = (text) =>
            {
                text.Append($"Get the name of the specified 'state' using {HashToStringMethodName}.");
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region States
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Parameters
        /************************************************************************************************************************/

        private void GatherParameters(AnimatorController controller)
        {
            foreach (var parameter in controller.parameters)
            {
                // If the hash is already occupied by a different parameter type, warn that it will only use the first type.
                // Supporting name sharing between multiple parameter types would mean the methods can't be called GetParameterName
                // because there would be multiple methods which only differ by return type.
                if (HashToParameter.TryGetValue(parameter.nameHash, out var existingParameter))
                {
                    if (existingParameter != parameter.type)
                    {
                        Debug.LogWarning($"Parameter type mismamtch: '<I>{parameter.name}</I>'" +
                            $" is used for parameters that are both '<I>{existingParameter}</I>'" +
                            $" and '<I>{parameter.type}</I>'. This is not supported by the Weaver Animation Linker.");
                    }

                    continue;
                }
                else HashToParameter.Add(parameter.nameHash, parameter.type);

                var field = AddHashConstant(parameter.name, parameter.nameHash);

                if (field == null || !WeaverSettings.Animations.createParameterWrappers)
                    return;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        AddGetSetMethods(field, typeof(bool), BoolSetterParameters, "Bool");
                        break;
                    case AnimatorControllerParameterType.Float:
                        AddGetSetMethods(field, typeof(float), FloatSetterParameters, "Float");
                        break;
                    case AnimatorControllerParameterType.Int:
                        AddGetSetMethods(field, typeof(int), IntSetterParameters, "Integer");
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        AddTriggerMethods(field);
                        break;
                    default:
                        throw new Exception("Unexpected Case");
                }
            }
        }

        /************************************************************************************************************************/

        private static readonly ParameterBuilder[]
            AnimatorParameter = { new ParameterBuilder(Scripting.ParameterModifier.This, typeof(Animator), "animator") },
            BoolSetterParameters = { AnimatorParameter[0], new ParameterBuilder(typeof(bool), "value") },
            FloatSetterParameters = { AnimatorParameter[0], new ParameterBuilder(typeof(float), "value") },
            IntSetterParameters = { AnimatorParameter[0], new ParameterBuilder(typeof(int), "value") };

        /************************************************************************************************************************/

        private void AddGetMethod(MemberBuilder parameter, Type type, string methodTypeName)
        {
            var method = RootType.AddMethod("Get " + parameter.NameSource, type, AnimatorParameter, (text, indent) =>
            {
                text.Append("animator.Get").Append(methodTypeName).Append("(").Append(parameter.Name).Append(");");
            });
            method.RegionIndex = ParameterWrappersRegion;
            method.CommentBuilder = (text) =>
            {
                text.Append("Gets the value of the '").Append(parameter.NameSource).Append("' parameter on the specified 'animator'.");
            };
        }

        private void AddGetSetMethods(MemberBuilder parameter, Type type, ParameterBuilder[] setterParameters, string methodTypeName)
        {
            AddGetMethod(parameter, type, methodTypeName);

            var method = RootType.AddMethod("Set " + parameter.NameSource, typeof(void), setterParameters, (text, indent) =>
            {
                text.Append("animator.Set").Append(methodTypeName).Append("(").Append(parameter.Name).Append(", value);");
            });
            method.RegionIndex = ParameterWrappersRegion;
            method.CommentBuilder = (text) =>
            {
                text.Append("Sets the value of the '").Append(parameter.NameSource).Append("' parameter on the specified 'animator'.");
            };
        }

        /************************************************************************************************************************/

        private void AddTriggerMethods(MemberBuilder parameter)
        {
            var method = RootType.AddMethod("Set " + parameter.NameSource, typeof(void), AnimatorParameter, (text, indent) =>
            {
                text.Append("animator.SetTrigger(").Append(parameter.Name).Append(");");
            });
            method.RegionIndex = ParameterWrappersRegion;
            method.CommentBuilder = (text) =>
            {
                text.Append("Activate the \"").Append(parameter.NameSource).Append("\" trigger parameter on the specified 'animator'.");
            };

            method = RootType.AddMethod("Reset " + parameter.NameSource, typeof(void), AnimatorParameter, (text, indent) =>
            {
                text.Append("animator.ResetTrigger(").Append(parameter.Name).Append(");");
            });
            method.RegionIndex = ParameterWrappersRegion;
            method.CommentBuilder = (text) =>
            {
                text.Append("Deactivate the \"").Append(parameter.NameSource).Append("\" trigger parameter on the specified 'animator'.");
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Build Script
        /************************************************************************************************************************/

        /// <summary>Appends the declaration of the elements of this script in C# code to the specified `text`.</summary>
        protected override void AppendScript(StringBuilder text)
        {
            if (WeaverSettings.Animations.notifyOnNewValue)
            {
                for (int i = 0; i < RootType.Elements.Count; i++)
                {
                    if (RootType.Elements[i] is FieldBuilder field && field.ExistingMember == null)
                    {
                        var type = HashToParameter.ContainsKey((int)field.Value) ? "Parameter" : "State";
                        Debug.Log($"New Animation {type} found: '<I>{field.NameSource}</I>' (Hash {field.Value})");
                    }
                }
            }

            RootType.SortElements(CompareMembers);

            base.AppendScript(text);
        }

        /************************************************************************************************************************/

        private static readonly Comparison<ElementBuilder> CompareMembers = (a, b) =>
        {
            int result;

            result = a.RegionIndex.CompareTo(b.RegionIndex);
            if (result != 0)
                return result;

            result = a.MemberType.CompareTo(b.MemberType);
            if (result != 0)
                return result;

            return a.NameSource.CompareTo(b.NameSource);
        };

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

