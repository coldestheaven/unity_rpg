using UnityEngine;

namespace Framework.Core.Patterns
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (!Application.isPlaying)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance != null) return _instance;

#if UNITY_2023_1_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                    if (_instance == null)
                    {
                        var singleton = new GameObject("(singleton) " + typeof(T));
                        _instance = singleton.AddComponent<T>();
                        DontDestroyOnLoad(singleton);
                    }

                    return _instance;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            lock (_lock)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }
    }
}
