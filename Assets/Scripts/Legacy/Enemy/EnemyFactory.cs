using UnityEngine;

namespace RPG.Enemy
{
    /// <summary>
    /// Factory Method pattern — centralises enemy creation for the legacy RPG.Enemy stack.
    ///
    /// EnemySpawner and any other system that needs to create enemies should call
    /// these methods instead of using Instantiate directly.  The interface allows
    /// the factory to be swapped (e.g. for a pooled factory) without changing callers.
    /// </summary>
    public interface IEnemyFactory
    {
        /// <summary>Instantiates <paramref name="prefab"/> at the given world position.</summary>
        EnemyBase Create(GameObject prefab, Vector3 position, Transform parent = null);

        /// <summary>Instantiates <paramref name="prefab"/> and applies the given <see cref="EnemyData"/> to the result.</summary>
        EnemyBase Create(EnemyData data, GameObject prefab, Vector3 position, Transform parent = null);
    }

    /// <summary>
    /// Default <see cref="IEnemyFactory"/> implementation.
    /// Attach this component to the same GameObject as <see cref="EnemySpawner"/> or
    /// reference it from there.
    /// </summary>
    public class EnemyFactory : MonoBehaviour, IEnemyFactory
    {
        public EnemyBase Create(GameObject prefab, Vector3 position, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[EnemyFactory] Prefab is null — cannot create enemy.");
                return null;
            }

            GameObject go = Instantiate(prefab, position, Quaternion.identity, parent);
            return go.GetComponent<EnemyBase>();
        }

        public EnemyBase Create(EnemyData data, GameObject prefab, Vector3 position,
                                Transform parent = null)
        {
            EnemyBase enemy = Create(prefab, position, parent);

            if (enemy != null && data != null)
            {
                enemy.data = data;
            }

            return enemy;
        }
    }
}
