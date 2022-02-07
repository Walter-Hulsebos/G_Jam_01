// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// An <see cref="EditorWindow"/> which executes the generation of procedural assets.
    /// </summary>
    internal sealed class AssetGeneratorWindow : EditorWindow
    {
        /************************************************************************************************************************/
        #region Fields
        /************************************************************************************************************************/

        public static readonly List<ProceduralAsset>
            AssetsToGenerate = new List<ProceduralAsset>();

        /// <summary>
        /// Called when all assets are finished generating.
        /// The parameter indicates whether generation was successful.
        /// </summary>
        public static event Action<bool>
            OnComplete;

        private static Scene _TemporaryScene;
        private static bool _CreatedTemporaryScene;
        private static List<ProceduralAsset> _AssetsBeingGenerated;
        private static AssetGeneratorWindow _Instance;

        private static Exception _Exception;
        private static UnityEngine.Object _ExceptionSource;
        private static int _ExceptionSourceLine;

        private readonly List<GUIContent> Labels = new List<GUIContent>();
        private GUIStyle _CurrentItemStyle;
        private Vector2 _ScrollPosition;
        private Action<bool> _OnComplete;
        private int _CurrentAsset, _FirstAssetLabel;
        private bool _IsGenerating, _IsShiftDown;
        private double _GenerationStartTime;

        public static ProceduralAsset CurrentlyGenerating { get; private set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Generation
        /************************************************************************************************************************/

        public static void Generate(bool immediate = false)
        {
            if (_Instance != null ||
                _AssetsBeingGenerated != null)
                return;

            if (EditorApplication.isPlaying)
            {
                AssetsToGenerate.Clear();
                return;
            }

            for (int i = AssetsToGenerate.Count - 1; i >= 0; i--)
            {
                if (!AssetsToGenerate[i].ShouldGenerate())
                    AssetsToGenerate.RemoveAt(i);
            }

            if (AssetsToGenerate.Count == 0)
                return;

            if (!immediate)
            {
                EditorApplication.LockReloadAssemblies();
                var window = CreateInstance<AssetGeneratorWindow>();
                window.Initialize();
                window.ShowAuxWindow();
            }
            else
            {
                GenerateImmediate();
            }
        }

        /************************************************************************************************************************/

        private static void GenerateImmediate()
        {
            do
            {
                bool success;

                try
                {
                    _AssetsBeingGenerated = SortAssetsToGenerate();

                    ShowProgressBar("", 0);

                    CreateTemporaryScene();

                    for (int i = 0; i < _AssetsBeingGenerated.Count; i++)
                    {
                        var asset = _AssetsBeingGenerated[i];

                        ShowProgressBar(GetCurrentAssetPath(asset), i / (float)_AssetsBeingGenerated.Count);

                        if (!GenerateAsset(asset))
                        {
                            success = false;
                            goto GenerationFailed;
                        }
                    }

                    success = true;

                    GenerationFailed:

                    ShowProgressBar("Done", 1);

                    WeaverEditorUtilities.ForceGenerate = false;
                    RefreshSelection();
                }
                finally
                {
                    _AssetsBeingGenerated = null;
                    CloseTemporaryScene();
                }

                OnComplete?.Invoke(success);
            }
            while (AssetsToGenerate.Count > 0);

            AssetDatabase.Refresh();
        }

        /************************************************************************************************************************/

        private static void ShowProgressBar(string info, float progress)
        {
            if (WeaverEditorUtilities.IsPreprocessingBuild && ProceduralAssetSettings.Instance.autoGenerateOnBuild)
                EditorUtility.DisplayProgressBar("Generating Procedural Assets", info, progress);

            //if (WeaverUtilities.IsPreprocessingBuild &&
            //    EditorUtility.DisplayCancelableProgressBar("Generating Procedural Assets", info, progress))
            //{
            //    throw new UnityEditor.Build.BuildFailedException("Cancelled Generating Procedural Assets");
            //}
        }

        /************************************************************************************************************************/

        private void Initialize()
        {
            _GenerationStartTime = EditorApplication.timeSinceStartup;
            _Instance = this;
            _OnComplete = OnComplete;
            OnComplete = null;

            //Debug.LogTemp("Generating " + AssetsToGenerate.DeepToString());

            _AssetsBeingGenerated = SortAssetsToGenerate();

            CreateTemporaryScene();
            InitializeLabels();

            Focus();
            _IsGenerating = true;
        }

        /************************************************************************************************************************/

        public void InitializeLabels()
        {
            _CurrentItemStyle = InternalGUI.DoingStyle;

            for (int i = 0; i < _AssetsBeingGenerated.Count; i++)
            {
                Labels.Add(BuildGeneratorLabel(_AssetsBeingGenerated[i]));
            }
        }

        /************************************************************************************************************************/

        public static GUIContent BuildGeneratorLabel(ProceduralAsset asset)
        {
            // Method Signature -> Asset Path.

            var text = WeaverUtilities.GetStringBuilder();

            if (asset.GeneratorMethod != null)
            {
                CSharp.AppendSignature(asset.GeneratorMethod, text, CSharp.NameVerbosity.Full, false, true, false);
                WeaverUtilities.ApplyBoldTagsToLastSection(text, 1, 2);
                text.Append(" -> ");
            }

            text.Append(GetCurrentAssetPath(asset));
            WeaverUtilities.ApplyBoldTagsToLastSection(text, 1, 2);

            return new GUIContent(text.ReleaseToString());
        }

        /************************************************************************************************************************/

        public static string GetCurrentAssetPath(ProceduralAsset asset)
        {
            return AssetDatabase.GetAssetPath(asset.Injector.Asset);
        }

        /************************************************************************************************************************/

        private static List<ProceduralAsset> SortAssetsToGenerate()
        {
            // Add any assets that are dependant on the ones you are about to generate.
            for (int i = 0; i < ProceduralAsset.AllProceduralAssets.Count; i++)
            {
                RestartLoop:

                var asset = ProceduralAsset.AllProceduralAssets[i];
                if (AssetsToGenerate.Contains(asset))
                    continue;

                for (int j = 0; j < AssetsToGenerate.Count; j++)
                {
                    if (asset.IsDependantOn(AssetsToGenerate[j]))
                    {
                        AssetsToGenerate.Add(asset);
                        i = 0;
                        goto RestartLoop;
                    }
                }
            }

            // Sort all assets to be generated according to their dependancies.
            List<ProceduralAsset> assets;
            try
            {
                assets = WeaverUtilities.TopologicalSort(AssetsToGenerate);
            }
            catch (ArgumentException exception)// If you find a cyclic dependancy, log it and try to continue anyway.
            {
                Debug.LogException(exception);
                assets = WeaverUtilities.TopologicalSort(AssetsToGenerate, true);
            }

            AssetsToGenerate.Clear();

            return assets;
        }

        /************************************************************************************************************************/

        private void Update()
        {
            if (!_IsGenerating)
                return;

            var startTime = EditorApplication.timeSinceStartup;

            var success = GenerateAsset(_AssetsBeingGenerated[_CurrentAsset]);

            if (!AreNewDependanciesAlreadyGenerated())
            {
                RestartWithNewDependancies();
                return;
            }

            AppendGenerationTimeToCurrentLabel(EditorApplication.timeSinceStartup - startTime);

            // If you successfully generated the current asset, move onto the next.
            if (success)
            {
                _CurrentAsset++;

                // If you are near the bottom of the window, scroll down.
                if (_CurrentAsset * InternalGUI.ListStyle.lineHeight >
                    _ScrollPosition.y + position.height * 0.75f)
                {
                    _ScrollPosition.y += InternalGUI.ListStyle.lineHeight;
                }

                if (_CurrentAsset < _AssetsBeingGenerated.Count)
                {
                    Repaint();
                }
                else
                {
                    Finish();

                    if (AssetsToGenerate.Count > 0)
                    {
                        OnComplete += _OnComplete;
                        _FirstAssetLabel += _CurrentAsset + 1;
                        _CurrentAsset = 0;
                        WeaverEditorUtilities.ForceGenerate = false;
                        Initialize();
                    }
                    else if (!_IsShiftDown)
                    {
                        Close();
                    }
                }
            }
            else// Otherwise if an exception was thrown and logged, stay open and show the current asset as an error.
            {
                Finish();
                _CurrentItemStyle = InternalGUI.ErrorStyle;
            }
        }

        /************************************************************************************************************************/

        private static bool GenerateAsset(ProceduralAsset asset)
        {
            try
            {
                CurrentlyGenerating = asset;
                asset.GenerateAndSave();
                RemoveOldDependancies(asset);

                if (_CreatedTemporaryScene)
                {
                    foreach (var go in _TemporaryScene.GetRootGameObjects())
                    {
                        Debug.LogWarning(asset.GeneratorMethod.GetNameCS() + " left the following object unparented in the temporary scene: " + go +
                            "\nAny GameObjects you create while generating a procedural asset must be parented under the procedural asset itself or destroyed manually to avoid this message.");
                        DestroyImmediate(go);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                // If no dependancies were added, log and store the exception.
                if (AddedDependancies.Count == 0)
                    CatchException(exception);

                // Otherwise we're about to restart generation anyway to include the new dependancies.

                return false;
            }
            finally
            {
                CurrentlyGenerating = null;
            }
        }

        private static void CatchException(Exception exception)
        {
            OnEndGeneratorMethod();

            Debug.LogException(exception);

            _Exception = exception;
            while (_Exception.InnerException != null)
                _Exception = _Exception.InnerException;

            var stackTrace = new StackTrace(_Exception, true);
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var fileName = frame.GetFileName();
                if (fileName != null && fileName.StartsWith(Environment.CurrentDirectory))
                {
                    var start = Environment.CurrentDirectory.Length + 1;
                    fileName = fileName.Substring(start, fileName.Length - start);
                    _ExceptionSource = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fileName);
                    _ExceptionSourceLine = frame.GetFileLineNumber();
                    return;
                }
            }
        }

        /************************************************************************************************************************/

        private void Finish()
        {
            if (!_IsGenerating)
                return;

            _IsGenerating = false;
            _CurrentItemStyle = InternalGUI.DoneStyle;

            Labels.Add(new GUIContent("Total elapsed time: " + (EditorApplication.timeSinceStartup - _GenerationStartTime)));

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            CloseTemporaryScene();
            WeaverSettings.SetDirty();
            RefreshSelection();
        }

        /************************************************************************************************************************/

        private void OnDestroy()
        {
            CloseTemporaryScene();
            _Instance = null;
            WeaverEditorUtilities.ForceGenerate = false;
            WeaverSettings.SetDirty();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();

            _OnComplete?.Invoke(_CurrentAsset >= _AssetsBeingGenerated.Count && _CurrentItemStyle == InternalGUI.DoneStyle);
            _AssetsBeingGenerated = null;

            if (AssetsToGenerate.Count > 0)
                Generate();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Refreshes the selection if any of the generated assets are currently selected in the Project Window.
        /// This ensures that any previews are refreshed immediately.
        /// </summary>
        private static void RefreshSelection()
        {
            if (Selection.activeObject == null)
                return;

            // Check if any of the generated assets are selected.
            for (int i = 0; i < _AssetsBeingGenerated.Count; i++)
            {
                var asset = _AssetsBeingGenerated[i].Injector?.Asset;
                if (asset != null && Selection.Contains(asset))
                    goto GeneratedAssetIsSelected;
            }

            // If none are, do nothing.
            return;

            GeneratedAssetIsSelected:

            // Otherwise deselect and re-select the current selection.
            EditorApplication.delayCall += () =>
            {
                var selection = Selection.objects;
                Selection.objects = new UnityEngine.Object[0];
                EditorApplication.delayCall += () => Selection.objects = selection;
            };
        }

        /************************************************************************************************************************/

        public static bool IsCurrentlyGenerating(ProceduralAsset asset)
        {
            if (_AssetsBeingGenerated == null)
                return false;

            for (int i = 0; i < _AssetsBeingGenerated.Count; i++)
            {
                if (_AssetsBeingGenerated[i] == asset)
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/
        #region Temp Scene
        /************************************************************************************************************************/

        internal static void CreateTemporaryScene()
        {
            // Don't create a temporary scene if there already is one or if no assets need one.
            if (_CreatedTemporaryScene ||
                !AnyAssetWantsTempScene())
                return;

            // Don't create a temporary scene if there are any unsaved scenes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    return;
            }

            // Or if there is an unnamed scene.
            _TemporaryScene = SceneManager.GetSceneByName("");
            if (_TemporaryScene.IsValid())
                return;

            // Otherwise do make a temporary scene.
            _TemporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _CreatedTemporaryScene = true;
            SceneManager.SetActiveScene(_TemporaryScene);
        }

        /************************************************************************************************************************/

        private static bool AnyAssetWantsTempScene()
        {
            for (int i = 0; i < _AssetsBeingGenerated.Count; i++)
            {
                if (_AssetsBeingGenerated[i].UseTempScene())
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private static void CloseTemporaryScene()
        {
            if (_CreatedTemporaryScene)
            {
                EditorSceneManager.CloseScene(_TemporaryScene, true);
                _CreatedTemporaryScene = false;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Timing
        /************************************************************************************************************************/

        private static double _GeneratorMethodTime = 0;

        public static void OnBeginGeneratorMethod()
        {
            _GeneratorMethodTime = EditorApplication.timeSinceStartup;
        }

        public static void OnEndGeneratorMethod()
        {
            _GeneratorMethodTime = EditorApplication.timeSinceStartup - _GeneratorMethodTime;
        }

        private void AppendGenerationTimeToCurrentLabel(double time)
        {
            var label = Labels[_FirstAssetLabel + _CurrentAsset];

            label.text = $"{label.text}: {((int)(time * 1000))}ms ({((int)(_GeneratorMethodTime * 1000))})";
            _GeneratorMethodTime = 0;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Dependancies
        /************************************************************************************************************************/

        private static readonly List<ProceduralAsset>
            AddedDependancies = new List<ProceduralAsset>();
        private static readonly List<ProceduralAsset>
            CheckedDependancies = new List<ProceduralAsset>();

        /************************************************************************************************************************/

        /// <summary>
        /// Verifies that the `dependancy` has been generated before the current asset.
        /// </summary>
        public static void VerifyAssetDependancy(ProceduralAsset dependancy)
        {
            if (CurrentlyGenerating == null ||
                dependancy == CurrentlyGenerating)
                return;

            CheckedDependancies.Add(dependancy);

            // If the current asset isn't dependant on the specified asset, add it as a dependancy.
            if (!CurrentlyGenerating.SavedProceduralData.IsDependantOn(dependancy))
            {
                CurrentlyGenerating.SavedProceduralData.AddDependancy(dependancy);
                AddedDependancies.Add(dependancy);
                Debug.Log(string.Concat(
                    "New procedural asset dependancy detected: ", CurrentlyGenerating.GeneratorMethod.GetNameCS(),
                    " depends on ", dependancy.GeneratorMethod.GetNameCS()));
            }
            // Otherwise if the dependancy isn't being generated, throw an exception.
            else if (!IsCurrentlyGenerating(dependancy))
            {
                throw new InvalidOperationException(
                    string.Concat(CurrentlyGenerating.GeneratorMethod.GetNameCS(), " tried to access ", dependancy.GeneratorMethod.GetNameCS(),
                    ", but doesn't have it listed as a dependancy. This should never happen."));
            }
        }

        /************************************************************************************************************************/

        private bool AreNewDependanciesAlreadyGenerated()
        {
            if (AddedDependancies.Count == 0) return true;

            for (int i = 0; i < AddedDependancies.Count; i++)
            {
                var addedDependancy = AddedDependancies[i];
                for (int j = 0; j < _CurrentAsset; j++)
                {
                    if (_AssetsBeingGenerated[j] == addedDependancy)
                        goto NextDependancy;
                }

                return false;

                NextDependancy:
                continue;
            }

            return true;
        }

        /************************************************************************************************************************/

        private void RestartWithNewDependancies()
        {
#if WEAVER_DEBUG
            Debug.Log("Regenerating to include new dependancies.");
#endif

            _FirstAssetLabel += _CurrentAsset + 1;
            Labels.RemoveRange(_FirstAssetLabel, Labels.Count - _FirstAssetLabel);
            Labels.Add(new GUIContent("Regenerating to include new dependancies."));
            _FirstAssetLabel++;

            AssetsToGenerate.AddRange(AddedDependancies);
            AddedDependancies.Clear();

            for (int i = _CurrentAsset; i < _AssetsBeingGenerated.Count; i++)
                AssetsToGenerate.Add(_AssetsBeingGenerated[i]);

            OnComplete = _OnComplete;

            _CurrentAsset = 0;
            Initialize();
        }

        /************************************************************************************************************************/

        private static void RemoveOldDependancies(ProceduralAsset asset)
        {
            if (asset.Dependancies == null)
                return;

            for (int i = asset.SavedProceduralData.Dependancies.Count - 1; i >= 0; i--)
            {
                var dependancy = asset.SavedProceduralData.Dependancies[i];
                if (!CheckedDependancies.Contains(dependancy))
                {
                    asset.SavedProceduralData.RemoveDependancy(i);
                    if (dependancy != null && dependancy.GeneratorMethod != null)
                    {
                        Debug.Log(string.Concat(
                            "Old procedural asset dependancy removed: ", asset.GeneratorMethod.GetNameCS(),
                            " no longer depends on ", dependancy.GeneratorMethod.GetNameCS()));
                    }
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        private void OnGUI()
        {
            _IsShiftDown = Event.current.shift;

            if (Event.current.type == EventType.KeyUp)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Finish();

                    if (!_IsShiftDown)
                        Close();
                }
            }

            DrawGenerationList();
        }

        /************************************************************************************************************************/

        private void DrawGenerationList()
        {
            if (Labels == null)
                return;

            _ScrollPosition = GUILayout.BeginScrollView(_ScrollPosition);

            if (_FirstAssetLabel + _CurrentAsset < Labels.Count)
            {
                var i = 0;
                for (; i < _FirstAssetLabel + _CurrentAsset; i++)
                {
                    GUILayout.Label(Labels[i], InternalGUI.DoneStyle);
                }

                GUILayout.Label(Labels[i++], _CurrentItemStyle);

                DrawException();

                for (; i < Labels.Count; i++)
                {
                    GUILayout.Label(Labels[i], InternalGUI.ListStyle);
                }
            }
            else
            {
                for (int i = 0; i < Labels.Count; i++)
                {
                    GUILayout.Label(Labels[i], InternalGUI.DoneStyle);
                }
            }

            GUILayout.EndScrollView();
        }

        /************************************************************************************************************************/

        private void DrawException()
        {
            if (_Exception == null)
                return;

            GUILayout.BeginHorizontal();

            var dontExpandWidth = GUILayout.ExpandWidth(false);

            if (GUILayout.Button("Retry", dontExpandWidth))
            {
                _CurrentAsset = 0;
                _Exception = null;
                Labels.Clear();
                InitializeLabels();
                CreateTemporaryScene();
                _IsGenerating = true;
                return;
            }

            if (GUILayout.Button("Skip", dontExpandWidth))
            {
                if (_CurrentAsset + 1 < _AssetsBeingGenerated.Count)
                {
                    var label = Labels[_FirstAssetLabel + _CurrentAsset];
                    label.text = $"<color=#{WeaverUtilities.ColorToHex(InternalGUI.ErrorStyle.normal.textColor)}" +
                        $">{label.text}</color>";

                    _CurrentAsset++;
                    _Exception = null;
                    _CurrentItemStyle = InternalGUI.DoingStyle;
                    CreateTemporaryScene();
                    _IsGenerating = true;
                }
                else
                {
                    Close();
                }

                return;
            }

            if (GUILayout.Button("Abort", dontExpandWidth))
            {
                Close();
            }

            GUILayout.Space(10);

            if (_ExceptionSource != null &&
                GUILayout.Button("Open Source", dontExpandWidth))
            {
                EditorApplication.delayCall += () =>
                {
                    Close();
                    AssetDatabase.OpenAsset(_ExceptionSource, _ExceptionSourceLine);
                };
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(_Exception.ToString(), InternalGUI.ErrorStyle);
        }

        /************************************************************************************************************************/

        private static class InternalGUI
        {
            /************************************************************************************************************************/

            public static readonly GUIStyle
                ListStyle, DoingStyle, DoneStyle, ErrorStyle;

            /************************************************************************************************************************/

            static InternalGUI()
            {
                ListStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true
                };

                DoingStyle = new GUIStyle(ListStyle);
                DoneStyle = new GUIStyle(ListStyle);
                ErrorStyle = new GUIStyle(ListStyle);

                // If labels normally have dark text, use dark colors.
                if (ListStyle.normal.textColor.r + ListStyle.normal.textColor.g + ListStyle.normal.textColor.b < 1.5f)
                {
                    DoingStyle.normal.textColor = new Color(0.6f, 0.6f, 0);
                    DoneStyle.normal.textColor = new Color(0, 0.5f, 0);
                    ErrorStyle.normal.textColor = new Color(0.5f, 0, 0);
                }
                else// Otherwise use light colors.
                {
                    DoingStyle.normal.textColor = new Color(1, 1, 0.25f);
                    DoneStyle.normal.textColor = new Color(0.25f, 1, 0.25f);
                    ErrorStyle.normal.textColor = new Color(1, 0.1f, 0.1f);
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

