// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Weaver
{
    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>The conditional compilation symbol used in the Unity Editor.</summary>
        public const string UnityEditor = "UNITY_EDITOR";

        /************************************************************************************************************************/

        /// <summary>The URL of the Weaver documentation.</summary>
        public const string DocumentationURL = "https://kybernetik.com.au/weaver";

        /// <summary>The URL of the Weaver thread on the Unity Forum.</summary>
        public const string ForumURL = "https://forum.unity.com/threads/592459";

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The email address to contact for anything regarding Weaver.</summary>
        public const string DeveloperEmail = "mail@kybernetik.com.au";

        /// <summary>[Editor-Only] The menu path of the Weaver Window.</summary>
        public const string WeaverWindowPath = "Window/General/Weaver";
#endif

        /************************************************************************************************************************/
        #region Version Details
        /************************************************************************************************************************/

        /// <summary>The Asset Store URL of Weaver Pro.</summary>
        internal const string AssetStoreProURL = "https://assetstore.unity.com/packages/tools/utilities/weaver-pro-60304?aid=1100l8ah5";

        /// <summary>This is Weaver Pro.</summary>
        public static readonly bool IsWeaverPro = true;

        /// <summary>This is "Weaver Pro v6.2".</summary>
        public static readonly string Version = "Weaver Pro v6.2";

#if UNITY_EDITOR
        internal static void OpenCurrentVersionInAssetStore() => Application.OpenURL(AssetStoreProURL);
#endif

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Returns a string containing the value of each element in `collection`.</summary>
        public static string DeepToString(this IEnumerable collection, string separator)
        {
            if (collection == null)
                return "null";
            else
                return DeepToString(collection.GetEnumerator(), separator);
        }

        /// <summary>Returns a string containing the value of each element in `collection` (each on a new line).</summary>
        public static string DeepToString(this IEnumerable collection) => DeepToString(collection, NewLine);

        /************************************************************************************************************************/

        /// <summary>Each element returned by `enumerator` is appended to `text`.</summary>
        public static void AppendDeepToString(StringBuilder text, IEnumerator enumerator, string separator)
        {
            text.Append("[]");
            var countIndex = text.Length - 1;
            var count = 0;

            while (enumerator.MoveNext())
            {
                text.Append(separator);
                text.Append('[');
                text.Append(count);
                text.Append("] = ");
                text.Append(enumerator.Current);

                count++;
            }

            text.Insert(countIndex, count);
        }

        /// <summary>Returns a string containing the value of each element in `enumerator`.</summary>
        public static string DeepToString(this IEnumerator enumerator, string separator)
        {
            var text = GetStringBuilder();
            AppendDeepToString(text, enumerator, separator);
            return text.ReleaseToString();
        }

        /// <summary>Returns a string containing the value of each element in `enumerator` (each on a new line).</summary>
        public static string DeepToString(this IEnumerator enumerator) => DeepToString(enumerator, NewLine);

        /************************************************************************************************************************/

        /// <summary>Returns the index of the last forward slash or back slash.</summary>
        public static int IndexOfLastSlash(string str) => Mathf.Max(str.LastIndexOf('/'), str.LastIndexOf('\\'));

        /************************************************************************************************************************/

        /// <summary>Replaces back slashes with forward slashes (\ -> /).</summary>
        public static string ReplaceSlashesForward(this string str) => str.Replace('\\', '/');

        /// <summary>Replaces forward slashes with back slashes (/ -> \).</summary>
        public static string ReplaceSlashesBack(this string str) => str.Replace('/', '\\');

        /************************************************************************************************************************/

        /// <summary>Returns `str` with any forward slashes removed from the end.</summary>
        public static string RemoveTrailingSlashes(this string str)
        {
            var end = str.Length - 1;
            while (str[end] == '/')
            {
                end--;
            }

            return str.Substring(0, end + 1);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Increments `index` until `str[index]` is no longer a whitespace character.
        /// </summary>
        public static void SkipWhiteSpace(string str, ref int index)
        {
            for (; index < str.Length; index++)
            {
                if (!char.IsWhiteSpace(str[index]))
                    break;
            }
        }

        /// <summary>
        /// Decrements `index` until `str[index]` is no longer a whitespace character.
        /// </summary>
        public static void SkipWhiteSpaceBackwards(string str, ref int index)
        {
            for (; index >= 0; index--)
            {
                if (!char.IsWhiteSpace(str[index]))
                    break;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the index of the '.' at the start of the file extension of `path` (or -1 if it has no file extension).
        /// </summary>
        public static int GetFileExtensionIndex(string path)
        {
            var lastSlash = path.LastIndexOf('/') + 1;

            return path.LastIndexOf('.', path.Length - 1, path.Length - lastSlash);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Compares two strings to sort files before sub folders.
        /// <para></para>Note: this method only interprets forward slashes.
        /// </summary>
        public static int CompareWithFilesBeforeFolders(string x, string y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null) return 1;

            var lastSlashx = x.LastIndexOf('/') - 1;
            var lastSlashy = y.LastIndexOf('/') - 1;

            if (lastSlashx == lastSlashy)
                return x.CompareTo(y);

            var end = lastSlashx;
            if (end > lastSlashy) end = lastSlashy;
            if (end > x.Length) end = x.Length;
            if (end > y.Length) end = y.Length;

            for (int i = 0; i < end; i++)
            {
                var a = x[i];
                var b = y[i];
                var comparison = a.CompareTo(b);
                if (comparison != 0) return comparison;
            }

            return lastSlashx.CompareTo(lastSlashy);
        }

        // Windows file sorting function.
        //public static int StrCmpLogicalW(string x, string y)
        //{
        //    if (x != null && y != null)
        //    {
        //        int xIndex = 0;
        //        int yIndex = 0;

        //        while (xIndex < x.Length)
        //        {
        //            if (yIndex >= y.Length)
        //                return 1;

        //            if (char.IsDigit(x[xIndex]))
        //            {
        //                if (!char.IsDigit(y[yIndex]))
        //                    return -1;

        //                // Compare the numbers.
        //                List<char> xText = new List<char>();
        //                List<char> yText = new List<char>();

        //                for (int i = xIndex; i < x.Length; i++)
        //                {
        //                    var xChar = x[i];

        //                    if (char.IsDigit(xChar))
        //                        xText.Add(xChar);
        //                    else
        //                        break;
        //                }

        //                for (int j = yIndex; j < y.Length; j++)
        //                {
        //                    var yChar = y[j];

        //                    if (char.IsDigit(yChar))
        //                        yText.Add(yChar);
        //                    else
        //                        break;
        //                }

        //                int xValue = Convert.ToInt32(new string(xText.ToArray()));
        //                int yValue = Convert.ToInt32(new string(yText.ToArray()));

        //                if (xValue < yValue)
        //                    return -1;
        //                else if (xValue > yValue)
        //                    return 1;

        //                // Skip.
        //                xIndex += xText.Count;
        //                yIndex += yText.Count;
        //            }
        //            else if (char.IsDigit(y[yIndex]))
        //                return 1;
        //            else
        //            {
        //                int difference = char.ToUpperInvariant(x[xIndex]).CompareTo(char.ToUpperInvariant(y[yIndex]));
        //                if (difference > 0)
        //                    return 1;
        //                else if (difference < 0)
        //                    return -1;

        //                xIndex++;
        //                yIndex++;
        //            }
        //        }

        //        if (yIndex < y.Length)
        //            return -1;
        //    }

        //    return 0;
        //}

        /************************************************************************************************************************/

        /// <summary>Returns 'T'.`obj.ToString()`. Useful for enums.</summary>
        public static string FriendlyFullName<T>(T obj)
        {
            var text = GetStringBuilder();
            AppendFriendlyFullName(text, obj);
            return text.ReleaseToString();
        }

        /// <summary>Appends 'Type'.`obj.ToString()`. Useful for enums.</summary>
        public static void AppendFriendlyFullName<T>(StringBuilder text, T obj)
        {
            text.Append(typeof(T).GetNameCS());

            if (obj == null)
                text.Append(".null");
            else
                text.Append('.').Append(obj);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds spaces to `camelCase` before each uppercase letter.
        /// </summary>
        public static string ConvertCamelCaseToFriendly(string camelCase, bool uppercaseFirst = false)
        {
            var text = WeaverUtilities.GetStringBuilder();
            ConvertCamelCaseToFriendly(text, camelCase, 0, camelCase.Length, uppercaseFirst);
            return text.ReleaseToString();
        }

        /// <summary>
        /// Adds spaces to `camelCase` before each uppercase letter.
        /// </summary>
        public static void ConvertCamelCaseToFriendly(StringBuilder text, string camelCase, int start, int end, bool uppercaseFirst = false)
        {
            text.Append(uppercaseFirst ?
                char.ToUpper(camelCase[start]) :
                camelCase[start]);

            start++;

            for (; start < end; start++)
            {
                var character = camelCase[start];
                if (char.IsUpper(character))// Space before upper case.
                {
                    text.Append(' ');
                    text.Append(character);

                    // No spaces between consecutive upper case, unless followed by a non-upper case.
                    start++;
                    if (start >= camelCase.Length) return;
                    else
                    {
                        character = camelCase[start];
                        if (!char.IsUpper(character))
                        {
                            start--;
                        }
                        else
                        {
                            char nextCharacter;
                            while (true)
                            {
                                start++;
                                if (start >= camelCase.Length)
                                {
                                    text.Append(character);
                                    return;
                                }
                                else
                                {
                                    nextCharacter = camelCase[start];

                                    if (char.IsUpper(nextCharacter))
                                    {
                                        text.Append(character);
                                    }
                                    else
                                    {
                                        text.Append(' ');
                                        text.Append(character);
                                        text.Append(nextCharacter);
                                        break;
                                    }

                                    character = nextCharacter;
                                }
                            }
                        }
                    }
                }
                else if (char.IsNumber(character))// Space before number.
                {
                    text.Append(' ');
                    text.Append(character);
                    while (true)
                    {
                        start++;
                        if (start >= camelCase.Length) return;
                        else
                        {
                            character = camelCase[start];

                            if (char.IsNumber(character))
                                text.Append(character);
                            else
                            {
                                start--;
                                break;
                            }
                        }
                    }
                }
                else text.Append(character);// Otherwise just append the character.
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds spaces to `camelCase` before each uppercase letter and removes any underscores from the start.
        /// </summary>
        public static string ConvertFieldNameToFriendly(string fieldName, bool uppercaseFirst = false)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "";

            var text = WeaverUtilities.GetStringBuilder();

            var start = 0;
            while (fieldName[start] == '_')
            {
                start++;
                if (start >= fieldName.Length)
                    return fieldName;
            }

            ConvertCamelCaseToFriendly(text, fieldName, start, fieldName.Length, uppercaseFirst);
            return text.ReleaseToString();
        }

        /************************************************************************************************************************/

        /// <summary>Appends the specified string sanitized for XML.</summary>
        public static void AppendXmlString(StringBuilder text, string str)
        {
            if (str == null)
                return;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                switch (c)
                {
                    case '<':
                        text.Append("&lt;");
                        break;
                    case '>':
                        text.Append("&gt;");
                        break;
                    default:
                        text.Append(c);
                        break;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Inserts rich text bold tags around the last word in `text`.
        /// The following characters denote the start of a section: dot, slash, tab, new line.
        /// </summary>
        public static void ApplyBoldTagsToLastSection(StringBuilder text, int skipSections = 0, int sectionCount = 1)
        {
            var sectionEnd = text.Length;
            int i;
            for (i = sectionEnd - 2; i >= 0; i--)
            {
                switch (text[i])
                {
                    case '.':
                    case '/':
                    case '\\':
                    case '\t':
                    case '\r':
                    case '\n':
                        if (skipSections == sectionCount)
                            sectionEnd = i;

                        if (--skipSections < 0)
                            goto FoundSectionStart;
                        else
                            break;

                    default:
                        break;
                }
            }

            FoundSectionStart:
            text.Insert(i + 1, "<B>");
            text.Insert(sectionEnd + 3, "</B>");
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string containing the hexadecimal representation of `color`.</summary>
        public static string ColorToHex(Color32 color)
        {
            var text = GetStringBuilder();
            AppendColorToHex(text, color);
            return text.ReleaseToString();
        }

        /// <summary>Appends the hexadecimal representation of `color`.</summary>
        public static void AppendColorToHex(StringBuilder text, Color32 color)
        {
            text.Append(color.r.ToString("X2"));
            text.Append(color.g.ToString("X2"));
            text.Append(color.b.ToString("X2"));
            text.Append(color.a.ToString("X2"));
        }

        /************************************************************************************************************************/

        /// <summary>Appends the a rich text color tag around `message`.</summary>
        public static void AppendColorTag(StringBuilder text, Color32 color, string message)
        {
            text.Append("<color=#");
            AppendColorToHex(text, color);
            text.Append('>');
            text.Append(message);
            text.Append("</color>");
        }

        /************************************************************************************************************************/
        #region String Building
        /************************************************************************************************************************/

        /// <summary>
        /// 4 spaces.
        /// <para></para>
        /// Could be '\t', but this makes it easier to copy code into websites like Stack Overflow which use 4 spaces for tabs.
        /// </summary>
        public const string Tab = "    ";

        /// <summary>Appends <see cref="Tab"/> the specified number of times.</summary>
        public static StringBuilder Indent(this StringBuilder text, int indent)
        {
            Start:

            switch (indent)
            {
                case 0: break;
                case 1: text.Append(Tab); break;
                case 2: text.Append(Tab + Tab); break;
                case 3: text.Append(Tab + Tab + Tab); break;
                default:
                    text.Append(Tab + Tab + Tab + Tab);
                    if (indent > 4)
                    {
                        indent -= 4;
                        goto Start;
                    }
                    else break;
            }

            return text;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Unity's profiler claims that each call to <see cref="Environment.NewLine"/> allocates 30 bytes of garbage
        /// so we cache the value here for AppendLineConst to use.
        /// </summary>
        public static readonly string NewLine = Environment.NewLine;

        /// <summary>
        /// This method allocates no garbage unlike <see cref="StringBuilder.AppendLine()"/> which allocates 30 bytes
        /// of garbage per call for accessing <see cref="Environment.NewLine"/> (according to Unity's profiler).
        /// </summary>
        public static StringBuilder AppendLineConst(this StringBuilder text) => text.Append(NewLine);

        /// <summary>
        /// This method allocates no garbage unlike <see cref="StringBuilder.AppendLine()"/> which allocates 30 bytes
        /// of garbage per call for accessing <see cref="Environment.NewLine"/> (according to Unity's profiler).
        /// </summary>
        public static StringBuilder AppendLineConst(this StringBuilder text, string value) => text.Append(value).Append(NewLine);

        /// <summary>
        /// This method allocates no garbage unlike <see cref="StringBuilder.AppendLine()"/> which allocates 30 bytes
        /// of garbage per call for accessing <see cref="Environment.NewLine"/> (according to Unity's profiler).
        /// </summary>
        public static StringBuilder AppendLineConst(this StringBuilder text, object value) => text.Append(value).Append(NewLine);

        /************************************************************************************************************************/
        #region String Builder Pool
        /************************************************************************************************************************/

#if UNITY_EDITOR
        private static readonly List<StringBuilder> StringBuilderPool = new List<StringBuilder>();
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// Gets an available <see cref="StringBuilder"/> from the pool in the Unity Editor but simply returns a new
        /// one at runtime.
        /// <para></para>
        /// Once you are done with it, give it back with <see cref="Release"/> or <seealso cref="ReleaseToString"/>
        /// </summary>
        public static StringBuilder GetStringBuilder()
        {
#if UNITY_EDITOR
            if (StringBuilderPool.Count > 0)
            {
                var builder = StringBuilderPool.Pop();

#if UNITY_ASSERTIONS
                if (builder.Length != 0)
                    Debug.LogError(
                        $"Retrieved a {nameof(StringBuilder)} from the pool with a non-zero Length. " +
                        $"You must not use a {nameof(StringBuilder)} after releasing it to the pool.\n{builder}");
#endif

                builder.Length = 0;
                return builder;
            }
#endif

            return new StringBuilder();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// Gives a string builder back to the pool.
        /// Use <see cref="ReleaseToString"/> if you also need its string.
        /// </summary>
        [System.Diagnostics.Conditional(UnityEditor)]
        public static void Release(this StringBuilder builder)
        {
            builder.Length = 0;
#if UNITY_EDITOR
            StringBuilderPool.Add(builder);
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gives a string builder to the pool and returns its string.
        /// Use <see cref="Release"/> if you don't need its string.
        /// </summary>
        public static string ReleaseToString(this StringBuilder builder)
        {
            var output = builder.ToString();
#if UNITY_EDITOR
            builder.Release();
#endif
            return output;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

