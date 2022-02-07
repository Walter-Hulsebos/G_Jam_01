// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Weaver
{
    /// <summary>An object that can specify its own meta-data.</summary>
    public interface IMetaDataProvider<T>
    {
        /************************************************************************************************************************/

        /// <summary>The meta-data of this object.</summary>
        T MetaData { get; }

        /************************************************************************************************************************/
    }
}

/************************************************************************************************************************/
#if UNITY_EDITOR
/************************************************************************************************************************/

namespace Weaver.Editor
{
    /// <summary>[Editor-Only] Various utilities for managing meta-data.</summary>
    public static class MetaDataUtils
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Gets the meta-data of the specified `asset`, either by passing it as a parameter into the specified
        /// `metaConstructor` or by accessing its <see cref="IMetaDataProvider{T}.MetaData"/> (if it can be cast).
        /// </summary>
        public static TMeta GetMetaData<TAsset, TMeta>(TAsset asset, ConstructorInfo metaConstructor)
        {
            try
            {
                if (metaConstructor != null)
                {
                    return (TMeta)metaConstructor.Invoke(ReflectionUtilities.OneObject(asset));
                }

                if (asset is IMetaDataProvider<TMeta> metaDataProvider)
                {
                    return metaDataProvider.MetaData;
                }

                Debug.LogWarning("Unable to get meta-data for '" + asset + "': " +
                    typeof(TMeta).GetNameCS() + " doesn't have a ctor(" + typeof(TAsset).GetNameCS() + ") and " +
                    asset.GetType().GetNameCS() + " doesn't implement " + typeof(IMetaDataProvider<TMeta>).GetNameCS(),
                    asset as Object);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a constructor of the specified `type` which takes a single `parameterType` parameter (or null).
        /// </summary>
        public static ConstructorInfo GetSingleParameterConstructor(Type type, Type parameterType)
        {
            return type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic, null,
                ReflectionUtilities.OneType(parameterType), null);
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, List<Type>> _TypeToMetaDataTypes;

        /// <summary>
        /// Returns a list of all potential meta-data types of the specified `type`.
        /// </summary>
        public static List<Type> GetMetaDataTypes(Type type)
        {
            List<Type> metaDataTypes;

            // Check cached results.
            if (_TypeToMetaDataTypes == null)
            {
                _TypeToMetaDataTypes = new Dictionary<Type, List<Type>>();
            }
            else if (_TypeToMetaDataTypes.TryGetValue(type, out metaDataTypes))
            {
                return metaDataTypes;
            }

            metaDataTypes = WeaverUtilities.GetList<Type>();
            Type primaryMetaDataType = null;

            // Check if the type implements IMetaDataProvider<T>.
            var arguments = ReflectionUtilities.GetGenericInterfaceArguments(type, typeof(IMetaDataProvider<>));
            if (arguments != null)
            {
                primaryMetaDataType = arguments[0];
                metaDataTypes.Add(primaryMetaDataType);
            }

            // Gather any types with a constructor that can take the asset type.
            ReflectionUtilities.Assemblies.ForEachTypeInDependantAssemblies(type.Assembly, (type2) =>
            {
                if (type2.IsAbstract ||
                    type2.IsGenericType ||
                    type2.IsEnum ||
                    type2 == primaryMetaDataType)
                    return;

                var constructors = type2.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < constructors.Length; i++)
                {
                    var parameters = constructors[i].GetParameters();
                    if (parameters.Length == 1 &&
                        parameters[0].ParameterType.IsAssignableFrom(type))
                    {
                        metaDataTypes.Add(type2);
                        break;
                    }
                }
            });

            if (metaDataTypes.Count == 0)
                WeaverUtilities.Release(ref metaDataTypes);

            _TypeToMetaDataTypes.Add(type, metaDataTypes);
            return metaDataTypes;
        }

        /************************************************************************************************************************/
    }
}

/************************************************************************************************************************/
#endif
/************************************************************************************************************************/

