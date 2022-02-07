// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to dependency injection.
    /// </summary>
    [Serializable]
    internal sealed class InjectionSettings : IOnCreate
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the procedural injector script should be generated so that injection attributes can be
        /// used in runtime code.
        /// </summary>
        public bool enableRuntimeInjection = true;

        /// <summary>
        /// Indicates whether overlay icons should be shown in the Project window for assets that are being used by
        /// or could be used by <see cref="AssetInjectionAttribute"/>s.
        /// </summary>
        public bool showProjectWindowOverlays = true;

        /// <summary>
        /// Indicates whether the system should automatically search for appropriate assets for any
        /// <see cref="AssetInjectionAttribute"/>s.
        /// </summary>
        public bool autoFindAssets = true;

        /// <summary>
        /// Indicates whether a message should be logged when a new asset is found for an <see cref="AssetInjectionAttribute"/>.
        /// </summary>
        public bool logAssetFound = true;

        /// <summary>
        /// Indicates whether the procedural injector script should have try/catch blocks around the code for each
        /// <see cref="InjectionAttribute"/> so that an exception caused by one won't prevent others from initialising.
        /// </summary>
        public bool catchInjectorExceptions = true;

        /// <summary>
        /// Indicates whether the procedural injector script should time its execution and log a message.
        /// </summary>
        public bool timeInjectorCode = false;

        /// <summary>
        /// Indicates whether the procedural injector script should log a message whenever it interacts with user code
        /// to aid in debugging.
        /// </summary>
        public bool logInjectorCode = false;

        [SerializeField]
        private List<AssetInjectionData> _AssetInjectionData;
        public static List<AssetInjectionData> AssetInjectionData => WeaverSettings.Injection._AssetInjectionData;

        /************************************************************************************************************************/

        void IOnCreate.OnCreate()
        {
            _AssetInjectionData = new List<AssetInjectionData>();
        }

        /************************************************************************************************************************/

        public static AssetInjectionData GetAssetInjectionData(AssetInjectionAttribute attribute, out bool isNew)
        {
            return Editor.AssetInjectionData.GetOrCreateData(ref WeaverSettings.Injection._AssetInjectionData, attribute, out isNew);
        }

        /************************************************************************************************************************/

        public void DoGUI()
        {
            if (WeaverEditorUtilities.DoToggle(ref showProjectWindowOverlays,
                "Show Project Window Overlays",
                "If enabled: an extra icon will be shown in the Project Window on each asset targeted by an asset injection attribute."))
            {
                ProjectWindowOverlays.OnShowSettingChanged();
            }

            WeaverEditorUtilities.DoToggle(ref autoFindAssets,
                "Auto Find Assets",
                "Indicates whether the system should automatically search for appropriate assets" +
                " for any asset injection attributes when they are first added.");

            WeaverEditorUtilities.DoToggle(ref logAssetFound,
                "Log Asset Found",
                "Indicates whether a message should be logged when a new asset is found for an asset injection attribute.");

            // Runtime.
            GUILayout.BeginVertical(GUI.skin.box);
            var enabled = GUI.enabled;

            const string ProOnly = "";

            WeaverEditorUtilities.DoToggle(ref enableRuntimeInjection,
                "Enable Runtime Injection",
                ProOnly + "Indicates whether the runtime injector script should be generated so that" +
                " injection attributes can be used in runtime code (for builds, not Play Mode in the editor).");

            if (!enableRuntimeInjection)
                GUI.enabled = false;

            WeaverEditorUtilities.DoToggle(ref catchInjectorExceptions,
                "Catch Injector Exceptions",
                ProOnly + "Indicates whether the runtime injector script should have try/catch blocks around the code for" +
                    " each injection attribute so that an exception caused by one won't prevent others from initialising.");

            WeaverEditorUtilities.DoToggle(ref timeInjectorCode,
                "Time Injector Code",
                ProOnly + "Indicates whether the runtime injector script should time its execution and log a message.");

            WeaverEditorUtilities.DoToggle(ref logInjectorCode,
                "Log Injector Code",
                ProOnly + "Indicates whether the runtime injector script should log a message" +
                " whenever it interacts with user code to aid in debugging.");

            GUI.enabled = enabled;
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/
    }
}

#endif

