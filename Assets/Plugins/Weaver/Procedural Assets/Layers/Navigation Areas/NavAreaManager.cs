// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// The central point for the <see cref="NavigationAreasScriptBuilder"/> to access the details it needs.
    /// </summary>
    internal sealed class NavAreaManager : LayerManager
    {
        /************************************************************************************************************************/

        public static readonly new NavAreaManager Instance = new NavAreaManager();

        /************************************************************************************************************************/

        public override BaseLayerSettings Settings => WeaverSettings.NavAreas;

        public override LayersScriptBuilder ScriptBuilder => NavigationAreasScriptBuilder.Instance;

        public override int DefaultLayerCount => 3;

        public override string[] NewLayerNames => GameObjectUtility.GetNavMeshAreaNames();

        public override int NameToValue(string name) => GameObjectUtility.GetNavMeshAreaFromName(name);

        /************************************************************************************************************************/
    }
}

#endif

