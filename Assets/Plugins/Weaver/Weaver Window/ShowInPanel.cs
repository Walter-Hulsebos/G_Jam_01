// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;

namespace Weaver.Editor.Window
{
    /// <summary>[Editor-Only] Indicates which <see cref="WeaverWindowPanel"/> something should be shown in.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ShowInPanelAttribute : Attribute
    {
        /************************************************************************************************************************/

        /// <summary>The type of the <see cref="WeaverWindowPanel"/> to show the target in.</summary>
        public readonly Type PanelType;

        /// <summary>Indicates whether the injector should be shown in its regular panel as well.</summary>
        public bool ShowInMain { get; set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ShowInPanelAttribute"/> with the specified <see cref="PanelType"/>.</summary>
        public ShowInPanelAttribute(Type panelType)
        {
            PanelType = panelType;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the <see cref="PanelType"/> associated with the `injector`.</summary>
        public static Type GetPanelType(InjectionAttribute injector, out Type secondaryPanel)
        {
            var attribute = injector.Member.GetAttribute<ShowInPanelAttribute>();
            if (attribute != null)
            {
                secondaryPanel = attribute.ShowInMain ? GetDefaultPanelType(injector) : null;
                return attribute.PanelType;
            }

            secondaryPanel = null;
            return GetDefaultPanelType(injector);
        }

        /************************************************************************************************************************/

        /// <summary>Returns the default <see cref="PanelType"/> of the `injector`.</summary>
        public static Type GetDefaultPanelType(InjectionAttribute injector)
        {
            if (injector is AssetInjectionAttribute asset && asset.ProceduralAsset != null)
                return typeof(ProceduralPanel);
            else
                return typeof(InjectionPanel);
        }

        /************************************************************************************************************************/
    }
}

#endif

