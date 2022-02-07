// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

namespace Weaver
{
    /// <summary>
    /// Exposes a method to be called when a new instance is created by <see cref="WeaverUtilities.EnsureExists"/>.
    /// </summary>
    public interface IOnCreate
    {
        /// <summary>
        /// Called when a new instance of the implementing type is created by <see cref="WeaverUtilities.EnsureExists"/>.
        /// </summary>
        void OnCreate();
    }

    /************************************************************************************************************************/

    public static partial class WeaverUtilities
    {
        /************************************************************************************************************************/

        /// <summary>
        /// If `obj` is null this method assigns a new instance to it and calls <see cref="IOnCreate.OnCreate"/>.
        /// </summary>
        public static void EnsureExists<T>(ref T obj) where T : class, IOnCreate, new()
        {
            if (obj == null)
            {
                obj = new T();
                obj.OnCreate();
            }
        }

        /************************************************************************************************************************/
    }
}

