using UnityEngine;

namespace Framework.Core.Patterns
{
    /// <summary>
    /// MonoBehaviour 单例基类。
    ///
    /// ■ 修复：在 <see cref="Awake"/> 中即时注册 <c>_instance</c>，
    ///   解决旧版"仅在 <c>Instance</c> getter 首次调用时才注册"导致的启动顺序问题：
    ///   若其他组件的 <c>Awake</c> 在单例 <c>Awake</c> 之前运行并访问 <c>Instance</c>，
    ///   旧版会再次 <c>FindObjectOfType</c> 或创建第二个 GameObject，引发重复实例。
    ///   现在无论访问时序如何，<c>Instance</c> 都指向已存在于场景中的组件。
    ///
    /// ■ 重复检测：若场景中存在第二个实例，Awake 立即将其销毁并记录警告。
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
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

                    // 场景中已存在但 Awake 尚未运行（极少情况）
#if UNITY_2023_1_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                    if (_instance == null)
                    {
                        var go = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
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
                    Debug.LogWarning(
                        $"[Singleton] 检测到重复的 {typeof(T).Name}，销毁多余实例 '{gameObject.name}'。");
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            lock (_lock)
            {
                if (_instance == this)
                    _instance = null;
            }
        }
    }
}
