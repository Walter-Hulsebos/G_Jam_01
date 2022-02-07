// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only]
    /// An <see cref="AssetGenerator"/> which saves <see cref="GameObject"/>s and <see cref="Component"/>s as prefabs.
    /// </summary>
    [AssetGenerator(typeof(GameObject))]
    [AssetGenerator(typeof(Component))]
    public class PrefabGenerator : AssetGenerator
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether a temporary scene should be opened while generating the specified `asset`.
        /// Returns true because prefabs should always be generated in a temporary scene.
        /// </summary>
        public override bool UseTempScene(ProceduralAsset asset) => true;

        /************************************************************************************************************************/

        /// <summary>.prefab</summary>
        public override string DefaultFileExtension => ".prefab";

        /************************************************************************************************************************/

        /// <summary>The default return type for generator methods used by this generator.</summary>
        protected override Type GeneratorMethodReturnType => typeof(GameObject);

        /// <summary>Checks if return type of a generator method is valid for this generator type.</summary>
        protected override bool ValidateGeneratorReturnType(Type returnType)
        {
            return
                typeof(GameObject) == returnType ||
                typeof(Component).IsAssignableFrom(returnType);
        }

        /************************************************************************************************************************/

        /// <summary>Invokes `asset.GeneratorMethod` with the correct parameters for this <see cref="AssetGenerator"/>.</summary>
        public override Object InvokeGeneratorMethod(ProceduralAsset asset)
        {
            var obj = asset.GeneratorMethod.Invoke(null, null) as Object;
            obj.name = Path.GetFileNameWithoutExtension(ProceduralAsset.CurrentAssetPath);
            return obj;
        }

        /************************************************************************************************************************/

        /// <summary>Creates and saves an empty prefab to save sub assets inside while the asset is still generating.</summary>
        public override void SaveDefaultAsset(string assetPath)
        {
            var gameObject = new GameObject();
            PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath, out _);
            Object.DestroyImmediate(gameObject);
        }

        /************************************************************************************************************************/

        /// <summary>Saves `obj` as a prefab at `assetPath`.</summary>
        public override void Save(ref Object obj, string assetPath, out bool hasSubAssets)
        {
            var gameObject = WeaverEditorUtilities.GetGameObject(obj, out var component);
            if (gameObject == null)
                throw new ArgumentException($"'{nameof(obj)}' isn't a {nameof(GameObject)} or {nameof(Component)}");

            Object prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath, out var success);

            // Make the prefab not editable.
            WeaverEditorUtilities.SetHideFlagsRecursive(((GameObject)prefab).transform, HideFlags.NotEditable);
            EditorUtility.SetDirty(prefab);

            // Save any sub assets as necessary.
            hasSubAssets = WeaverEditorUtilities.SaveSubAssets(obj, prefab);

            if (component != null)
                prefab = ((GameObject)prefab).GetComponent(component.GetType());

            Destroy(gameObject);

            ProceduralAsset.InvokeOnImportAsset(assetPath);

            obj = prefab;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Destroys the specified `obj`.
        /// If it is a <see cref="Component"/>, this method destroys its <see cref="Component.gameObject"/> instead.
        /// </summary>
        public override void Destroy(Object obj)
        {
            if (obj is Component component)
                obj = component.gameObject;

            base.Destroy(obj);
        }

        /************************************************************************************************************************/
    }
}

#endif

