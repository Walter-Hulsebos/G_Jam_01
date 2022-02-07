// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="PathMatcher"/> specifically for Unity Assets.
    /// </summary>
    internal sealed class AssetPathMatcher : PathMatcher
    {
        /************************************************************************************************************************/
        #region Initialisation
        /************************************************************************************************************************/

        private static readonly Dictionary<InjectionAttribute, bool>
            AttributeHasUniqueName = new Dictionary<InjectionAttribute, bool>();

        private static readonly HashSet<InjectionAttribute>
            AttributesWithSiblings = new HashSet<InjectionAttribute>();

        /************************************************************************************************************************/

        static AssetPathMatcher()
        {
            var nameToAttribute = new Dictionary<string, InjectionAttribute>();
            InjectionAttribute previous = null;

            for (int i = 0; i < InjectorManager.AllInjectionAttributes.Count; i++)
            {
                EvaluateGrouping(nameToAttribute, InjectorManager.AllInjectionAttributes[i], ref previous);
            }
        }

        /************************************************************************************************************************/

        private static void EvaluateGrouping(Dictionary<string, InjectionAttribute> nameToAttribute, InjectionAttribute current, ref InjectionAttribute previous)
        {
            // Check for unique names.
            if (nameToAttribute.TryGetValue(current.Member.Name, out var existingWithName))
            {
                AttributeHasUniqueName[existingWithName] = false;
            }
            else
            {
                nameToAttribute.Add(current.Member.Name, current);
                AttributeHasUniqueName[current] = true;
            }

            // Check for siblings.
            if (previous != null && previous.Member.DeclaringType == current.Member.DeclaringType)
            {
                AttributesWithSiblings.Add(previous);
                AttributesWithSiblings.Add(current);
            }

            previous = current;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Static Access
        /************************************************************************************************************************/

        private static readonly Dictionary<AssetInjectionAttribute, AssetPathMatcher>
            AttributeToMatcher = new Dictionary<AssetInjectionAttribute, AssetPathMatcher>();

        /************************************************************************************************************************/

        public static AssetPathMatcher Get(AssetInjectionAttribute attribute)
        {
            if (!AttributeToMatcher.TryGetValue(attribute, out var matcher))
            {
                var matchTargets = new List<string>(5);

                if (attribute.FileName != null)
                    matchTargets.Add(attribute.FileName);

                matchTargets.Add(attribute.Member.GetNameCS());

                var declaringTypeWithoutNamespace =
                    attribute.Member.DeclaringType.Namespace != null ?
                    WeaverEditorUtilities.GetFullNameWithoutNamespace(attribute.Member.DeclaringType) :
                    null;

                if (declaringTypeWithoutNamespace != null)
                    matchTargets.Add(declaringTypeWithoutNamespace + "." + attribute.Member.Name);

                if (AttributeHasUniqueName.TryGetValue(attribute, out var hasUniqueName) && hasUniqueName)
                    matchTargets.Add(attribute.Member.Name);

                if (!AttributesWithSiblings.Contains(attribute))
                {
                    matchTargets.Add(attribute.Member.DeclaringType.GetNameCS());

                    if (declaringTypeWithoutNamespace != null)
                        matchTargets.Add(declaringTypeWithoutNamespace);
                }

                matcher = new AssetPathMatcher(matchTargets, attribute);
                AttributeToMatcher.Add(attribute, matcher);
            }

            matcher.Reset();

            return matcher;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Instance Members
        /************************************************************************************************************************/

        public readonly AssetInjectionAttribute Attribute;

        /************************************************************************************************************************/

        private AssetPathMatcher(IList<string> targetPaths, AssetInjectionAttribute attribute)
            : base(targetPaths)
        {
            Attribute = attribute;
        }

        /************************************************************************************************************************/

        protected override bool FinalValidation(string path)
        {
            // Once a valid path is found we need to double check that the asset there is actually the correct type.
            // This is mainly for component types which check all prefab paths without knowing which ones actually have the right component on them.
            // But it's also due to the fact that AssetDatabase.FindAssets uses the type name only, so it can find types with the same name in other namespaces.

            var asset = AssetDatabase.LoadAssetAtPath(path, Attribute.AssetType);
            if (asset == null)
                return false;

            // Then we also need to make sure the attribute will actually allow it as a valid asset.
            return Attribute.TrySetAsset(asset);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

