using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core.Patterns
{
    /// <summary>
    /// 通用对象池（纯 C# 类，非 MonoBehaviour）。
    ///
    /// 相较于旧版改进：
    ///   • 内部改用 <see cref="Stack{T}"/> — LIFO 使最近归还的对象仍在 CPU 缓存中，
    ///     比 Queue 的 FIFO 有更好的缓存局部性。
    ///   • <paramref name="maxSize"/> 上限：池满时新归还对象触发 onDestroy 而非无限堆积。
    ///   • 修复 CountAll bug：旧版 Release 不会减 CountAll 导致计数错误。
    ///   • 新增 <see cref="Get(out PooledObject{T})"/> — using 作用域自动归还。
    ///
    /// ■ 手动管理：
    /// <code>
    ///   var pool = new ObjectPool&lt;MyClass&gt;(() => new MyClass(), initialSize: 8);
    ///   var obj  = pool.Get();
    ///   // ... 使用 obj ...
    ///   pool.Release(obj);
    /// </code>
    ///
    /// ■ using 作用域：
    /// <code>
    ///   using (pool.Get(out var obj))
    ///   {
    ///       obj.DoWork();
    ///   }   // 自动归还
    /// </code>
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T>      _pool;
        private readonly Func<T>       _createFunc;
        private readonly Action<T>     _actionOnGet;
        private readonly Action<T>     _actionOnRelease;
        private readonly Action<T>     _actionOnDestroy;
        private readonly int           _maxSize;

        /// <summary>池中总实例数（空闲 + 活跃）。</summary>
        public int CountAll     { get; private set; }
        /// <summary>当前正在使用中的实例数。</summary>
        public int CountActive  => CountAll - CountInactive;
        /// <summary>当前空闲（可复用）的实例数。</summary>
        public int CountInactive => _pool.Count;

        /// <param name="createFunc">创建新实例的工厂方法。</param>
        /// <param name="actionOnGet">从池中取出时的回调（重置状态等）。</param>
        /// <param name="actionOnRelease">归还到池时的回调（清理引用等）。</param>
        /// <param name="actionOnDestroy">池满时销毁实例的回调。</param>
        /// <param name="maxSize">池的最大容量（超限时销毁而非缓存）。</param>
        /// <param name="initialSize">启动时预热的数量。</param>
        public ObjectPool(
            Func<T>   createFunc,
            Action<T> actionOnGet     = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            int       maxSize         = 64,
            int       initialSize     = 0)
        {
            _createFunc      = createFunc  ?? throw new ArgumentNullException(nameof(createFunc));
            _actionOnGet     = actionOnGet;
            _actionOnRelease = actionOnRelease;
            _actionOnDestroy = actionOnDestroy;
            _maxSize         = Mathf.Max(1, maxSize);
            _pool            = new Stack<T>(Mathf.Max(initialSize, 8));

            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(_createFunc());
                CountAll++;
            }
        }

        // ── 核心 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// 从池中取出一个实例（池空时自动创建）。
        /// 使用完毕后必须调用 <see cref="Release"/> 归还，否则造成"内存泄漏"。
        /// </summary>
        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
                CountAll++;
            }
            _actionOnGet?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// 取出实例并返回 using 作用域令牌，作用域结束时自动归还。
        /// </summary>
        /// <param name="scope">作用域令牌，实现 <see cref="IDisposable"/>。</param>
        public PooledObject<T> Get(out T scope)
        {
            scope = Get();
            return new PooledObject<T>(this, scope);
        }

        /// <summary>将实例归还池中。归还后不得再持有或访问该实例。</summary>
        public void Release(T obj)
        {
            if (obj == null) return;
            _actionOnRelease?.Invoke(obj);

            if (_pool.Count < _maxSize)
            {
                _pool.Push(obj);
            }
            else
            {
                // 池满：销毁溢出实例
                _actionOnDestroy?.Invoke(obj);
                CountAll--;
            }
        }

        /// <summary>清空池，对每个空闲实例调用 onDestroy 回调。</summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                _actionOnDestroy?.Invoke(_pool.Pop());
                CountAll--;
            }
        }
    }

    /// <summary>
    /// <see cref="ObjectPool{T}.Get(out T)"/> 返回的作用域令牌。
    /// </summary>
    public readonly struct PooledObject<T> : IDisposable where T : class
    {
        private readonly ObjectPool<T> _pool;
        private readonly T             _obj;
        internal PooledObject(ObjectPool<T> pool, T obj) { _pool = pool; _obj = obj; }
        public void Dispose() => _pool?.Release(_obj);
    }

    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private bool expandPool = true;

        private Queue<GameObject> pool = new Queue<GameObject>();
        private Transform poolContainer;
        private bool _initialized;

        private void Awake()
        {
            if (prefab != null)
                Initialize(prefab, initialSize);
        }

        /// <summary>
        /// Initializes the pool at runtime with a given prefab.
        /// Safe to call multiple times — subsequent calls are ignored unless
        /// <paramref name="force"/> is true.
        /// </summary>
        public void Initialize(GameObject poolPrefab, int size = 10, bool force = false)
        {
            if (_initialized && !force) return;

            prefab = poolPrefab;
            _initialized = true;

            if (poolContainer == null)
            {
                poolContainer = new GameObject($"Pool_{prefab.name}").transform;
                poolContainer.SetParent(transform);
            }

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolContainer);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                NotifyGetFromPool(obj);
                return obj;
            }

            if (expandPool && prefab != null)
            {
                GameObject obj = Instantiate(prefab, poolContainer);
                obj.SetActive(true);
                NotifyGetFromPool(obj);
                return obj;
            }

            return null;
        }

        public void Release(GameObject obj)
        {
            if (obj == null) return;
            NotifyReleaseToPool(obj);
            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);
            pool.Enqueue(obj);
        }

        private static void NotifyGetFromPool(GameObject obj)
        {
            var poolable = obj.GetComponent<Framework.Interfaces.IPoolable>();
            poolable?.OnGetFromPool();
        }

        private static void NotifyReleaseToPool(GameObject obj)
        {
            var poolable = obj.GetComponent<Framework.Interfaces.IPoolable>();
            poolable?.OnReleaseToPool();
        }
    }
}
