// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using Weaver.Editor;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="AssetInjectionAttribute"/> which assigns the target asset directly to the attributed member.
    /// </summary>
    public sealed class AssetReferenceAttribute : AssetInjectionAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the attributed member type is <see cref="Asset{T}"/>.
        /// </summary>
        public bool IsWeaverAsset => _WeaverAssetConstructor != null;
        private ConstructorInfo _WeaverAssetConstructor;

        /// <summary>[Editor-Only]
        /// The type of asset which will be injected into the attributed <see cref="InjectionAttribute.Member"/>.
        /// </summary>
        public override Type AssetType => _AssetType;
        private Type _AssetType;

        /// <summary>[Editor-Only]
        /// Returns true. Asset references can safely be assigned in Edit Mode.
        /// </summary>
        public override bool InEditMode => true;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// </summary>
        protected override bool TryInitialize()
        {
            // Weaver.Asset.
            if (MemberType.IsGenericType && MemberType.GetGenericTypeDefinition() == typeof(Asset<>))
            {
                var arguments = MemberType.GetGenericArguments();
                _AssetType = arguments[0];
                _WeaverAssetConstructor = MemberType.GetConstructor(ReflectionUtilities.OneType(typeof(string)));
            }
            else// Direct Asset Reference.
            {
                _AssetType = MemberType;
            }

            return base.TryInitialize(_AssetType);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the specified `asset` can be injected into the attributed
        /// <see cref="InjectionAttribute.Member"/> by this attribute.
        /// </summary>
        public override bool ValidateAsset(Object asset)
        {
            if (!base.ValidateAsset(asset))
                return false;

            if (_WeaverAssetConstructor == null ||
                EditorOnly)
                return true;

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (WeaverEditorUtilities.IsResource(assetPath, out _))
                return true;

            Debug.LogWarning($"Invalid Target Asset: '{assetPath}'" +
                $" is not inside a 'Resources' folder so it cannot be assigned to {ToString()}");
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a value to be assigned to the attributed property.</summary>
        protected override object GetValueToInject()
        {
            if (Asset == null)
                return null;

            if (IsWeaverAsset)// Weaver.Asset.
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(Asset);
                return _WeaverAssetConstructor.Invoke(ReflectionUtilities.OneObject(assetPath));
            }
            else// Direct Asset Reference.
            {
                return Asset;
            }
        }

        /************************************************************************************************************************/

        private Texture _Icon;

        /// <summary>[Editor-Only] The <see cref="Texture"/> to use as an icon for this attribute.</summary>
        protected internal override Texture Icon
        {
            get
            {
                if (_Icon == null)
                {
                    var iconProperty = MemberType.GetProperty("Icon", ReflectionUtilities.StaticBindings);
                    if (iconProperty != null && typeof(Texture).IsAssignableFrom(iconProperty.PropertyType))
                    {
                        var getter = iconProperty.GetGetMethod(true);
                        if (getter != null)
                        {
                            _Icon = (Texture)getter.Invoke(null, null);
                            if (_Icon != null)
                                goto Return;
                        }
                    }

                    _Icon = Icons.AssetReference;
                }

                Return:
                return _Icon;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        protected internal override void GatherInjectorDetails(Editor.Procedural.InjectorScriptBuilder builder)
        {
            if (IsWeaverAsset)
            {
                if (Asset == null)
                    return;

                builder.AddToMethod("Awake", this, (text, indent) =>
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(Asset);
                    if (WeaverEditorUtilities.IsResource(assetPath, out var resourcePathStart))
                    {
                        assetPath = WeaverEditorUtilities.AssetToResourcePath(assetPath, resourcePathStart);

                        text.Indent(indent)
                            .Append("var asset = new ")
                            .Append(MemberType.GetNameCS())
                            .Append("(\"")
                            .Append(assetPath)
                            .AppendLineConst("\");");

                        builder.AppendSetValue(text, indent, this, "asset");
                    }
                    else
                    {
                        Debug.LogError($"Target Asset is not inside a Resources folder: {ToString()} {assetPath}", Asset);
                    }
                });
            }
            else
            {
                base.GatherInjectorDetails(builder);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the values this attribute
        /// wants assigned it.
        /// </summary>
        protected internal override void SetupInjectorValues(Editor.Procedural.InjectorScriptBuilder builder)
        {
            if (IsWeaverAsset)
            {
                // No direct reference to the asset. The Resource path is hard coded into the script.
            }
            else
            {
                base.SetupInjectorValues(builder);
            }
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

