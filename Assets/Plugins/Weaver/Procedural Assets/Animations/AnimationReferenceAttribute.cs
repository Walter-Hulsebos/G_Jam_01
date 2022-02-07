// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Weaver.Editor.Procedural;
#endif

namespace Weaver
{
    /************************************************************************************************************************/

    /// <summary>
    /// Marks a <see cref="string"/> or <see cref="int"/> field to show a popup menu in the inspector which lets you
    /// select an animation value name or hash respectively. Values include both states and parameters.
    /// <para></para>
    /// WARNING: selecting a value using this attribute does not link the field to that state or parameter. Renaming
    /// the state or parameter will NOT automatically update the value of the attributed field.
    /// <para></para>
    /// If you're interested in an animation system which avoids the need for these weak unsafe references entirely,
    /// you should check out <a href="https://kybernetik.com.au/animancer">Animancer</a>.
    /// </summary>
    public sealed class AnimationReferenceAttribute : PropertyAttribute { }

    /************************************************************************************************************************/
}

/************************************************************************************************************************/
#if UNITY_EDITOR
/************************************************************************************************************************/

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="PropertyDrawer"/> used for fields marked with an <see cref="AnimationReferenceAttribute"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationReferenceAttribute))]
    internal sealed class AnimationReferenceDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        private static readonly int[] Hashes;
        private static readonly string[] Names;

        /************************************************************************************************************************/

        static AnimationReferenceDrawer()
        {
            var animationsType = AnimationsScriptBuilder.Instance.ExistingType.Type;
            var fields = animationsType.GetFields(BindingFlags.Public | BindingFlags.Static);

            Hashes = new int[fields.Length];
            Names = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(int))
                    continue;

                var hash = (int)field.GetValue(null);
                Hashes[i] = hash;

                Names[i] = AnimationsScriptBuilder.Instance.GetCompiledHashToString(hash);
            }
        }

        /************************************************************************************************************************/

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var showMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            label = EditorGUI.BeginProperty(position, label, property);
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.IntPopup(position, label.text, property.intValue, Names, Hashes);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.intValue = newValue;
                    }
                    break;

                case SerializedPropertyType.String:
                    EditorGUI.BeginChangeCheck();
                    var selectedIndex = Array.IndexOf(Names, property.stringValue);
                    selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, Names);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.stringValue = Names[selectedIndex];
                    }
                    break;

                default:
                    EditorGUI.PropertyField(position, property, property.isExpanded);
                    break;
            }
            EditorGUI.EndProperty();

            EditorGUI.showMixedValue = showMixedValue;
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /************************************************************************************************************************/
    }
}

/************************************************************************************************************************/
#endif
/************************************************************************************************************************/

