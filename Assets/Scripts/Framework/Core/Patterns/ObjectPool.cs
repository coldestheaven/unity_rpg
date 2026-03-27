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

        private void Awake()
        {
            poolContainer = new GameObject($"Pool_{prefab.name}").transform;
            poolContainer.SetParent(transform);

            for (int i = 0; i < initialSize; i++)
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
                return obj;
            }
            else if (expandPool)
            {
                GameObject obj = Instantiate(prefab, poolContainer);
                obj.SetActive(true);
                return obj;
            }
            return null;
        }

        public void Release(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);
            pool.Enqueue(obj);
        }
    }
}
