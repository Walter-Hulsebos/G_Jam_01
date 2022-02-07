// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Weaver.Editor;
using Weaver.Editor.Procedural;
#endif

namespace Weaver
{
    /// <summary>
    /// An <see cref="AssetInjectionAttribute"/> which instantiates a copy of the target asset and assigns the copy to
    /// the attributed member.
    /// </summary>
    public sealed class AssetInstanceAttribute : AssetInjectionAttribute
    {
        /************************************************************************************************************************/

        /// <summary>
        /// If set to true: when the instance is created it will not be assigned to the attributed member.
        /// This is useful if the instance doesn't need a static access point or if it will assign one itself.
        /// </summary>
        public bool DontAssign { get; set; }

        /// <summary>
        /// If set to true: the instance will be created with its <see cref="GameObject.activeSelf"/> set to false.
        /// Only works for <see cref="GameObject"/>s and <see cref="Component"/>s.
        /// </summary>
        public bool StartInactive { get; set; }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns false. Asset instances should not be created automatically in Edit Mode.
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

            Object instance;

            if (StartInactive && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var parent = new GameObject();
                parent.SetActive(false);

                instance = Object.Instantiate(Asset, parent.transform);

                var gameObject = WeaverEditorUtilities.GetGameObject(instance, out _);
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                    gameObject.transform.SetParent(null);
                }

                Object.DestroyImmediate(parent);
            }
            else
            {
                instance = Object.Instantiate(Asset);
            }

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                Object.DontDestroyOnLoad(instance);

            return instance;
        }

        /// <summary>[Editor-Only]
        /// Tries to set the value of the attributed member. Catches and logs any exceptions.
        /// </summary>
        public override void SetValue(object value)
        {
            if (DontAssign)
                return;

            base.SetValue(value);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The <see cref="Texture"/> to use as an icon for this attribute.</summary>
        protected internal override Texture Icon => Icons.AssetInstance;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Appends any optional properties that have been set on this attribute.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, ref bool isFirst)
        {
            base.AppendDetails(text, ref isFirst);

            if (DontAssign)
                AppendDetail(text, ref isFirst, nameof(DontAssign));

            if (StartInactive)
                AppendDetail(text, ref isFirst, nameof(StartInactive));
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the details this attribute
        /// wants built into it.
        /// </summary>
        protected internal override void GatherInjectorDetails(InjectorScriptBuilder builder)
        {
            if (Asset == null ||
                DontAssign)
                return;

            builder.AddToMethod("Awake", this, (text, indent) =>
            {
                var field = builder.AppendGetSerializedReference(text, indent);
                builder.AppendDontDestroyOnLoad(text, indent, field);
                builder.AppendSetValue(text, indent, this);
            });
        }

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called on each injector when building the runtime injector script to gather the values this attribute
        /// wants assigned it.
        /// </summary>
        protected internal override void SetupInjectorValues(InjectorScriptBuilder builder)
        {
            if (DontAssign)
            {
                GetValueToInject();
                return;
            }

            base.SetupInjectorValues(builder);
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

