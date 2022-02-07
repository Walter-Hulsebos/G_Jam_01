// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Weaver.Editor.Procedural.Scripting;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Information about a type deriving from <see cref="AssetListBase"/>.
    /// </summary>
    internal sealed class AssetListTypeInfo
    {
        /************************************************************************************************************************/

        public readonly Type ListType;
        public readonly Type AssetType;
        public readonly MonoScript Script;

        /************************************************************************************************************************/

        private AssetListTypeInfo(Type listType)
        {
            ListType = listType;

            Script = UnityScripts.GetScript(listType);

            var arguments = ReflectionUtilities.GetGenericInterfaceArguments(listType, typeof(IAssetList<>));
            if (arguments != null)
                AssetType = arguments[0];
        }

        /************************************************************************************************************************/

        private static List<AssetListTypeInfo> _ListTypes;

        public static List<AssetListTypeInfo> ListTypes
        {
            get
            {
                if (_ListTypes == null)
                {
                    var types = typeof(AssetListBase).GetDerivedTypes();

                    for (int i = types.Count - 1; i >= 0; i--)
                    {
                        var type = types[i];
                        if (type.IsGenericType)
                            types.RemoveAt(i);
                    }

                    types.Sort((a, b) => a.FullName.CompareTo(b.FullName));

                    _ListTypes = new List<AssetListTypeInfo>(types.Count);
                    for (int i = 0; i < types.Count; i++)
                    {
                        _ListTypes.Add(new AssetListTypeInfo(types[i]));
                    }
                    types.Release();
                }

                return _ListTypes;
            }
        }

        /************************************************************************************************************************/

        public void DoScriptGUI(Rect rect, ref bool isUsed)
        {
            if (Script != null)
            {
                if (!isUsed)
                {
                    // Only allow scripts to be removed, not precompiled DLLs.
                    if (!ShouldAllowDestroyScript(Script, out var assetPath))
                    {
                        isUsed = true;
                    }
                    else
                    {
                        var left = rect.xMin;
                        rect.xMin = rect.xMax - GUIStyles.RemoveButtonWidth;
                        var content = GUIStyles.GetTempRemoveButton("Remove this unused asset list type");
                        if (GUI.Button(rect, content, GUIStyles.RemoveButtonStyle))
                        {
                            var message = assetPath + "\n\nYou cannot undo this action, though you can just generate the script again if needed.";

                            if (EditorUtility.DisplayDialog("Delete Script?", message, "Delete", "Cancel"))
                                AssetDatabase.DeleteAsset(assetPath);
                        }
                        rect.xMin = left;
                        rect.width -= GUIStyles.RemoveButtonWidth;
                    }
                }

                {
                    var content = EditorGUIUtility.ObjectContent(Script, typeof(MonoScript));

                    if (GUI.Button(rect, content, GUIStyles.FakeObjectFieldStyle))
                        Selection.activeObject = Script;
                }
            }
            else
            {
                GUI.Label(rect, ListType.GetNameCS());
            }
        }

        /************************************************************************************************************************/

        private static bool ShouldAllowDestroyScript(MonoScript script, out string assetPath)
        {
            if (script.name == "ObjectList")
            {
                assetPath = null;
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(script);
            return AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath) == script;
        }

        /************************************************************************************************************************/

        public static bool ShouldAllowDestroyScript(Type type)
        {
            var script = UnityScripts.GetScript(type);
            return
                script != null &&
                ShouldAllowDestroyScript(script, out _);
        }

        /************************************************************************************************************************/

        public static bool ScriptExists(string listTypeName)
        {
            var path = string.Concat(ProceduralAssetSettings.OutputDirectory, listTypeName, ".cs");
            return File.Exists(path);
        }

        /************************************************************************************************************************/

        public static string GenerateScript(string listTypeName, bool editorOnly, Type baseType)
        {
            var script = WeaverUtilities.GetStringBuilder()
                .AppendLineConst("// This file was procedurally generated by Weaver.")
                .AppendLineConst();

            if (editorOnly)
                script.AppendLineConst("#if " + WeaverUtilities.UnityEditor);

            var indent = 0;
            if (ProceduralAssetSettings.Instance.scriptsUseWeaverNamespace)
            {
                script.AppendLineConst("namespace " + nameof(Weaver));
                script.OpenScope(ref indent);
                script.Indent(indent);
            }

            script.Append("public sealed class ")
                .Append(listTypeName)
                .Append(" : ")
                .Append(baseType.GetNameCS())
                .AppendLineConst(" { }");

            if (ProceduralAssetSettings.Instance.scriptsUseWeaverNamespace)
                script.AppendLineConst("}");

            if (editorOnly)
                script.AppendLineConst("#endif");

            var path = ProceduralAssetSettings.EnsureOutputDirectoryExists();
            path += listTypeName + ".cs";

            File.WriteAllText(path, script.ReleaseToString());
            AssetDatabase.ImportAsset(path);
            Debug.Log("Generated Procedural Script: " + path, AssetDatabase.LoadAssetAtPath<MonoScript>(path));

            return path;
        }

        /************************************************************************************************************************/
    }
}

#endif

