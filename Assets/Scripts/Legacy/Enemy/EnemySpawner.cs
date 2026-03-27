using UnityEngine;

namespace RPG.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("生成设置")]
        public GameObject[] enemyPrefabs;
        public float spawnInterval = 5f;
        public int maxEnemies = 10;
        public float spawnRange = 15f;

        [Header("生成区域")]
        public Transform spawnCenter;
        public bool useCustomBounds = false;
        public Vector2 customMinBounds;
        public Vector2 customMaxBounds;

        private float spawnTimer;
        private int currentEnemyCount;

        private void Update()
        {
            if (currentEnemyCount >= maxEnemies) return;

            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnInterval)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefabs.Length == 0) return;

            int randomIndex = Random.Range(0, enemyPrefabs.Length);
            GameObject enemyPrefab = enemyPrefabs[randomIndex];

            Vector2 spawnPosition = GetRandomSpawnPosition();

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            newEnemy.transform.SetParent(transform);

            currentEnemyCount++;
        }

        private Vector2 GetRandomSpawnPosition()
        {
            Vector2 center = useCustomBounds ? (customMinBounds + customMaxBounds) / 2f : (Vector2)(spawnCenter != null ? spawnCenter.position : transform.position);
            Vector2 boundsSize = useCustomBounds ? (customMaxBounds - customMinBounds) : new Vector2(spawnRange * 2, spawnRange * 2);

            float randomX = center.x + Random.Range(-boundsSize.x / 2f, boundsSize.x / 2f);
            float randomY = center.y + Random.Range(-boundsSize.y / 2f, boundsSize.y / 2f);

            return new Vector2(randomX, randomY);
        }

        public void OnEnemyDied()
        {
            currentEnemyCount--;
        }

        public void SetSpawnRate(float newRate)
        {
            spawnInterval = newRate;
        }

        public void SetMaxEnemies(int newMax)
        {
            maxEnemies = newMax;
        }

        private void OnDrawGizmosSelected()
        {
            if (useCustomBounds)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube((customMinBounds + customMaxBounds) / 2f, customMaxBounds - customMinBounds);
            }
            else
            {
                Vector2 center = spawnCenter != null ? spawnCenter.position : transform.position;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(center, new Vector3(spawnRange * 2, spawnRange * 2, 0));
            }
        }
    }
}
