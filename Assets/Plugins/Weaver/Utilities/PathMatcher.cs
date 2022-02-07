// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only]
    /// A utility for finding file paths that match certain targets as closely as possible.
    /// </summary>
    public class PathMatcher
    {
        /************************************************************************************************************************/

        /// <summary>The paths this matcher is trying to find a match for.</summary>
        public readonly IList<string> TargetPaths;

        /// <summary>The best match that has been found so far.</summary>
        public string MatchedPath { get; private set; }

        /// <summary>The index in the <see cref="TargetPaths"/> of the <see cref="MatchedPath"/>.</summary>
        public int MatchedTargetIndex { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="PathMatcher"/> that tries to find paths as close to any of the specified
        /// `targetPaths` as possible, with paths earliest in ths list getting the highest priority.
        /// </summary>
        public PathMatcher(IList<string> targetPaths)
        {
            TargetPaths = targetPaths;

            for (int i = 0; i < targetPaths.Count; i++)
                targetPaths[i] = targetPaths[i].ToLower();

            Reset();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Clears the <see cref="MatchedPath"/> and <see cref="MatchedTargetIndex"/>.
        /// </summary>
        public void Reset()
        {
            MatchedPath = null;
            MatchedTargetIndex = TargetPaths.Count - 1;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the specified `path` to see if it's a better match than the current <see cref="MatchedPath"/> this
        /// method sets it as the <see cref="MatchedPath"/> and returns true.
        /// </summary>
        public virtual bool TryMatchFile(string path)
        {
            var withoutExtension = WeaverEditorUtilities.GetPathWithoutExtension(path);
            if (TryMatch(withoutExtension, path))
                return true;

            if (withoutExtension.Length != path.Length && TryMatch(path, path))
                return true;

            return false;
        }

        /************************************************************************************************************************/

        private bool TryMatch(string path, string rawPath)
        {
            for (int i = 0; i < MatchedTargetIndex + 1; i++)
            {
                if (TryMatchEnd(TargetPaths[i], path, rawPath))
                {
                    MatchedTargetIndex = i;
                    return true;
                }
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the end of the `path` matches the `targetEnd` and outputs a score denoting how well
        /// any separators matched.
        /// </summary>
        private bool TryMatchEnd(string targetEnd, string checkPath, string rawCheckPath)
        {
            if (checkPath.Length < targetEnd.Length + 1)
                return false;

            var j = checkPath.Length - 1;

            for (int i = targetEnd.Length - 1; i >= 0 && j >= 0; i--)
            {
                var targetChar = targetEnd[i];
                var checkChar = checkPath[j];

                if (IsSeparator(targetChar))
                {
                    if (!IsSeparator(checkChar))
                        continue;
                }
                else if (IsSeparator(checkChar))
                {
                    // Repeat this loop with the same i and the next j.
                    i++;
                }
                else if (targetChar != char.ToLower(checkChar))
                {
                    return false;
                }

                j--;
            }

            if (!FinalValidation(rawCheckPath))
                return false;

            MatchedPath = checkPath;
            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified value is a space or any of the following characters: ./+
        /// </summary>
        public static bool IsSeparator(char c)
        {
            switch (c)
            {
                case '.':
                case ',':
                case '/':
                case '+':
                case '-':
                case ' ':
                    return true;
                default:
                    return false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Once a potentially better match is found, this method is called to verify that it is actually an acceptable
        /// target.
        /// </summary>
        protected virtual bool FinalValidation(string path) => true;

        /************************************************************************************************************************/
    }
}

#endif

