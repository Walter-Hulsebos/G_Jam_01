// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Gathers all <see cref="AssetGenerator"/> types and allows them to be accessed via the asset type they can each
    /// generate.
    /// </summary>
    internal static class AssetGenerators
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, Type>
            AssetToGeneratorType = new Dictionary<Type, Type>();

        private static readonly Dictionary<Type, AssetGenerator>
            TypeToGenerator = new Dictionary<Type, AssetGenerator>();

        /************************************************************************************************************************/

        static AssetGenerators()
        {
            ReflectionUtilities.Assemblies.ForEachTypeInDependantAssemblies((type) =>
            {
                if (type.IsAbstract ||
                    !type.IsDefined(typeof(AssetGeneratorAttribute), false))
                    return;

                if (!typeof(AssetGenerator).IsAssignableFrom(type))
                {
                    Debug.LogError($"Found {WeaverEditorUtilities.GetAttributeDisplayString(typeof(AssetGeneratorAttribute))}" +
                        $" on a type that doesn't inherit from {typeof(AssetGenerator).GetNameCS()}: {type.GetNameCS()}");
                    return;
                }

                var attributes = type.GetCustomAttributes(typeof(AssetGeneratorAttribute), false);
                for (int i = 0; i < attributes.Length; i++)
                {
                    AssetToGeneratorType.Add((attributes[i] as AssetGeneratorAttribute).AssetType, type);
                }
            });

            //AssetToGeneratorType.DeepToString().LogTemp();
        }

        /************************************************************************************************************************/

        internal static void AddGeneratorType(Type assetType, Type generatorType)
        {
            AssetToGeneratorType.Add(assetType, generatorType);
        }

        /************************************************************************************************************************/

        public static AssetGenerator GetAssetGenerator(Type assetType)
        {
            if (!TypeToGenerator.TryGetValue(assetType, out var generator))
            {
                var generatorType = GetGeneratorType(assetType);

                if (generatorType != null)
                {
                    if (!TypeToGenerator.TryGetValue(generatorType, out generator))
                    {
                        try
                        {
                            generator = (AssetGenerator)Activator.CreateInstance(generatorType);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }

                        TypeToGenerator.Add(generatorType, generator);
                    }
                }

                TypeToGenerator.Add(assetType, generator);
            }

            //Utils.LogTemp(assetType + " generator is " + generator?.ToString());
            return generator;
        }

        /************************************************************************************************************************/

        private static Type GetGeneratorType(Type assetType)
        {
            if (AssetToGeneratorType.TryGetValue(assetType, out var generatorType))
                return generatorType;

            if (assetType.BaseType != null)
                return GetGeneratorType(assetType.BaseType);

            return null;
        }

        /************************************************************************************************************************/

        private static bool _IsShowingDescriptions;
        private static string[] _Descriptions;
        private static GUIStyle _DescriptionStyle;

        public static void DoGUI()
        {
            _IsShowingDescriptions = EditorGUILayout.Foldout(_IsShowingDescriptions, "Asset Generators", true);
            if (!_IsShowingDescriptions)
                return;

            if (_Descriptions == null)
            {
                _Descriptions = new string[AssetToGeneratorType.Count];

                var i = 0;
                foreach (var assetType in AssetToGeneratorType.Keys)
                {
                    var generator = GetAssetGenerator(assetType);

                    var text = WeaverUtilities.GetStringBuilder();

                    text.Append("<B>")
                        .Append(assetType.GetNameCS())
                        .Append("</B> is generated by <B>")
                        .Append(generator.GetType().GetNameCS())
                        .Append("</B>");

                    generator.AppendFullDescription(text);

                    _Descriptions[i++] = text.ReleaseToString();
                }

                _DescriptionStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleLeft,
                    richText = true,
                    wordWrap = true,
                };
            }

            for (int i = 0; i < _Descriptions.Length; i++)
            {
                EditorGUILayout.LabelField(_Descriptions[i], _DescriptionStyle);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

