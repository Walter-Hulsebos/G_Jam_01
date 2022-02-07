// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only] An <see cref="AssetGenerator"/> which saves asset lists.</summary>
    [AssetGenerator(typeof(AssetListBase))]
    public sealed class AssetListGenerator : AssetGenerator
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, AssetGenerator>
            AssetTypeToElementGenerator = new Dictionary<Type, AssetGenerator>();

        private static AssetGenerator GetElementGenerator(Type assetType)
        {
            if (!AssetTypeToElementGenerator.TryGetValue(assetType, out var generator))
            {
                var elementType = assetType;
                while (!elementType.IsGenericType)
                {
                    elementType = elementType.BaseType;
                    if (elementType == null)
                    {
                        AssetTypeToElementGenerator.Add(assetType, null);
                        return null;
                    }
                }

                var arguments = elementType.GetGenericArguments();
                elementType = arguments[arguments.Length - 1];

                generator = AssetGenerators.GetAssetGenerator(elementType);
                AssetTypeToElementGenerator.Add(assetType, generator);
            }

            return generator;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether a temporary scene should be opened while generating the specified `asset`.
        /// Uses the return value of the generator used for the individual elements of the list.
        /// </summary>
        public override bool UseTempScene(ProceduralAsset asset)
        {
            var obj = asset.Injector.Asset;
            var assetType = obj != null ? obj.GetType() : asset.Injector.AssetType;

            var generator = GetElementGenerator(assetType);

            return
                generator == null ||
                generator.UseTempScene(asset);
        }

        /************************************************************************************************************************/

        /// <summary>The default return type for generator methods used by this generator.</summary>
        protected override Type GeneratorMethodReturnType => typeof(IEnumerable);

        /// <summary>Checks if return type of a generator method is valid for this generator type.</summary>
        protected override bool ValidateGeneratorReturnType(Type returnType)
        {
            return typeof(IEnumerable).IsAssignableFrom(returnType);
        }

        /************************************************************************************************************************/

        /// <summary>Invokes `asset.GeneratorMethod` with the correct parameters for this <see cref="AssetGenerator"/>.</summary>
        public override Object InvokeGeneratorMethod(ProceduralAsset proceduralAsset)
        {
            var outputDirectory = GetOutputDirectory(proceduralAsset, out var list);
            var outputDirectoryAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputDirectory);
            outputDirectory += "/";

            // Generate the assets.
            var assets = (IEnumerable)proceduralAsset.GeneratorMethod.Invoke(null, null);

            var savedListData = proceduralAsset.SavedListData;
            var oldAssetIndices = savedListData.OldAssetNameToIndex;

            // Save each asset.
            if (assets != null)
            {
                foreach (var obj in assets)
                {
                    if (obj == null)
                        continue;

                    var asset = obj as Object;

                    if (string.IsNullOrEmpty(asset.name))
                    {
                        Debug.LogWarning($"A Procedural Asset in '{proceduralAsset.Injector}'" +
                            " hasn't been given a name so it won't be saved");
                        continue;
                    }

                    var elementGenerator = AssetGenerators.GetAssetGenerator(asset.GetType());
                    string assetPath;

                    if (oldAssetIndices.TryGetValue(asset.name, out var index))
                    {
                        assetPath = AssetDatabase.GetAssetPath(savedListData.GetAndClearOldAsset(index));

                        if (savedListData.GetOldHasSubAssets(index))
                            WeaverEditorUtilities.DestroySubAssets(assetPath);

                        oldAssetIndices.Remove(asset.name);
                    }
                    else
                    {
                        assetPath = outputDirectory + asset.name + elementGenerator.DefaultFileExtension;
                    }

                    elementGenerator.Save(ref asset, assetPath, out var hasSubAssets);

                    savedListData.AddNewAsset(asset, hasSubAssets);
                }
            }

            // Destroy any old assets that havent been regenerated.
            savedListData.DeleteOldAssets();

            if (list != null)
            {
                list.Directory = outputDirectoryAsset;
                return null;
            }
            else if (!proceduralAsset.Injector.MemberType.IsAbstract)
            {
                list = (AssetListBase)ScriptableObject.CreateInstance(proceduralAsset.Injector.MemberType);
                list.Directory = outputDirectoryAsset;
                return list;
            }
            else
            {
                return null;
            }
        }

        /************************************************************************************************************************/

        private string GetOutputDirectory(ProceduralAsset proceduralAsset, out AssetListBase list)
        {
            string directory;

            list = proceduralAsset.Injector.Asset as AssetListBase;
            if (list != null)
            {
                directory = list.GetAndVerifyDirectoryPath();
                if (directory != null)
                    return directory;
            }

            directory = ProceduralAsset.CurrentAssetPath;
            directory = Path.GetDirectoryName(directory);

            if (!WeaverEditorUtilities.IsResource(directory, out _))
            {
                directory = $"{directory}/Resources/{proceduralAsset.GetRealFileName()}";

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.Log($"Created directory '{directory}' for {proceduralAsset.Injector}",
                        proceduralAsset.Injector.Asset);

                    AssetDatabase.ImportAsset(directory);
                }
            }

            return directory;
        }

        /************************************************************************************************************************/

        /// <summary>Does nothing.</summary>
        public override void SaveDefaultAsset(string assetPath) { }

        /// <summary>Saves `obj` at the specified `assetPath`.</summary>
        public override void Save(ref Object obj, string assetPath, out bool hasSubAssets)
        {
            base.Save(ref obj, assetPath, out hasSubAssets);
            obj.hideFlags = HideFlags.None;
        }

        /************************************************************************************************************************/
    }
}

#endif

