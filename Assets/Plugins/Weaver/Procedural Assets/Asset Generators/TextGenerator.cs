// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

//#define LOG_INSTEAD_OF_SAVING

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only]
    /// An <see cref="AssetGenerator"/> which saves <see cref="TextAsset"/>s as ".txt" text files.
    /// </summary>
    [AssetGenerator(typeof(TextAsset))]
    public class TextGenerator : AssetGenerator
    {
        /************************************************************************************************************************/

        /// <summary>.txt</summary>
        public override string DefaultFileExtension => ".txt";

        /************************************************************************************************************************/

        /// <summary>The default return type for generator methods used by this generator.</summary>
        protected override Type GeneratorMethodReturnType => typeof(void);

        /// <summary>
        /// The parameter types of a generator method for this asset type. When overriding this property, consider
        /// using <see cref="ReflectionUtilities.OneType(Type)"/> or <see cref="ReflectionUtilities.TwoTypes(Type, Type)"/>.
        /// </summary>
        protected override Type[] GeneratorMethodParameterTypes
            => ReflectionUtilities.OneType(typeof(StringBuilder));

        /************************************************************************************************************************/

        /// <summary>Invokes `asset.GeneratorMethod` with the correct parameters for this <see cref="AssetGenerator"/>.</summary>
        public override Object InvokeGeneratorMethod(ProceduralAsset asset)
        {
            var text = WeaverUtilities.GetStringBuilder();

            AppendHeader(text, asset);

            asset.GeneratorMethod.Invoke(null, ReflectionUtilities.OneObject(text));

            if (text.Length > 0)
            {
#if LOG_INSTEAD_OF_SAVING
                Debug.Log(text.ReleaseToString());
#else
                var path = ProceduralAsset.CurrentAssetPath;

                SaveAndRelease(text, path);

                if (asset.Injector.Asset == null)
                {
                    AssetDatabase.ImportAsset(path);
                    var newlySavedAsset = AssetDatabase.LoadAssetAtPath(path, asset.Injector.AssetType);
                    asset.Injector.TrySetAsset(newlySavedAsset);
                }
#endif

                OnSaveText(asset);
            }
            else
            {
                text.Release();
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Writes the contents of the `text` to the specified file `path` and releases the <see cref="StringBuilder"/> for later reuse.
        /// </summary>
        public static void SaveAndRelease(StringBuilder text, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, text.ReleaseToString());
            AssetGeneratorWindow.OnComplete += (success) => AssetDatabase.ImportAsset(path);
        }

        /************************************************************************************************************************/

        /// <summary>Override to append any default text at the top of every file.</summary>
        public virtual void AppendHeader(StringBuilder script, ProceduralAsset asset) { }

        /// <summary>Called after the asset is saved.</summary>
        protected virtual void OnSaveText(ProceduralAsset asset) { }

        /************************************************************************************************************************/

        /// <summary>Does nothing because <see cref="TextAsset"/>s are saved inside <see cref="InvokeGeneratorMethod"/>.</summary>
        public override void Save(ref Object obj, string assetPath, out bool hasSubAssets)
        {
            hasSubAssets = false;
        }

        /************************************************************************************************************************/

        /// <summary>Explains how to cancel the generation of an asset.</summary>
        public override string HowToCancel
        {
            get { return "set the StringBuilder.Length = 0"; }
        }

        /************************************************************************************************************************/
    }
}

#endif

