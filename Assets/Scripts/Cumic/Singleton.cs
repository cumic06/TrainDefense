using UnityEngine;

namespace Cumic
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;

        [SerializeField]
        private bool dontDestroyOnLoad;

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}