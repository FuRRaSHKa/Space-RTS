using UnityEngine;

namespace HalloGames.Architecture.Singletones
{
    public class MonoSingleton<TInstance> : MonoBehaviour where TInstance : Component
    {
        public static TInstance Instance
        {
            get;
            private set;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this as TInstance;
            OverridedAwake();
        }

        /// <summary>
        /// Call this instead Awake
        /// </summary>
        protected virtual void OverridedAwake()
        {

        }
    }
}
