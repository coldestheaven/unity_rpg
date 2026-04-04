using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core.Patterns
{
    public class ObjectPool<T> where T : class, new()
    {
        private Queue<T> _pool;
        private System.Func<T> _createFunc;
        private System.Action<T> _actionOnGet;
        private System.Action<T> _actionOnRelease;
        private System.Action<T> _actionOnDestroy;

        public int CountAll { get; private set; }
        public int CountActive { get { return CountAll - CountInactive; } }
        public int CountInactive { get { return _pool.Count; } }

        public ObjectPool(System.Func<T> createFunc, int initialSize = 10)
        {
            _pool = new Queue<T>();
            _createFunc = createFunc;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = _createFunc();
                _pool.Enqueue(obj);
                CountAll++;
            }
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                _actionOnGet?.Invoke(obj);
                return obj;
            }
            else
            {
                T obj = _createFunc();
                CountAll++;
                _actionOnGet?.Invoke(obj);
                return obj;
            }
        }

        public void Release(T obj)
        {
            _actionOnRelease?.Invoke(obj);
            _pool.Enqueue(obj);
        }

        public void Clear()
        {
            if (_actionOnDestroy != null)
            {
                foreach (var obj in _pool)
                {
                    _actionOnDestroy(obj);
                }
            }
            _pool.Clear();
            CountAll = 0;
        }
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
