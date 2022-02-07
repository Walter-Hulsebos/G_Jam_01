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
    /// An <see cref="AssetGenerator"/> which saves <see cref="Texture2D"/>s as ".png" image files.
    /// </summary>
    [AssetGenerator(typeof(Texture2D))]
    public class TextureGenerator : AssetGenerator
    {
        /************************************************************************************************************************/

        /// <summary>.png</summary>
        public override string DefaultFileExtension => ".png";

        /************************************************************************************************************************/

        /// <summary>The default return type for generator methods used by this generator.</summary>
        protected override Type GeneratorMethodReturnType => typeof(Texture2D);

        /************************************************************************************************************************/

        /// <summary>Saves `texture` as a PNG at `assetPath`.</summary>
        public static void Save(Texture2D texture, string assetPath)
        {
            // Unity can't save textures directly, so we need to encode and save the file manually.
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        }

        /// <summary>Saves `asset` as a texture at `assetPath`.</summary>
        public override void Save(ref Object obj, string assetPath, out bool hasSubAssets)
        {
            Save(obj as Texture2D, assetPath);

            AssetDatabase.ImportAsset(assetPath);
            obj = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            hasSubAssets = false;
        }

        /************************************************************************************************************************/
    }
}

#endif

