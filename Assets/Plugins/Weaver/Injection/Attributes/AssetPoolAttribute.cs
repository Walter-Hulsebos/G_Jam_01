// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Text;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Weaver.Editor;
using Weaver.Editor.Procedural;
using Weaver.Editor.Procedural.Scripting;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="AssetInjectionAttribute"/> which assigns an <see cref="ObjectPool{T}"/> to the attributed member
    /// that creates new items by instantiating copies of the target asset.
    /// </summary>
    public sealed class AssetPoolAttribute : AssetInjectionAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The injected pool immediately creates this many items.</summary>
        public int PreAllocate { get; set; }

        /// <summary>
        /// By default, the injected pool will automatically release all active items when a scene is loaded.
        /// Setting this to true disables that behaviour.
        /// </summary>
        public bool DontReleaseOnSceneLoad { get; set; }

        /// <summary>
        /// By default, the injected pool will be shared with any other pools using the target asset.
        /// Setting this to true disables that behaviour.
        /// </summary>
        public bool DontGetSharedPool { get; set; }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        private Type _AssetType;

        /// <summary>[Editor-Only]
        /// The type of asset which will be injected into the attributed <see cref="InjectionAttribute.Member"/>.
        /// </summary>
        public override Type AssetType => _AssetType;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// <para></para>
        /// Specifically, this method ensures that the <see cref="InjectionAttribute.MemberType"/> inherits from
        /// <see cref="ObjectPool{T}"/> and isn't abstract.
        /// </summary>
        protected override bool TryInitialize()
        {
            if (MemberType.IsAbstract)
                return LogThisInvalidAttribute("is marked as abstract.");

            if (!MemberType.IsGenericType || MemberType.GetGenericTypeDefinition() != typeof(ObjectPool<>))
                return LogThisInvalidAttribute("doesn't inherit from ObjectPool<T> where T inherits from Object.");

            var arguments = MemberType.GetGenericArguments();
            _AssetType = arguments[0];
            return TryInitialize(_AssetType);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns false. Pools could potentially be injected in Edit Mode, but the instances would need to be
        /// destroyed manually and their ownership would be unclear.
        /// </summary>
        public override bool InEditMode => false;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a value to be assigned to the attributed property.
        /// </summary>
        protected override object GetValueToInject()
        {
            if (Asset == null)
                return null;

            if (typeof(GameObject) == _AssetType)
            {
                var extensionType = GetExtensionType();
                var methodName = GetPrefabMethodName();

                var parameters = ThreeObjects(Asset, PreAllocate, !DontReleaseOnSceneLoad);

                return extensionType.InvokeMember(methodName, ReflectionUtilities.StaticBindings | BindingFlags.InvokeMethod, null, null, parameters);
            }
            else if (typeof(Component).IsAssignableFrom(_AssetType))
            {
                var extensionType = GetExtensionType();
                var methodName = GetComponentMethodName();

                var parameterTypes = ThreeTypes(AssetType, typeof(int), typeof(bool));
                var parameters = ThreeObjects(Asset, PreAllocate, !DontReleaseOnSceneLoad);

                var method = extensionType.GetMethod(methodName, ReflectionUtilities.StaticBindings);
                method = method.MakeGenericMethod(ReflectionUtilities.OneType(_AssetType));

                return method.Invoke(null, parameters);
            }
            else
            {
                var assetType = ReflectionUtilities.OneType(_AssetType);

                var method = typeof(AssetPoolAttribute).GetMethod(nameof(GetInstantiate), ReflectionUtilities.InstanceBindings);
                method = method.MakeGenericMethod(assetType);

                var func = method.Invoke(this, null);

                var arguments = ReflectionUtilities.TwoObjects(func, PreAllocate);

                var pool = Activator.CreateInstance(MemberType, arguments);
                //ReleaseOnSceneLoad(pool);
                return pool;
            }
        }

        /************************************************************************************************************************/

        private Type GetExtensionType()
        {
            // Get the extension type - a type with the same name as the pool type, but without generic arguments.
            // Doesn't currently support nested types.

            var type = MemberType;
            var name = type.Name;
            var backQuote = name.LastIndexOf('`');
            if (backQuote < 0)
                return type;

            name = name.Substring(0, backQuote);

            if (type.Namespace != null)
                name = type.Namespace + "." + name;

            return type.Assembly.GetType(name, true);
        }

        /************************************************************************************************************************/

        private string GetComponentMethodName() => DontGetSharedPool ? nameof(ObjectPool.CreateComponentPool) : nameof(ObjectPool.GetSharedComponentPool);

        private string GetPrefabMethodName() => DontGetSharedPool ? nameof(ObjectPool.CreatePrefabPool) : nameof(ObjectPool.GetSharedPrefabPool);

        /************************************************************************************************************************/

        private Func<T> GetInstantiate<T>() where T : Object => () => Object.Instantiate(Asset as T);

        /************************************************************************************************************************/

        private static object[] _ThreeObjects;

        private static object[] ThreeObjects(object obj0, object obj1, object obj2)
        {
            if (_ThreeObjects == null)
                _ThreeObjects = new object[3];

            _ThreeObjects[0] = obj0;
            _ThreeObjects[1] = obj1;
            _ThreeObjects[2] = obj2;
            return _ThreeObjects;
        }

        /************************************************************************************************************************/

        private static Type[] _ThreeTypes;

        private static Type[] ThreeTypes(Type type0, Type type1, Type type2)
        {
            if (_ThreeTypes == null)
                _ThreeTypes = new Type[3];

            _ThreeTypes[0] = type0;
            _ThreeTypes[1] = type1;
            _ThreeTypes[2] = type2;
            return _ThreeTypes;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="Texture"/> to use as an icon for this attribute.</summary>
        protected internal override Texture Icon => Icons.AssetPool;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Appends any optional properties that have been set on this attribute.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, ref bool isFirst)
        {
            base.AppendDetails(text, ref isFirst);

            if (PreAllocate != 0)
                AppendDetail(text, ref isFirst, nameof(PreAllocate) + "=" + PreAllocate);

            if (DontReleaseOnSceneLoad)
                AppendDetail(text, ref isFirst, nameof(DontReleaseOnSceneLoad));

            if (DontGetSharedPool)
                AppendDetail(text, ref isFirst, nameof(DontGetSharedPool));
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        protected internal override void GatherInjectorDetails(InjectorScriptBuilder builder)
        {
            if (Asset == null)
                return;

            builder.AddToMethod("Awake", this, (text, indent) =>
            {
                var assetFieldName = builder.AppendGetSerializedReference(text, indent, _AssetType, false, "original");

                text.Indent(indent)
                    .Append("var pool = ");

                var assetType = Asset.GetType();
                if (typeof(GameObject) == assetType)
                {
                    AppendPoolCreator(text, GetPrefabMethodName(), assetFieldName);
                }
                else if (typeof(Component).IsAssignableFrom(assetType))
                {
                    AppendPoolCreator(text, GetComponentMethodName(), assetFieldName);
                }
                else
                {
                    text.Append("new ")
                        .Append(MemberType.GetNameCS())
                        .AppendLineConst("(() => Instantiate(")
                        .Append(assetFieldName)
                        .AppendLineConst("), ")
                        .Append(PreAllocate)
                        .AppendLineConst(");");
                }

                if (typeof(IPoolable).IsAssignableFrom(assetType))
                {
                    text.Indent(indent)
                        .Append("pool.OnRelease = (item) => ");

                    CSharpProcedural.AppendInterfaceMethodCall(text, assetType, typeof(IPoolable), nameof(IPoolable.OnRelease), "item");
                    text.AppendLineConst("();");
                }

                builder.AppendSetValue(text, indent, this, "pool");
            });
        }

        /************************************************************************************************************************/

        private void AppendPoolCreator(StringBuilder text, string methodName, string assetFieldName)
        {
            text.Append(GetExtensionType().GetNameCS())
                .Append('.')
                .Append(methodName)
                .Append('(')
                .Append(assetFieldName)
                .Append(", ")
                .Append(PreAllocate)
                .Append(", ")
                .Append((!DontReleaseOnSceneLoad).ToStringCS())
                .AppendLineConst(");");
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the values this attribute
        /// wants assigned it.
        /// </summary>
        protected internal override void SetupInjectorValues(InjectorScriptBuilder builder)
        {
            if (Asset == null)
                return;

            builder.AddObjectReference(Asset, this);
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

