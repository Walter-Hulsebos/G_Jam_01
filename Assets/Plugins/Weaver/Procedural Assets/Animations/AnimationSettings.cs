// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using Weaver.Editor.Procedural;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal] Settings relating to the <see cref="AnimationsScriptBuilder"/>.</summary>
    [Serializable]
    internal sealed class AnimationSettings : ProceduralScriptSettings
    {
        /************************************************************************************************************************/

        public bool
            notifyOnNewValue = true,
            createParameterWrappers = true;

        /************************************************************************************************************************/

        public override void DoGUI()
        {
            WeaverEditorUtilities.DoToggle(ref notifyOnNewValue,
                "Notify On New Value",
                "If enabled: this script will log a message while generating if a new value is found that wasn't present last time");

            WeaverEditorUtilities.DoToggle(ref createParameterWrappers,
                "Create Parameter Wrappers",
                "If enabled: the script will include extension methods for the Animator class which get and set each animation parameter");
        }

        /************************************************************************************************************************/
    }
}

#endif

