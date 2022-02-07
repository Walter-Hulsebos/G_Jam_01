// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// Settings relating to the <see cref="Procedural.ShadersScriptBuilder"/>.
    /// </summary>
    [Serializable]
    internal sealed class ShaderSettings : ProceduralScriptSettings, IOnCreate
    {
        /************************************************************************************************************************/

        public List<Shader> shaders;

        /************************************************************************************************************************/

        void IOnCreate.OnCreate()
        {
            shaders = new List<Shader>();
        }

        /************************************************************************************************************************/

        private SerializedProperty _ShadersProperty;

        public SerializedProperty GetShadersListProperty()
        {
            if (_ShadersProperty == null)
                _ShadersProperty = WeaverSettings.SerializedObject.FindProperty("_Shaders." + nameof(shaders));
            else if (_ShadersProperty.serializedObject.targetObject == null)
                _ShadersProperty = null;

            return _ShadersProperty;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes any null or duplicate elements from the <see cref="shaders"/> list.
        /// </summary>
        public void CleanList()
        {
            var set = WeaverUtilities.GetHashSet<Shader>();

            for (int i = 0; i < shaders.Count; i++)
            {
                var shader = shaders[i];
                if (shader == null || set.Contains(shader))
                    shaders.RemoveAt(i);
                else
                    set.Add(shader);
            }

            set.Release();
        }

        /************************************************************************************************************************/
    }
}

#endif

