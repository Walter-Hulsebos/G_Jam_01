// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A visual indicator in the project window for any asset that could potentially be linked to an
    /// <see cref="AssetInjectionAttribute"/>.
    /// <para></para>
    /// Prefabs use <see cref="PotentialPrefabInjectionOverlay"/> instead.
    /// </summary>
    internal class PotentialAssetInjectionOverlay : AssetInjectionOverlay
    {
        /************************************************************************************************************************/

        private static readonly List<AssetInjectionAttribute>
            AttributesWithoutTargets = new List<AssetInjectionAttribute>();

        private static readonly Dictionary<Type, PotentialAssetInjectionOverlay>
            TypeToOverlay = new Dictionary<Type, PotentialAssetInjectionOverlay>();

        /************************************************************************************************************************/

        protected PotentialAssetInjectionOverlay(List<AssetInjectionAttribute> potentialTargets)
            : base(potentialTargets)
        { }

        /************************************************************************************************************************/

        public static void AddAttributeWithoutTarget(AssetInjectionAttribute attribute)
        {
            if (attribute.ProceduralAsset != null)
                return;

            AttributesWithoutTargets.Add(attribute);
            PotentialPrefabInjectionOverlay.AddComponentAttributeWithoutTarget(attribute);
            ClearPotentialTargetsTooltip(attribute.AssetType);
        }

        /************************************************************************************************************************/

        public static void RemoveAttributeWithoutTarget(AssetInjectionAttribute attribute)
        {
            if (AttributesWithoutTargets.Remove(attribute))
            {
                PotentialPrefabInjectionOverlay.RemoveComponentAttributeWithoutTarget(attribute);
                ClearPotentialTargetsTooltip(attribute.AssetType);
            }
        }

        /************************************************************************************************************************/

        private static void ClearPotentialTargetsTooltip(Type assetType)
        {
            var clearTargets = WeaverUtilities.GetList<Type>();

            foreach (var type in TypeToOverlay.Keys)
            {
                if (assetType.IsAssignableFrom(type))
                    clearTargets.Add(type);
            }

            for (int i = 0; i < clearTargets.Count; i++)
            {
                TypeToOverlay.Remove(clearTargets[i]);
            }

            clearTargets.Release();
        }

        /************************************************************************************************************************/

        public new static bool TryDoGUI(string guid, Rect selectionRect)
        {
            if (AttributesWithoutTargets.Count == 0)
                return false;

            var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null)
                return false;

            var overlay = TryGetOverlay(asset);
            if (overlay != null)
                overlay.DoGUI(selectionRect);

            return true;
        }

        /************************************************************************************************************************/

        protected override Texture Icon => Icons.PotentialTarget;

        /************************************************************************************************************************/

        protected override void AppendTooltipPrefix(StringBuilder tooltip)
        {
            tooltip.Append("This asset could be used as the target for the following Weaver injection attributes:")
                .AppendLineConst()
                .AppendLineConst();
        }

        /************************************************************************************************************************/

        private static PotentialAssetInjectionOverlay TryGetOverlay(Object asset)
        {
            if (asset is GameObject gameObject)
                return PotentialPrefabInjectionOverlay.TryGetOverlay(gameObject);

            var assetType = asset.GetType();
            if (TypeToOverlay.TryGetValue(assetType, out var overlay))
                return overlay;

            var potentialTargets = GatherPotentialTargets(asset);
            if (potentialTargets.Count > 0)
                overlay = new PotentialAssetInjectionOverlay(potentialTargets);

            TypeToOverlay.Add(assetType, overlay);
            return overlay;
        }

        /************************************************************************************************************************/

        protected static List<AssetInjectionAttribute> GatherPotentialTargets(Object asset)
        {
            var assetType = asset.GetType();

            var targets = WeaverUtilities.GetList<AssetInjectionAttribute>();

            for (int i = 0; i < AttributesWithoutTargets.Count; i++)
            {
                var attribute = AttributesWithoutTargets[i];
                if (attribute.AssetType.IsAssignableFrom(assetType))
                    targets.Add(attribute);
            }

            return targets;
        }

        /************************************************************************************************************************/
    }
}

#endif

