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
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong - there should never be more than 1 singleton!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();
                            DontDestroyOnLoad(singleton);
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
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

        protected virtual void OnDestroy()
        {
            _applicationIsQuitting = true;
        }
    }
}
