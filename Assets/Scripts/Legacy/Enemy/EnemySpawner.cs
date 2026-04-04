using System.Collections.Generic;
using Framework.Core.Patterns;
using UnityEngine;

namespace RPG.Enemy
{
    /// <summary>
    /// Spawns enemies at a configurable interval using:
    ///   - Factory Method (IEnemyFactory) to decouple creation logic
    ///   - Object Pool (GameObjectPool) per prefab to avoid per-spawn allocation
    ///
    /// Pool containers are created as children of this GameObject on Awake.
    /// Dead enemies must call <see cref="ReturnToPool"/> to be recycled.
    /// </summary>
    [RequireComponent(typeof(EnemyFactory))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject[] enemyPrefabs;
        public float spawnInterval = 5f;
        public int maxEnemies = 10;
        public float spawnRange = 15f;

        [Header("Pool Settings")]
        [Tooltip("Pre-warm each prefab's pool with this many instances.")]
        [SerializeField] private int initialPoolSizePerPrefab = 5;

        [Header("Spawn Area")]
        public Transform spawnCenter;
        public bool useCustomBounds = false;
        public Vector2 customMinBounds;
        public Vector2 customMaxBounds;

        private float _spawnTimer;
        private int _activeEnemyCount;
        private IEnemyFactory _factory;

        // One pool per prefab, keyed by instance-ID for reliability
        private readonly Dictionary<int, GameObjectPool> _pools =
            new Dictionary<int, GameObjectPool>();

        private void Awake()
        {
            _factory = GetComponent<IEnemyFactory>();
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            if (enemyPrefabs == null) return;

            foreach (var prefab in enemyPrefabs)
            {
                if (prefab == null) continue;

                int key = prefab.GetInstanceID();
                if (_pools.ContainsKey(key)) continue;

                // Create a dedicated child GameObject for each pool
                var poolHost = new GameObject($"Pool_{prefab.name}");
                poolHost.transform.SetParent(transform);

                var pool = poolHost.AddComponent<GameObjectPool>();
                pool.Initialize(prefab, initialPoolSizePerPrefab);
                _pools[key] = pool;
            }
        }

        private void Update()
        {
            if (_activeEnemyCount >= maxEnemies) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                SpawnEnemy();
                _spawnTimer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            int idx = Random.Range(0, enemyPrefabs.Length);
            GameObject prefab = enemyPrefabs[idx];
            if (prefab == null) return;

            int key = prefab.GetInstanceID();
            Vector2 position = GetRandomSpawnPosition();

            GameObject go;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
                if (go != null)
                {
                    go.transform.position = position;
                }
            }
            else
            {
                // Fallback: use factory directly if pool was not created
                var enemy = _factory?.Create(prefab, position, transform);
                go = enemy != null ? enemy.gameObject : null;
            }

            if (go == null) return;

            // Register death callback so we can recycle or decrement the counter
            var enemyBase = go.GetComponent<EnemyBase>();
            if (enemyBase != null)
                RegisterDeathCallback(enemyBase, prefab);

            _activeEnemyCount++;
        }

        /// <summary>
        /// Call this (e.g. from a death callback) to return an enemy to its pool.
        /// </summary>
        public void ReturnToPool(GameObject enemy, GameObject sourcePrefab)
        {
            _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);

            if (sourcePrefab == null)
            {
                Destroy(enemy);
                return;
            }

            int key = sourcePrefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var pool))
                pool.Release(enemy);
            else
                Destroy(enemy);
        }

        private void RegisterDeathCallback(EnemyBase enemy, GameObject sourcePrefab)
        {
            // We listen for the EnemyDiedEvent published by EnemyReward and correlate
            // by position; for a cleaner approach, store (enemy, prefab) in a dictionary.
            // Here we wire a simple one-shot lambda directly on the health component.
            var health = enemy.GetComponent<Gameplay.Combat.Health>();
            if (health == null) return;

            System.Action onDeath = null;
            onDeath = () =>
            {
                health.OnDeath -= onDeath;
                // Delay return so death animations can finish
                StartCoroutine(DelayedReturn(enemy.gameObject, sourcePrefab, 0.6f));
            };
            health.OnDeath += onDeath;
        }

        private System.Collections.IEnumerator DelayedReturn(
            GameObject enemy, GameObject sourcePrefab, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enemy != null)
                ReturnToPool(enemy, sourcePrefab);
        }

        // Legacy API kept for external callers
        public void OnEnemyDied() => _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        public void SetSpawnRate(float rate) => spawnInterval = rate;
        public void SetMaxEnemies(int max) => maxEnemies = max;

        private Vector2 GetRandomSpawnPosition()
        {
            Vector2 center = useCustomBounds
                ? (customMinBounds + customMaxBounds) * 0.5f
                : spawnCenter != null
                    ? (Vector2)spawnCenter.position
                    : (Vector2)transform.position;

            Vector2 halfSize = useCustomBounds
                ? (customMaxBounds - customMinBounds) * 0.5f
                : new Vector2(spawnRange, spawnRange);

            return center + new Vector2(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y));
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 center = useCustomBounds
                ? (customMinBounds + customMaxBounds) * 0.5f
                : spawnCenter != null
                    ? (Vector2)spawnCenter.position
                    : (Vector2)transform.position;

            Vector3 size = useCustomBounds
                ? (Vector3)(customMaxBounds - customMinBounds)
                : new Vector3(spawnRange * 2f, spawnRange * 2f, 0f);

            if (useCustomBounds)
                size.z = 0f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
