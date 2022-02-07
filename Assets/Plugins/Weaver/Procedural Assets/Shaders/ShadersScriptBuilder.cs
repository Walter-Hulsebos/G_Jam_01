// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the properties and symbols in a set of
    /// shaders chosen in the <see cref="Window.ShadersPanel"/>.
    /// </summary>
    public sealed class ShadersScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ShadersScriptBuilder"/>.</summary>
        public ShadersScriptBuilder(Action<StringBuilder> generatorMethod) : base(generatorMethod) { }

        /************************************************************************************************************************/

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => WeaverSettings.Shaders.enabled;

        /************************************************************************************************************************/

        /// <summary>Gathers the element details of this script.</summary>
        protected override void GatherScriptDetails()
        {
            if (WeaverSettings.Shaders.shaders == null)
                return;

            for (int i = 0; i < WeaverSettings.Shaders.shaders.Count; i++)
            {
                var shader = WeaverSettings.Shaders.shaders[i];
                if (shader == null)
                    return;

                var typeName = new Substring(shader.name);
                var type = RootType.GetOrAddNestedTypesForDirectories(typeName);
                type = type.AddNestedType(typeName);
                type.CommentBuilder = (text) => text.Append(shader.name);

                var nameField = type.AddField("Name", shader.name);
                nameField.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                nameField.CommentBuilder = (text) =>
                    text.Append("Shader Name (for use with <see cref=\"UnityEngine.Shader.Find(string)\"/>)");

                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int j = 0; j < propertyCount; j++)
                {
                    var propertyIndex = j;
                    var propertyName = ShaderUtil.GetPropertyName(shader, propertyIndex);

                    var field = type.AddField(propertyName, 0);
                    field.Modifiers = AccessModifiers.Public | AccessModifiers.Static | AccessModifiers.Readonly;
                    field.AppendInitializer = (text, indent, value) =>
                        text.Append(" = UnityEngine.Shader.PropertyToID(\"").Append(propertyName).Append("\")");
                    field.CommentBuilder = (text) => AppendDescription(text, shader, propertyIndex);
                    field.ValueEquals = null;
                }

                GatherKeywords(shader, type);
            }
        }

        /************************************************************************************************************************/

        private static void AppendDescription(StringBuilder text, Shader shader, int propertyIndex)
        {
            text.Append(ShaderUtil.GetPropertyDescription(shader, propertyIndex));

            text.Append(" [");
            var type = ShaderUtil.GetPropertyType(shader, propertyIndex);
            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Range:
                    text.Append("Range(Default=");
                    text.Append(ShaderUtil.GetRangeLimits(shader, propertyIndex, 0));
                    text.Append(", Min=");
                    text.Append(ShaderUtil.GetRangeLimits(shader, propertyIndex, 1));
                    text.Append(", Max=");
                    text.Append(ShaderUtil.GetRangeLimits(shader, propertyIndex, 2));
                    text.Append(")");
                    break;

                case ShaderUtil.ShaderPropertyType.TexEnv:
                    switch (ShaderUtil.GetTexDim(shader, propertyIndex))
                    {
                        case UnityEngine.Rendering.TextureDimension.Tex2D: text.Append("2D Texture"); break;
                        case UnityEngine.Rendering.TextureDimension.Tex3D: text.Append("3D Texture"); break;
                        case UnityEngine.Rendering.TextureDimension.Cube: text.Append("Cube Texture"); break;
                        case UnityEngine.Rendering.TextureDimension.Tex2DArray: text.Append("2D Texture Array"); break;
                        case UnityEngine.Rendering.TextureDimension.CubeArray: text.Append("Cube Texture Array"); break;
                        case UnityEngine.Rendering.TextureDimension.Unknown:
                        case UnityEngine.Rendering.TextureDimension.None:
                        case UnityEngine.Rendering.TextureDimension.Any:
                        default: text.Append("Texture"); break;
                    }
                    break;

                case ShaderUtil.ShaderPropertyType.Color:
                case ShaderUtil.ShaderPropertyType.Vector:
                case ShaderUtil.ShaderPropertyType.Float:
                default:
                    text.Append(type);
                    break;
            }
            text.Append(']');
        }

        /************************************************************************************************************************/

        private static HashSet<string> _Keywords;

        private static void GatherKeywords(Shader shader, TypeBuilder declaringType)
        {
            // There doesn't seem to be a proper way of getting the keywords used by a shader.
            // So we need to parse the file's text to identify keywords used in #ifs. :(

            var assetPath = AssetDatabase.GetAssetPath(shader);
            if (!File.Exists(assetPath))
                return;

            using (var stream = new FileStream(assetPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                if (_Keywords == null)
                    _Keywords = new HashSet<string>();
                else
                    _Keywords.Clear();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var start = line.IndexOf("#pragma");
                    if (start < 0)
                        continue;

                    start += 7;
                    start = IndexOfNextSymbol(line, start);

                    if (string.Compare(line, start, "multi_compile", 0, 13) != 0 &&
                        string.Compare(line, start, "shader_feature", 0, 14) != 0)
                        continue;

                    start += 13;
                    while (true)
                    {
                        start = IndexOfNextSymbol(line, start);
                        if (start < 0)
                            break;

                        var end = IndexOfSymbolEnd(line, start);

                        var keyword = line.Substring(start, end - start);
                        if (!_Keywords.Contains(keyword))
                        {
                            _Keywords.Add(keyword);

                            var field = declaringType.AddField(keyword, keyword);
                            field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                            field.CommentBuilder = (text) => text.Append("Keyword: ").Append(keyword);
                        }

                        start = end;
                    }
                }
            }
        }

        /************************************************************************************************************************/

        private static int IndexOfNextSymbol(string str, int start)
        {
            while (++start < str.Length)
            {
                switch (str[start])
                {
                    case ' ':
                    case '\t':
                        break;
                    default:
                        return start;
                }
            }

            return -1;
        }

        /************************************************************************************************************************/

        private static int IndexOfSymbolEnd(string str, int start)
        {
            while (++start < str.Length)
            {
                if (!CSharpProcedural.IsValidInMemberName(str[start]))
                    break;
            }

            return start;
        }

        /************************************************************************************************************************/
    }
}

#endif

