// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Reflection;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Weaver.Editor.Procedural
{
    /// <summary>
    /// A group of details relating to an <see cref="AssetInjectionAttribute"/> which allow its target asset to be
    /// procedurally generated.
    /// </summary>
    public sealed class ProceduralAsset
#if UNITY_EDITOR
       : IDependant<ProceduralAsset>, IHasCustomMenu
#endif
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The target <see cref="AssetInjectionAttribute"/>.</summary>
        public readonly AssetInjectionAttribute Injector;

        /// <summary>[Editor-Only] The target <see cref="ProceduralAssetAttribute"/>.</summary>
        public readonly ProceduralAssetAttribute ProceduralAttribute;

        /// <summary>[Editor-Only] The <see cref="AssetGenerator"/> that will be used to generate and save the asset.</summary>
        public readonly AssetGenerator AssetGenerator;

        /// <summary>[Editor-Only] The method that will be used to generate the asset.</summary>
        public readonly MethodInfo GeneratorMethod;

        /// <summary>[Editor-Only]
        /// A function that determines whether this asset should generate its asset at the moment.
        /// </summary>
        private readonly Func<bool> ShouldGenerateFunc;

        /// <summary>[Editor-Only]
        /// A function that determines whether this asset should be shown in the <see cref="Window.WeaverWindow"/> at
        /// the moment.
        /// </summary>
        private readonly Func<bool> ShouldShowFunc;

        /************************************************************************************************************************/

        private ProceduralAsset(AssetInjectionAttribute injector, ProceduralAssetAttribute proceduralAttribute,
            AssetGenerator assetGenerator, MethodInfo generatorMethod)
        {
            Injector = injector;
            ProceduralAttribute = proceduralAttribute;
            AssetGenerator = assetGenerator;
            GeneratorMethod = generatorMethod;

            ShouldGenerateFunc = TryGetCheckerFunc(ProceduralAttribute.CheckShouldGenerate);
            ShouldShowFunc = TryGetCheckerFunc(ProceduralAttribute.CheckShouldShow);
        }

        /************************************************************************************************************************/

        private Func<bool> TryGetCheckerFunc(string memberName)
        {
            var func = ReflectionUtilities.GetMemberFunc<bool>(
                Injector.Member.DeclaringType,
                memberName,
                null,
                out var error,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (error != null)
                Debug.LogWarning($"{WeaverEditorUtilities.GetAttributeDisplayString(typeof(ProceduralAssetAttribute))}" +
                    $" on {Injector.Member.GetNameCS()} error: {error}");

            return func;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// If the `injector`s attributed member has a <see cref="ProceduralAssetAttribute"/> this method tries to find
        /// a suitable <see cref="Procedural.AssetGenerator"/> and generator method. If successful, it creates and
        /// returns a <see cref="ProceduralAsset"/> containing those details.
        /// </summary>
        public static ProceduralAsset TryCreate(AssetInjectionAttribute injector, out string error)
        {
            error = null;

            var proceduralAttribute = injector.Member.GetAttribute<ProceduralAssetAttribute>();
            if (proceduralAttribute == null)
                return null;

            if (proceduralAttribute.Generator == null)
                proceduralAttribute.Generator = "Generate" + injector.Member.Name;

            if (injector.AssetType == null)
            {
                error = "the injector has no AssetType. " + injector;
                return null;
            }

            var assetGenerator = AssetGenerators.GetAssetGenerator(injector.AssetType);
            if (assetGenerator == null)
            {
                error = "no suitable " + nameof(Procedural.AssetGenerator) + " type was found";
                return null;
            }

            var generatorMethod = assetGenerator.GetGeneratorMethod(injector.Member, proceduralAttribute.Generator);
            if (generatorMethod == null)
            {
                error = "there is no applicable generator method named " + injector.Member.DeclaringType.GetNameCS() + "." + proceduralAttribute.Generator;
                return null;
            }

            return new ProceduralAsset(injector, proceduralAttribute, assetGenerator, generatorMethod);
        }

        /************************************************************************************************************************/

        private ProceduralAssetData _SavedProceduralData;

        internal ProceduralAssetData SavedProceduralData
        {
            get
            {
                if (_SavedProceduralData == null)
                    _SavedProceduralData = ProceduralAssetSettings.GetProceduralAssetData(this);
                return _SavedProceduralData;
            }
        }

        /************************************************************************************************************************/

        private ProceduralAssetListData _SavedListData;

        internal ProceduralAssetListData SavedListData
        {
            get
            {
                if (_SavedListData == null)
                    _SavedListData = ProceduralAssetSettings.GetProceduralAssetListData(this);
                return _SavedListData;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Determines whether this asset should generate its asset at the moment.
        /// </summary>
        public bool ShouldGenerate()
        {
            if (ShouldGenerateFunc != null)
                return ShouldGenerateFunc();
            else
                return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Determines whether this asset should be shown in the <see cref="Window.WeaverWindow"/> at the moment.
        /// </summary>
        public bool ShouldShow()
        {
            if (ShouldShowFunc != null)
                return ShouldShowFunc();
            else
                return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor Only]
        /// The asset path at which the currently generating asset will be saved.
        /// </summary>
        public static string CurrentAssetPath { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Internal, Editor Only]
        /// Generates this asset and saves it in the project. Called by the <see cref="AssetGeneratorWindow"/>.
        /// </summary>
        internal void GenerateAndSave()
        {
            OnImportAsset = null;

            // Determine the asset path.
            CurrentAssetPath = DetermineDestinationAssetPath();

            // Generate the asset.
            AssetGeneratorWindow.OnBeginGeneratorMethod();
            var asset = AssetGenerator.InvokeGeneratorMethod(this);
            AssetGeneratorWindow.OnEndGeneratorMethod();

            // Save the asset.
            if (asset != null)
            {
                // If the asset has been given a name, use it as the file name the first time it is generated.
                if (Injector.Asset == null && !string.IsNullOrEmpty(asset.name))
                {
                    var directory = Path.GetDirectoryName(CurrentAssetPath);
                    var extension = Path.GetExtension(CurrentAssetPath);
                    CurrentAssetPath = directory + "/" + asset.name + extension;
                }

                AssetGenerator.Save(ref asset, CurrentAssetPath, out SavedProceduralData.hasSubAssets);

                if (Injector.TrySetAsset(asset))
                {
                    Injector.InjectValue();
                }

                SavedProceduralData.SerializeDependancies();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="AssetInjectionAttribute.FileName"/> if one was specified. Otherwise returns the full
        /// name of the <see cref="InjectionAttribute.Member"/>; with or without the namespace depending on the
        /// <see cref="ProceduralAssetSettings.includeNamespaceInName"/> setting.
        /// </summary>
        public string GetRealFileName()
        {
            return
                Injector.FileName ??
                Injector.Member.GetNameCS(ProceduralAssetSettings.Instance.includeNamespaceInName ? CSharp.NameVerbosity.Full : CSharp.NameVerbosity.Basic);
        }

        /************************************************************************************************************************/

        private string DetermineDestinationAssetPath()
        {
            if (Injector.Asset != null)
            {
                return AssetGenerator.GetAssetPathAndDestroyOldSubAssets(this);
            }
            else
            {
                var fileName = GetRealFileName();

                var fileExtension = ProceduralAttribute.FileExtension;

                if (fileExtension == null)
                    fileExtension = AssetGenerator.DefaultFileExtension;

                if (fileExtension.Length > 0 && fileExtension[0] != '.')
                    fileExtension = "." + fileExtension;

                var directory = ProceduralAssetSettings.EnsureOutputDirectoryExists();
                return directory + fileName + fileExtension;
            }
        }

        /************************************************************************************************************************/

        internal bool UseTempScene()
        {
            switch (ProceduralAttribute.OptionalUseTempScene)
            {
                case OptionalBool.True: return true;
                case OptionalBool.False: return false;
                default:
                case OptionalBool.Unspecified: return AssetGenerator.UseTempScene(this);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// This callback is cleared before generation begins and triggered after the asset generates and is saved.
        /// </summary>
        public static event Action<AssetImporter> OnImportAsset;

        internal static void InvokeOnImportAsset(string assetPath)
        {
            OnImportAsset?.Invoke(AssetImporter.GetAtPath(assetPath));
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws the GUI for this attribute in the inspector.
        /// </summary>
        public void DoGUI()
        {
            GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
            GUILayout.BeginHorizontal();

            Injector.DoAssetField();

            var content = WeaverEditorUtilities.TempContent(
                "G",
                "Left Click = Generate This\nRight Click = Generate All in this Namespace\nMiddle Click = Generate All");

            if (GUILayout.Button(content, GUIStyles.SmallButtonStyle))
            {
                switch (Event.current.button)
                {
                    default:
                    case 0:
                        WeaverEditorUtilities.ForceGenerate = true;
                        Generate();
                        break;

                    case 1:
                        GenerateNamespace(Injector.Member.DeclaringType.Namespace);
                        break;

                    case 2:
                        GenerateAll();
                        break;
                }
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;

            Injector.CheckContextMenu();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Adds various functions for this asset to the `menu`.
        /// </summary>
        public void AddItemsToMenu(GenericMenu menu)
        {
            // Generate.
            menu.AddItem(new GUIContent("Generate"), false, () =>
            {
                WeaverEditorUtilities.ForceGenerate = true;
                Generate();
            });

            // Log Dependancies.
            menu.AddItem(new GUIContent("Log Dependancies"), false, () => Debug.Log("Dependancies for " + Injector + ": " + SavedProceduralData.Dependancies.DeepToString(), Injector.Asset));

            // Asset Generator Specifics.
            AssetGenerator.AddItemsToMenu(menu, this);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns a string describing this <see cref="ProceduralAsset"/>.
        /// </summary>
        public override string ToString()
        {
            return $"{Injector}:" +
                $" GeneratorType={AssetGenerator.GetType().GetNameCS()}," +
                $" GeneratorMethod={GeneratorMethod.GetNameCS()}";
        }

        /************************************************************************************************************************/
        #region Static Access
        /************************************************************************************************************************/

        private static List<ProceduralAsset> _AllProceduralAssets;

        /// <summary>[Editor-Only]
        /// A list of every <see cref="ProceduralAsset"/>.
        /// </summary>
        public static List<ProceduralAsset> AllProceduralAssets
        {
            get
            {
                if (_AllProceduralAssets == null)
                {
                    _AllProceduralAssets = new List<ProceduralAsset>();
                    var assetInjectors = AssetInjectionAttribute.AllAssetInjectors;
                    for (int i = 0; i < assetInjectors.Count; i++)
                    {
                        var injector = assetInjectors[i];
                        if (injector.ProceduralAsset != null)
                            _AllProceduralAssets.Add(injector.ProceduralAsset);
                    }
                }

                return _AllProceduralAssets;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns the <see cref="ProceduralAsset"/> which uses the specified `generatorMethod` (if any).
        /// </summary>
        public static ProceduralAsset GetFromGenerator(MethodInfo generatorMethod)
        {
            var fields = generatorMethod.DeclaringType.GetFields(ReflectionUtilities.StaticBindings);
            for (int i = 0; i < fields.Length; i++)
            {
                var injector = AssetInjectionAttribute.Get(fields[i]);
                if (injector != null && injector.ProceduralAsset.GeneratorMethod == generatorMethod)
                    return injector.ProceduralAsset;
            }

            var properties = generatorMethod.DeclaringType.GetProperties(ReflectionUtilities.StaticBindings);
            for (int i = 0; i < properties.Length; i++)
            {
                var injector = AssetInjectionAttribute.Get(properties[i]);
                if (injector != null && injector.ProceduralAsset.GeneratorMethod == generatorMethod)
                    return injector.ProceduralAsset;
            }

            return null;
        }

        /// <summary>[Editor-Only]
        /// Returns the <see cref="ProceduralAsset"/> which uses the specified `generatorMethod` (if any).
        /// </summary>
        public static ProceduralAsset GetFromGenerator(Delegate generatorMethod) => GetFromGenerator(generatorMethod.Method);

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Generates the procedural asset associated with the specified `generatorMethod`.
        /// </summary>
        public static void Generate(MethodInfo generatorMethod, bool immediate = false)
        {
            var asset = GetFromGenerator(generatorMethod);
            if (asset != null)
                asset.Generate(immediate);
        }

        /// <summary>[Editor-Only]
        /// Generates this procedural asset.
        /// </summary>
        public void Generate(bool immediate = false)
        {
            AssetGeneratorWindow.AssetsToGenerate.Add(this);
            AssetGeneratorWindow.Generate(immediate);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Generates every procedural asset in the specified `nameSpace`.
        /// </summary>
        public static void GenerateNamespace(string nameSpace, bool immediate = false)
        {
            for (int i = 0; i < AllProceduralAssets.Count; i++)
            {
                var asset = AllProceduralAssets[i];
                if (asset.Injector.Member.DeclaringType.Namespace == nameSpace)
                    AssetGeneratorWindow.AssetsToGenerate.Add(asset);
            }

            AssetGeneratorWindow.Generate(immediate);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Generates every procedural asset in the project.
        /// </summary>
        public static void GenerateAll(bool immediate = false)
        {
            AssetGeneratorWindow.AssetsToGenerate.AddRange(AllProceduralAssets);
            AssetGeneratorWindow.Generate(immediate);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// If there are any procedural assets which don't currently have a target asset, this method asks the user
        /// what they want to do about it.
        /// </summary>
        public static void CheckForMissingAssets()
        {
            if (!ProceduralAssetSettings.Instance.checkForMissingAssets ||
                EditorUtility.scriptCompilationFailed)
                return;

            var missingAssets = new List<ProceduralAsset>();

            var all = AllProceduralAssets;
            for (int i = 0; i < all.Count; i++)
            {
                var asset = all[i];
                if (asset.Injector.Asset == null && asset.ShouldGenerate())
                    missingAssets.Add(asset);
            }

            AskAboutMissingAssets(missingAssets);
        }

        /************************************************************************************************************************/

        private static void AskAboutMissingAssets(List<ProceduralAsset> missingAssets)
        {
            if (missingAssets.Count == 0)
                return;

            var message = WeaverUtilities.GetStringBuilder();
            if (missingAssets.Count == 1)
            {
                message.Append(missingAssets[0].Injector);
                message.Append(" hasn't been generated. Would you like to generate it now?");
            }
            else
            {
                message.Append("There are ");
                message.Append(missingAssets.Count);
                message.AppendLineConst(" procedural assets which haven't been generated. Would you like to generate them now?");

                for (int i = 0; i < missingAssets.Count; i++)
                {
                    message.AppendLineConst();
                    message.Append("- ").Append(missingAssets[i].Injector.Member.GetNameCS());

                    if (i >= 9)
                    {
                        message.AppendLineConst();
                        message.AppendLineConst("...");
                        break;
                    }
                }
            }

            var result = EditorUtility.DisplayDialogComplex("Generate Missing Assets?", message.ReleaseToString(),
                "Generate", "Ignore", "Don't Ask Again");
            switch (result)
            {
                case 0:// Generate.
                    AssetGeneratorWindow.AssetsToGenerate.AddRange(missingAssets);
                    AssetGeneratorWindow.Generate(true);
                    break;

                default:
                case 1:// Ignore.
                    break;

                case 2:// Don't Ask Again.
                    ProceduralAssetSettings.Instance.checkForMissingAssets = false;
                    break;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #region Dependancies
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Other assets which must always be generated before this one.</summary>
        public IEnumerable<ProceduralAsset> Dependancies => SavedProceduralData.Dependancies;
#endif

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first.
        /// </summary>
        [System.Diagnostics.Conditional(WeaverUtilities.UnityEditor)]
        public static void MarkDependancy(MethodInfo dependancy)
        {
#if UNITY_EDITOR
            if (AssetGeneratorWindow.CurrentlyGenerating != null)
            {
                var attribute = GetFromGenerator(dependancy);
                if (attribute == null)
                    throw new ArgumentException("The specified method is not the generator method of a procedural asset.");

                AssetGeneratorWindow.VerifyAssetDependancy(attribute);
                return;
            }
#endif

            throw new InvalidOperationException($"You can only call {nameof(MarkDependancy)} while an asset is being generated.");
        }

        /// <summary>[Editor-Conditional]
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first.
        /// </summary>
        [System.Diagnostics.Conditional(WeaverUtilities.UnityEditor)]
        public static void MarkDependancy(Delegate dependancy) => MarkDependancy(dependancy.Method);

        /// <summary>[Editor-Conditional]
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first.
        /// </summary>
        [System.Diagnostics.Conditional(WeaverUtilities.UnityEditor)]
        public static void MarkDependancy<T>(Func<T> dependancy) => MarkDependancy(dependancy.Method);

        /************************************************************************************************************************/

        /// <summary>
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first. Then returns the target asset of that `dependancy`.
        /// <para></para>
        /// This method can only be called while an asset is being generated. Otherwise it throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public static T GrabDependancy<T>(MethodInfo dependancy)
        {
#if UNITY_EDITOR
            if (AssetGeneratorWindow.CurrentlyGenerating != null)
            {
                var asset = GetFromGenerator(dependancy);
                if (asset == null)
                    throw new ArgumentException("The specified method is not the generator method of a procedural asset.");

                AssetGeneratorWindow.VerifyAssetDependancy(asset);

                return (T)asset.Injector.GetValue();
            }
#endif

            throw new InvalidOperationException($"You can only call {nameof(GrabDependancy)} while an asset is being generated.");
        }

        /// <summary>
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first. Then returns the target asset of that `dependancy`.
        /// <para></para>
        /// This method can only be called while an asset is being generated. Otherwise it throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public static T GrabDependancy<T>(Delegate dependancy) => GrabDependancy<T>(dependancy.Method);

        /// <summary>
        /// Marks the currently generating asset as requiring the specified `dependancy` so that whenever either is
        /// generated, the dependancy is generated first. Then returns the target asset of that `dependancy`.
        /// <para></para>
        /// This method can only be called while an asset is being generated. Otherwise it throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public static T GrabDependancy<T>(Func<T> dependancy) => GrabDependancy<T>(dependancy.Method);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

