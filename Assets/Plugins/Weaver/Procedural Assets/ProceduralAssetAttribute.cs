// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;

namespace Weaver
{
    /// <summary>
    /// When placed alongside any kind of <see cref="AssetInjectionAttribute"/>, this attribute allows the injected
    /// asset to be procedurally generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ProceduralAssetAttribute : Attribute
    {
        /************************************************************************************************************************/

        /// <summary>
        /// The name of the static method which is used to generate this procedural asset.
        /// </summary>
        public string Generator { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The name of a static <see cref="bool"/> property or method which determines whether to show the asset in
        /// the <see cref="Editor.Window.WeaverWindow"/>.
        /// </summary>
        public string CheckShouldShow { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The name of a static <see cref="bool"/> property or method which determines whether to generate the asset.
        /// </summary>
        public string CheckShouldGenerate { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The file extension of the asset file.
        /// Should begin with a period.
        /// If not specified, the <see cref="Editor.Procedural.AssetGenerator.DefaultFileExtension"/> will be used.
        /// </summary>
        public string FileExtension { get; set; }

        /************************************************************************************************************************/

        internal OptionalBool OptionalAutoGenerateOnBuild { get; private set; }

        /// <summary>
        /// Determines when the target asset should be automatically generated when compiling a build.
        /// If not set, the global setting will be used.
        /// </summary>
        public bool AutoGenerateOnBuild
        {
            get { return OptionalAutoGenerateOnBuild.ToBool(); }
            set { OptionalAutoGenerateOnBuild = WeaverUtilities.ToOptionalBool(value); }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines when the target asset should be automatically generated whenever assets are saved.
        /// Default false.
        /// </summary>
        public bool AutoGenerateOnSave { get; set; }

        /************************************************************************************************************************/

        internal OptionalBool OptionalUseTempScene { get; private set; }

        /// <summary>
        /// Indicates whether a temporary scene should be opened during the asset generation process.
        /// </summary>
        public bool UseTempScene
        {
            get { return OptionalUseTempScene.ToBool(); }
            set { OptionalUseTempScene = WeaverUtilities.ToOptionalBool(value); }
        }

        /************************************************************************************************************************/
    }
}

