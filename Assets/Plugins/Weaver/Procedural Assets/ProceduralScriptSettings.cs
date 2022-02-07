// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Base class for settings relating to a procedural script.
    /// </summary>
    public abstract class ProceduralScriptSettings
    {
        /************************************************************************************************************************/

        /// <summary>Should the script be generated?</summary>
        public bool enabled;

        /// <summary>Draws the GUI for these settings.</summary>
        public virtual void DoGUI() { }

        /************************************************************************************************************************/
    }
}

#endif

