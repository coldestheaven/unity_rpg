using System.Collections.Generic;
using Framework.Core.Patterns;
using Framework.Interfaces;
using UnityEngine;

namespace Framework.Core.Pools
{
    /// <summary>
    /// 集中式多预制件 GameObject 对象池管理器（单例）。
    ///
    /// 相较于原有的 <c>GameObjectPool（MonoBehaviour，单预制件）</c>，本管理器：
    ///   • 支持任意数量的预制件，统一在一个 GameObject 下管理。
    ///   • 可通过 <see cref="Get{T}"/> 泛型接口直接返回组件引用。
    ///   • 支持每种预制件设置最大容量上限（防止内存无限膨胀）。
    ///   • 场景切换时调用 <see cref="ClearAll"/> 或 <see cref="DestroyPool"/> 按需清理。
    ///
    /// ■ 典型用法：
    /// <code>
    ///   // 预热（可在 Loading 界面调用）
    ///   GameObjectPoolManager.Instance.Preload(bulletPrefab, 50, maxSize: 100);
    ///
    ///   // 取出
    ///   var bullet = GameObjectPoolManager.Instance.Get(bulletPrefab, firePoint.position, firePoint.rotation);
    ///   var bulletComp = GameObjectPoolManager.Instance.Get&lt;Bullet&gt;(bulletPrefab, pos, rot);
    ///
    ///   // 归还（在 Bullet.OnTriggerEnter 或定时器里调用）
    ///   GameObjectPoolManager.Instance.Release(bullet);
    /// </code>
    ///
    /// ■ 自动归还：让预制件根组件实现 <see cref="IPoolable"/>，
    ///   池在 Get/Release 时会自动通知 <see cref="IPoolable.OnGetFromPool"/> /
    ///   <see cref="IPoolable.OnReleaseToPool"/>。
    /// </summary>
    public sealed class GameObjectPoolManager : Singleton<GameObjectPoolManager>
    {
        // ── 内部数据 ──────────────────────────────────────────────────────────

        private sealed class Pool
        {
            public readonly GameObject    Prefab;
            public readonly Transform     Container;
            public readonly Stack<GameObject> Inactive;
            public          int           MaxSize;
            public          int           TotalCreated;

            public Pool(GameObject prefab, Transform parent, int maxSize)
            {
                Prefab       = prefab;
                MaxSize      = maxSize;
                TotalCreated = 0;
                Inactive     = new Stack<GameObject>(Mathf.Max(8, maxSize / 2));

                Container = new GameObject($"[Pool] {prefab.name}").transform;
                Container.SetParent(parent);
            }
        }

        // 预制件 → 池
        private readonly Dictionary<GameObject, Pool> _pools
            = new Dictionary<GameObject, Pool>(16);

        // 活跃实例 → 所属池（用于 Release 时定位归属）
        private readonly Dictionary<GameObject, Pool> _activeToPool
            = new Dictionary<GameObject, Pool>(64);

        // ── Unity 生命周期 ────────────────────────────────────────────────────

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _pools.Clear();
            _activeToPool.Clear();
        }

        // ── 预热 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 预热指定预制件的对象池。
        /// 适合在 Loading 界面提前分配内存，避免游戏中的首次 Instantiate 峰值。
        /// </summary>
        /// <param name="prefab">要预热的预制件。</param>
        /// <param name="count">预热数量。</param>
        /// <param name="maxSize">该预制件池的最大缓存容量（超出则丢弃而非缓存）。</param>
        public void Preload(GameObject prefab, int count, int maxSize = 64)
        {
            if (prefab == null) { Debug.LogError("[GOPool] Preload: prefab 为 null。"); return; }

            var pool = GetOrCreatePool(prefab, maxSize);
            int toCreate = Mathf.Max(0, count - pool.TotalCreated);
            for (int i = 0; i < toCreate; i++)
                pool.Inactive.Push(CreateInstance(pool));
        }

        // ── 取出 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 从池中取出 GameObject 并激活，放置在世界坐标 <paramref name="position"/>。
        /// 若池为空且未超出上限则自动扩容；超出上限时返回 <c>null</c>。
        /// </summary>
        public GameObject Get(GameObject prefab,
            Vector3    position = default,
            Quaternion rotation = default,
            Transform  parent   = null)
        {
            if (prefab == null) { Debug.LogError("[GOPool] Get: prefab 为 null。"); return null; }

            var pool = GetOrCreatePool(prefab);
            GameObject obj;

            if (pool.Inactive.Count > 0)
            {
                obj = pool.Inactive.Pop();
            }
            else if (pool.TotalCreated < pool.MaxSize)
            {
                obj = CreateInstance(pool);
            }
            else
            {
                Debug.LogWarning($"[GOPool] 池已满（maxSize={pool.MaxSize}），prefab='{prefab.name}'。" +
                                 "请增大 maxSize 或检查是否有归还遗漏。");
                return null;
            }

            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            _activeToPool[obj] = pool;
            NotifyGet(obj);
            return obj;
        }

