// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using UnityEngine;

namespace Weaver
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> component which automatically detects the <see cref="ObjectPool{T}"/> that
    /// created it so it can be released back to that pool (or simply destroyed if it wasn't created by a pool) using
    /// the <see cref="ObjectPool.TryReleaseOrDestroyGameObject{T}(T)"/> extension method.
    /// <para></para>
    /// When inheriting from this class, <typeparamref name="T"/> should always be the child class itself, I.E.
    /// <c>class ChildClass : PoolableBehaviour&lt;ChildClass&gt;</c>
    /// <para></para>
    /// <a href="https://kybernetik.com.au/weaver/docs/misc/object-pooling">More detailed instructons on how to use
    /// this class and those related to it can be found in the documentation</a>.
    /// </summary>
    public abstract class PoolableBehaviour<T> : MonoBehaviour, IPoolable where T : PoolableBehaviour<T>, IPoolable
    {
        /************************************************************************************************************************/

        /// <summary>The pool that created this object (or null if not created by a pool).</summary>
        public readonly ObjectPool<T> Pool = ObjectPool.GetCurrentPool<T>();

        /************************************************************************************************************************/

        /// <summary>
        /// Called by the <see cref="Pool"/> when releasing this component to it.
        /// Asserts that the <see cref="Component.gameObject"/> was active and deactivates it (unless overridden).
        /// </summary>
        public virtual void OnRelease()
        {
#if UNITY_ASSERTIONS
            if (!gameObject.activeSelf)
                Debug.LogError("Projectile was already inactive when releasing it to the pool: " + this, this);
#endif

            gameObject.SetActive(false);
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Inspector Gadgets GUI event.</summary>
        protected virtual void AfterInspectorGUI()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                if (Pool == null)
                    UnityEditor.EditorGUILayout.LabelField("Pool", "null");
                else
                    Pool.DoInspectorGUI();
            }
            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

