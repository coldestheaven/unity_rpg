using UnityEngine;

namespace Framework.Base
{
    public class MonoBehaviourBase : MonoBehaviour
    {
        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        protected virtual void FixedUpdate() { }
        protected virtual void LateUpdate() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }

    public abstract class ScriptableObjectBase : ScriptableObject
    {
        [SerializeField] protected string id;
        [SerializeField] protected string displayName;

        public string ID => id;
        public string DisplayName => displayName;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
        }

        public virtual void Initialize()
        {
            // Override for custom initialization
        }
    }

    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                lock (_lock)
                {
                    if (_instance != null) return _instance;

#if UNITY_2023_1_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = (T)FindObjectOfType(typeof(T));
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

        protected virtual void Awake()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    DontDestroyOnLoad(gameObject);
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);
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
