// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A visual indicator in the project window for any prefab that could potentially be linked to an
    /// <see cref="AssetInjectionAttribute"/>.
    /// </summary>
    internal sealed class PotentialPrefabInjectionOverlay : PotentialAssetInjectionOverlay
    {
        /************************************************************************************************************************/

        private static readonly List<AssetInjectionAttribute>
            ComponentAttributesWithoutTargets = new List<AssetInjectionAttribute>();

        private static readonly Dictionary<GameObject, PotentialPrefabInjectionOverlay>
            PrefabToOverlay = new Dictionary<GameObject, PotentialPrefabInjectionOverlay>();

        private readonly bool HasAnyTargets;
        private readonly Component[] Components;

        /************************************************************************************************************************/

        private PotentialPrefabInjectionOverlay(GameObject gameObject, List<AssetInjectionAttribute> potentialTargets)
            : base(potentialTargets)
        {
            HasAnyTargets = potentialTargets.Count > 0;
            Components = gameObject.GetComponents<Component>();
        }

        /************************************************************************************************************************/

        public static void AddComponentAttributeWithoutTarget(AssetInjectionAttribute attribute)
        {
            if (typeof(Component).IsAssignableFrom(attribute.AssetType))
                ComponentAttributesWithoutTargets.Add(attribute);
        }

        /************************************************************************************************************************/

        public static void RemoveComponentAttributeWithoutTarget(AssetInjectionAttribute attribute)
        {
            ComponentAttributesWithoutTargets.Remove(attribute);
        }

        /************************************************************************************************************************/

        public static PotentialAssetInjectionOverlay TryGetOverlay(GameObject gameObject)
        {
            if (PrefabToOverlay.TryGetValue(gameObject, out var overlay))
            {
                // Verify that the overlay has all the same components as when the overlay was created.
                var components = gameObject.GetComponents<Component>();
                if (components.Length != overlay.Components.Length)
                    goto RecreateOverlay;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != overlay.Components[i])
                        goto RecreateOverlay;
                }

                // If it does, return it.
                goto Return;

                // Otherwise let it get garbage collected and try to create a new one.
                RecreateOverlay:
                PrefabToOverlay.Remove(gameObject);
            }

            var potentialTargets = GatherPotentialTargets(gameObject);
            overlay = new PotentialPrefabInjectionOverlay(gameObject, potentialTargets);

            PrefabToOverlay.Add(gameObject, overlay);

            Return:
            // If the overlay doesn't actually have any targets, we still need to use it to remember the components on
            // the object, but we don't actually want to return it to be drawn.
            return overlay.HasAnyTargets ? overlay : null;
        }

        /************************************************************************************************************************/

        private static List<AssetInjectionAttribute> GatherPotentialTargets(GameObject gameObject)
        {
            var targets = PotentialAssetInjectionOverlay.GatherPotentialTargets(gameObject);

            for (int i = 0; i < ComponentAttributesWithoutTargets.Count; i++)
            {
                var attribute = ComponentAttributesWithoutTargets[i];
                var component = gameObject.GetComponent(attribute.AssetType);
                if (component != null)
                    targets.Add(attribute);
            }

            return targets;
        }

        /************************************************************************************************************************/
    }
}

#endif

