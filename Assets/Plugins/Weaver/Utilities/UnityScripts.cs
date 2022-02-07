// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only] A variety of utility methods relating to script assets in Unity.</summary>
    public static class UnityScripts
    {
        /************************************************************************************************************************/
        #region Script Details
        /************************************************************************************************************************/

        private static Dictionary<Type, MonoScript> _Scripts;

        /// <summary>
        /// Tries to get the script asset containing `type`.
        /// </summary>
        public static MonoScript GetScript(Type type)
        {
            MonoScript script;

            if (_Scripts == null)
            {
                var scripts = MonoImporter.GetAllRuntimeMonoScripts();
                _Scripts = new Dictionary<Type, MonoScript>(scripts.Length);
                for (int i = 0; i < scripts.Length; i++)
                {
                    script = scripts[i];
                    var scriptClass = script.GetClass();
                    if (scriptClass != null && !_Scripts.ContainsKey(scriptClass))
                        _Scripts.Add(scriptClass, script);
                }
            }

            var rootType = type;
            while (rootType.DeclaringType != null)
                rootType = rootType.DeclaringType;

            _Scripts.TryGetValue(rootType, out script);
            return script;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Tries to get the path to the script asset containing `type`. If this fails, it instead gets the path to its
        /// assembly.
        /// </summary>
        public static string GetSourcePath(Type type, out bool isScript)
        {
            var script = GetScript(type);
            if (script != null)
            {
                isScript = true;
                return AssetDatabase.GetAssetPath(script);
            }
            else
            {
                isScript = false;
                return type.Assembly.Location.ReplaceSlashesForward();
            }
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, int> _ExecutionTimes;

        /// <summary>
        /// Tries to get the execution time of the script asset containing `type`.
        /// </summary>
        public static int GetExecutionTime(Type type)
        {
            if (_ExecutionTimes == null)
                _ExecutionTimes = new Dictionary<Type, int>();

            if (!_ExecutionTimes.TryGetValue(type, out var executionTime))
            {
                var script = GetScript(type);
                if (script != null)
                    executionTime = MonoImporter.GetExecutionOrder(script);

                _ExecutionTimes.Add(type, executionTime);
            }

            return executionTime;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

