using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// 遗留兼容单例基类，仅供现有 RPG.Core 系统继续使用。
    /// 新代码请使用 <see cref="Framework.Base.SingletonMonoBehaviour{T}"/>。
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();

                    if (instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
