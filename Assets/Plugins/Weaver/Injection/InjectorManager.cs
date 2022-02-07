// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Manages the gathering of all dependency injectors and allows global access to them.
    /// </summary>
    internal static class InjectorManager
    {
        /************************************************************************************************************************/

        private static readonly List<IInjector> _AllInjectors = new List<IInjector>();

        /// <summary>
        /// All valid <see cref="IInjector"/>s that have been gathered from the currently loaded assemblies.
        /// </summary>
        public static List<IInjector> AllInjectors
        {
            get
            {
                AssertIsInitialized();
                return _AllInjectors;
            }
        }

        /************************************************************************************************************************/

        private static readonly List<InjectionAttribute> _AllInjectionAttributes = new List<InjectionAttribute>();

        /// <summary>
        /// All valid <see cref="InjectionAttribute"/>s that have been gathered from the currently loaded assemblies.
        /// </summary>
        public static List<InjectionAttribute> AllInjectionAttributes
        {
            get
            {
                AssertIsInitialized();
                return _AllInjectionAttributes;
            }
        }

        /************************************************************************************************************************/

        private static readonly List<OnInjectionCompleteAttribute> _AllEvents = new List<OnInjectionCompleteAttribute>();

        /// <summary>
        /// All valid <see cref="OnInjectionCompleteAttribute"/>s that have been gathered from the currently loaded assemblies.
        /// </summary>
        public static List<OnInjectionCompleteAttribute> AllEvents
        {
            get
            {
                AssertIsInitialized();
                return _AllEvents;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the initialisation process is complete.
        /// </summary>
        public static bool HasInitialized { get; private set; }

        /// <summary>
        /// Logs an error if the initialisation process it not yet complete.
        /// </summary>
        public static void AssertIsInitialized()
        {
            if (!HasInitialized)
            {
                Debug.LogError("The " + nameof(Weaver) + "." + nameof(InjectorManager) + " hasn't finished initialising." +
                    " Something is trying to access it too early.");

                HasInitialized = true;// Pretend it's done now so we don't keep logging errors.
            }
        }

        /************************************************************************************************************************/

        static InjectorManager()
        {
            ReflectionUtilities.Assemblies.ForEachTypeInDependantAssemblies(GatherAttributes);

            HasInitialized = true;

            WeaverUtilities.StableInsertionSort(_AllInjectionAttributes, (a, b) => CSharp.CompareNamespaceTypeMember(a.Member, b.Member));
            WeaverUtilities.StableInsertionSort(_AllEvents, OnInjectionCompleteAttribute.CompareExecutionTime);

            //_AllInjectionAttributes.DeepToString().LogTemp();
            //_AllEvents.DeepToString().LogTemp();

            for (int i = 0; i < _AllInjectionAttributes.Count; i++)
                _AllInjectors.Add(_AllInjectionAttributes[i]);

            for (int i = 0; i < _AllEvents.Count; i++)
                _AllInjectors.Add(_AllEvents[i]);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Iterates through all non-generic types in the `assembly` and gathers any properties with
        /// <see cref="InjectionAttribute"/>s.
        /// </summary>
        private static void GatherAttributes(Type type)
        {
            if (type.IsGenericType)
                return;

            var startCount = _AllInjectionAttributes.Count;

            var fields = WeaverUtilities.GetList<FieldInfo>();
            var properties = WeaverUtilities.GetList<PropertyInfo>();

            // Gather Injectors.
            ReflectionUtilities.GetAttributedFields(type, ReflectionUtilities.StaticBindings, _AllInjectionAttributes, fields);
            ReflectionUtilities.GetAttributedProperties(type, ReflectionUtilities.StaticBindings, _AllInjectionAttributes, properties);

            if (_AllInjectionAttributes.Count > startCount)
            {
                // Initialize and Verify Properties.
                var offset = startCount + fields.Count;
                for (int i = properties.Count - 1; i >= 0; i--)
                {
                    if (!_AllInjectionAttributes[offset + i].Validate(properties[i]))
                        _AllInjectionAttributes.RemoveAt(offset + i);
                }

                // Initialize and Verify Fields.
                for (int i = fields.Count - 1; i >= 0; i--)
                {
                    if (!_AllInjectionAttributes[startCount + i].Validate(fields[i]))
                        _AllInjectionAttributes.RemoveAt(startCount + i);
                }
            }

            fields.Release();
            properties.Release();

            // Gather Events.
            startCount = _AllEvents.Count;
            var methods = WeaverUtilities.GetList<MethodInfo>();
            type.GetAttributedMethods(ReflectionUtilities.StaticBindings, _AllEvents, methods);
            for (int i = methods.Count - 1; i >= 0; i--)
            {
                if (!_AllEvents[startCount + i].TryInitialize(methods[i]))
                    _AllEvents.RemoveAt(i);
            }
            methods.Release();
        }

        /************************************************************************************************************************/
    }
}

#endif