        /// <summary>
        /// 取出 GameObject 并返回指定组件（泛型快捷版本）。
        /// </summary>
        public T Get<T>(GameObject prefab,
            Vector3    position = default,
            Quaternion rotation = default,
            Transform  parent   = null) where T : Component
        {
            var obj = Get(prefab, position, rotation, parent);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        // ── 归还 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 将 GameObject 归还池中（停用并移回容器节点）。
        /// <paramref name="obj"/> 必须是通过本管理器 <see cref="Get"/> 取出的实例。
        /// </summary>
        /// <returns>归还成功返回 <c>true</c>；找不到所属池时返回 <c>false</c>。</returns>
        public bool Release(GameObject obj)
        {
            if (obj == null) return false;

            if (!_activeToPool.TryGetValue(obj, out var pool))
            {
                Debug.LogWarning($"[GOPool] Release: '{obj.name}' 不属于任何池（可能已归还或未由本管理器创建）。");
                return false;
            }

            NotifyRelease(obj);
            _activeToPool.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(pool.Container);

            if (pool.Inactive.Count < pool.MaxSize)
                pool.Inactive.Push(obj);
            else
                Destroy(obj);   // 超限时销毁（通常不会触发）

            return true;
        }

        // ── 批量操作 ──────────────────────────────────────────────────────────

        /// <summary>将指定预制件的所有活跃实例归还池中。</summary>
        public void ReleaseAll(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool)) return;

            using (ListPool<GameObject>.Rent(out var toRelease))
            {
                foreach (var kv in _activeToPool)
                    if (kv.Value == pool) toRelease.Add(kv.Key);

                foreach (var obj in toRelease)
                    Release(obj);
            }
        }

        /// <summary>
        /// 销毁指定预制件的整个池（包括所有活跃与空闲实例）。
        /// 场景卸载时调用。
        /// </summary>
        public void DestroyPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool)) return;

            // 移除活跃追踪
            using (ListPool<GameObject>.Rent(out var toRemove))
            {
                foreach (var kv in _activeToPool)
                    if (kv.Value == pool) toRemove.Add(kv.Key);
                foreach (var obj in toRemove)
                {
                    _activeToPool.Remove(obj);
                    Destroy(obj);
                }
            }

            // 销毁容器节点（级联销毁所有空闲子物体）
            if (pool.Container != null)
                Destroy(pool.Container.gameObject);

            _pools.Remove(prefab);
        }

        /// <summary>销毁所有池（场景切换时调用）。</summary>
        public void ClearAll()
        {
            using (ListPool<GameObject>.Rent(out var prefabs))
            {
                prefabs.AddRange(_pools.Keys);
                foreach (var p in prefabs)
                    DestroyPool(p);
            }
        }

        // ── 调试 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 打印所有池的统计信息（空闲 / 活跃 / 总创建量）。
        /// </summary>
        public string GetStats()
        {
            using (StringBuilderPool.Rent(out var sb))
            {
                sb.AppendLine("[GameObjectPoolManager] 统计：");
                foreach (var kv in _pools)
                {
                    var pool = kv.Value;
                    int active = pool.TotalCreated - pool.Inactive.Count;
                    sb.Append("  ").Append(pool.Prefab.name)
                      .Append("  空闲=").Append(pool.Inactive.Count)
                      .Append("  活跃=").Append(active)
                      .Append("  已创建=").Append(pool.TotalCreated)
                      .Append("  上限=").AppendLine(pool.MaxSize.ToString());
                }
                return sb.ToString();
            }
        }

        // ── 内部方法 ──────────────────────────────────────────────────────────

        private Pool GetOrCreatePool(GameObject prefab, int maxSize = 64)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Pool(prefab, transform, maxSize);
                _pools[prefab] = pool;
            }
            return pool;
        }

        private static GameObject CreateInstance(Pool pool)
        {
            var obj = Instantiate(pool.Prefab, pool.Container);
            obj.SetActive(false);
            pool.TotalCreated++;
            return obj;
        }

        private static void NotifyGet(GameObject obj)
        {
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnGetFromPool();
        }

        private static void NotifyRelease(GameObject obj)
        {
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReleaseToPool();
        }
    }
}
