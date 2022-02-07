// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Weaver.Editor;
using Weaver.Editor.Procedural;
using Weaver.Editor.Window;
using Object = UnityEngine.Object;
#endif

namespace Weaver
{
    /// <summary>An <see cref="InjectionAttribute"/> for attributes that inject an asset reference.</summary>
    public abstract class AssetInjectionAttribute : InjectionAttribute
    {
        /************************************************************************************************************************/
        #region Constructor Properties
        /************************************************************************************************************************/

        /// <summary>If set, this value will be used as the ideal file name when searching for the target asset.</summary>
        public string FileName { get; set; }

        /************************************************************************************************************************/

        /// <summary>If true, this attribute will not search for a target asset by name when it is first applied.</summary>
        public bool DisableAutoFind { get; set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        private bool _HasInitializedProceduralAsset;
        private ProceduralAsset _ProceduralAsset;

        /// <summary>[Editor-Only] The <see cref="Editor.Procedural.ProceduralAsset"/> associated with this attribute (if any).</summary>
        internal ProceduralAsset ProceduralAsset
        {
            get
            {
                if (!_HasInitializedProceduralAsset)
                {
                    _HasInitializedProceduralAsset = true;
                    _ProceduralAsset = ProceduralAsset.TryCreate(this, out var error);
                    if (error != null)
                        Debug.LogWarning($"{this} is not a valid [{nameof(ProceduralAsset)}]: {error}");
                }

                return _ProceduralAsset;
            }
        }

        /************************************************************************************************************************/

        private AssetInjectionData _SavedData;

        internal AssetInjectionData SavedData
        {
            get
            {
                AcquireSavedData();
                return _SavedData;
            }
        }

        /// <summary>[Editor-Only]
        /// Ensures that the <see cref="SavedData"/> has been acquired. If new data was created due to none existing
        /// previously, this method also tries to find an appropriate asset to assign to this attribute based on the
        /// attributed member's name.
        /// </summary>
        private void AcquireSavedData()
        {
            if (_SavedData != null)
                return;

            _SavedData = InjectionSettings.GetAssetInjectionData(this, out var isNew);

            if (isNew && !DisableAutoFind && WeaverSettings.Injection.autoFindAssets)
                _SavedData.TryFindTargetAsset();

            AssetInjectionOverlay.OnAfterAssetChanged(this);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The target asset.</summary>
        internal Object Asset
        {
            get => SavedData.Asset;
            private set
            {
                AssetInjectionOverlay.OnBeforeAssetChanged(this);
                SavedData.Asset = value;
                AssetInjectionOverlay.OnAfterAssetChanged(this);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// <para></para>
        /// Specifically, this method ensures that the <see cref="InjectionAttribute.MemberType"/> inherits from
        /// <see cref="Object"/>.
        /// </summary>
        protected override bool TryInitialize() => TryInitialize(MemberType);

        /// <summary>[Editor-Only]
        /// Attempts to initialize this attribute and returns true if successful.
        /// <para></para>
        /// Specifically, this method ensures that the <see cref="InjectionAttribute.MemberType"/> inherits from
        /// <see cref="Object"/>.
        /// </summary>
        protected bool TryInitialize(Type assetType)
        {
            if (!typeof(Object).IsAssignableFrom(assetType))
                return LogThisInvalidAttribute("doesn't inherit from UnityEngine.Object.");

            if (WeaverEditorUtilities.IsEditorOnly(assetType))
                EditorOnly = true;

            return true;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Injection
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Ensures that the specified `asset` can be injected by this attribute and assigns it to the
        /// <see cref="Asset"/>. The return value indicates whether it was assigned.
        /// </summary>
        protected internal bool TrySetAsset(Object asset)
        {
            if (asset != null && !ValidateAsset(asset))
                return false;

            Asset = asset;

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether the specified `asset` can be injected into the attributed
        /// <see cref="InjectionAttribute.Member"/> by this attribute.
        /// </summary>
        public virtual bool ValidateAsset(Object asset)
        {
            return
                AssetDatabase.Contains(asset) &&
                AssetType.IsAssignableFrom(asset.GetType());
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// The type of asset which will be injected into the attributed <see cref="InjectionAttribute.Member"/>.
        /// </summary>
        public virtual Type AssetType => MemberType;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Assigns the appropriate value to the attributed property.
        /// </summary>
        protected internal override void InjectValue()
        {
            // Make sure we've got the target asset to show the Project Window Overlay.
            AcquireSavedData();

            base.InjectValue();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a message indicating that no value has been assigned for this attribute.
        /// </summary>
        protected override string GetMissingValueMessage()
        {
            return
                "No asset has been assigned for " + this +
                "\nUse " + WeaverUtilities.WeaverWindowPath + " to assign an asset or set '" +
                nameof(Optional) + " = true' in the attribute's constructor.";
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Indicates whether this attribute should currently be visible in the <see cref="WeaverWindow"/>.
        /// </summary>
        public override bool ShouldShow
        {
            get
            {
                if (ProceduralAsset != null)
                    return ProceduralAsset.ShouldShow();

                return base.ShouldShow;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the inspector GUI for this attribute.</summary>
        public override void DoInspectorGUI()
        {
            if (ProceduralAsset != null)
            {
                ProceduralAsset.DoGUI();
                return;
            }

            GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
            GUILayout.BeginHorizontal();

            DoAssetField();

            if (!AssetType.IsAbstract && Asset == null)
                DoCreateButton();

            DoFindButton();

            GUILayout.EndHorizontal();
            GUI.enabled = true;

            CheckContextMenu();
        }

        /************************************************************************************************************************/

        private static readonly GUILayoutOption[]
            SingleLineHeight = { GUILayout.Height(EditorGUIUtility.singleLineHeight) };

        /// <summary>[Editor-Only]
        /// Draws an inspector field for the <see cref="Asset"/>.
        /// </summary>
        public void DoAssetField()
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            WeaverWindow.DoPingGUI(rect, this);

            var color = GUI.color;
            if (Asset == null)
                GUI.color = Optional ? WeaverEditorUtilities.WarningColor : WeaverEditorUtilities.ErrorColor;

            EditorGUI.BeginChangeCheck();
            var asset = EditorGUI.ObjectField(rect, GetTempLabelContent(), Asset, AssetType, false);
            if (EditorGUI.EndChangeCheck())
                ManuallyAssignAsset(asset);

            GUI.color = color;
        }

        /************************************************************************************************************************/

        private string _InspectorTooltipAssetPath;

        /// <summary>[Editor-Only]
        /// Builds, caches, and returns a tooltip message for this attribute.
        /// </summary>
        protected override string GetInspectorTooltip()
        {
            var assetPath = Asset != null ? AssetDatabase.GetAssetPath(Asset) : null;
            if (_InspectorTooltipAssetPath != assetPath)
            {
                _InspectorTooltipAssetPath = assetPath;
                _InspectorTooltip = null;
            }

            if (_InspectorTooltip == null)
            {
                var tooltip = WeaverUtilities.GetStringBuilder()
                    .Append(base.ToString());

                if (_InspectorTooltipAssetPath != null)
                    tooltip.AppendLineConst().AppendLineConst().Append(_InspectorTooltipAssetPath);

                if (Tooltip != null)
                    tooltip.AppendLineConst().AppendLineConst().Append(Tooltip);

                _InspectorTooltip = tooltip.ReleaseToString();
            }

            return _InspectorTooltip;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws a button to attempt to find the target asset using an <see cref="AssetPathMatcher"/>.
        /// </summary>
        protected void DoFindButton()
        {
            if (!GUILayout.Button(WeaverEditorUtilities.TempContent("F", "Find target asset"), GUIStyles.SmallButtonStyle))
                return;

            WeaverEditorUtilities.ClearAssetPathCache();

            if (_SavedData.TryFindTargetAsset())
            {
                InjectValue();
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws a button to attempt to create a default asset of the <see cref="AssetType"/> in the procedural asset
        /// output directory.
        /// </summary>
        protected void DoCreateButton()
        {
            if (!GUILayout.Button(WeaverEditorUtilities.TempContent("C", "Create new asset"), GUIStyles.SmallButtonStyle))
                return;

            WeaverEditorUtilities.ClearAssetPathCache();

            Asset = ProceduralAssetSettings.CreateNewAssetInOutputDirectory(AssetType, Member.GetNameCS(), out var assetPath);

            Debug.Log($"Created {AssetType.GetNameCS()} at {assetPath}", Asset);

            InjectValue();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Tries to set the <see cref="Asset"/> and records an <see cref="Undo"/> state for it.
        /// </summary>
        protected void ManuallyAssignAsset(Object asset)
        {
            Undo.RecordObject(WeaverSettings.Instance, "Set Injection Attribute Asset");

            if (TrySetAsset(asset))
            {
                WeaverEditorUtilities.ClearAssetPathCache();
                InjectValue();
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Adds various functions for this attribute to the `menu`.
        /// </summary>
        public override void AddItemsToMenu(GenericMenu menu)
        {
            base.AddItemsToMenu(menu);

            // Open in Explorer.
            var assetPath = AssetDatabase.GetAssetPath(Asset);
            if (assetPath != null)
                menu.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(assetPath));
            else
                menu.AddDisabledItem(new GUIContent("Show in Explorer"));

            // Procedural Asset Specifics.
            if (ProceduralAsset != null)
                ProceduralAsset.AddItemsToMenu(menu);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Appends any optional properties that have been set on this attribute.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, ref bool isFirst)
        {
            base.AppendDetails(text, ref isFirst);

            if (ProceduralAsset != null)
                AppendDetail(text, ref isFirst, "Procedural");
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building

        /// <summary>[Editor-Only] [Pro-Only]
        /// Called during a build by <see cref="UnityEditor.Build.IPreprocessBuildWithReport.OnPreprocessBuild"/>.
        /// <para></para>
        /// Logs a warning if no <see cref="Asset"/> is assigned and this attribute isn't marked as
        /// <see cref="InjectionAttribute.Optional"/>.
        /// </summary>
        protected internal override void OnStartBuild()
        {
            if (Asset == null && !Optional)
                Debug.LogWarning(GetMissingValueMessage());
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
                builder.AppendGetSerializedReference(text, indent);
                builder.AppendSetValue(text, indent, this);
            });
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

            var value = (Object)GetValueToInject();
            builder.AddObjectReference(value, this);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Asset Lookup
        /************************************************************************************************************************/

        private static List<AssetInjectionAttribute> _AllAssetInjectors;

        /// <summary>[Editor-Only]
        /// A subset of <see cref="InjectorManager.AllInjectionAttributes"/> containing only asset injectors.
        /// </summary>
        public static List<AssetInjectionAttribute> AllAssetInjectors
        {
            get
            {
                if (_AllAssetInjectors == null)
                {
                    _AllAssetInjectors = new List<AssetInjectionAttribute>();
                    for (int i = 0; i < InjectorManager.AllInjectionAttributes.Count; i++)
                    {
                        if (InjectorManager.AllInjectionAttributes[i] is AssetInjectionAttribute injector)
                            _AllAssetInjectors.Add(injector);
                    }
                }

                return _AllAssetInjectors;
            }
        }

        /************************************************************************************************************************/

        private static Dictionary<MemberInfo, AssetInjectionAttribute> _MemberToAssetInjector;

        /// <summary>[Editor-Only]
        /// Returns the <see cref="AssetInjectionAttribute"/> on the specified `member` (if any).
        /// </summary>
        public static AssetInjectionAttribute Get(MemberInfo member)
        {
            if (_MemberToAssetInjector == null)
            {
                _MemberToAssetInjector = new Dictionary<MemberInfo, AssetInjectionAttribute>();
                var all = AllAssetInjectors;
                for (int i = 0; i < all.Count; i++)
                {
                    var attribute = all[i];
                    _MemberToAssetInjector.Add(attribute.Member, attribute);
                }
            }

            _MemberToAssetInjector.TryGetValue(member, out var injector);
            return injector;
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<object, Object>
            ValueToAsset = new Dictionary<object, Object>();

        /// <summary>[Editor-Only]
        /// Tries to set the value of the attributed member. Catches and logs any exceptions.
        /// </summary>
        public override void SetValue(object value)
        {
            // We could store and unregister the previous value but there's not much point since this generally only needs to
            // happen on initialisation.

            base.SetValue(value);

            if (value != null)
                ValueToAsset[value] = Asset;
        }

        /// <summary>[Editor-Only]
        /// Returns the original asset that was used to inject the specified `value` (if any).
        /// </summary>
        public static Object GetSourceAsset(object value)
        {
            ValueToAsset.TryGetValue(value, out var asset);
            return asset;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

