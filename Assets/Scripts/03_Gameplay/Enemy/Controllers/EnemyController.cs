using UnityEngine;

namespace Gameplay.Enemy
{
    /// <summary>
    /// 敌人控制器
    /// </summary>
    public class EnemyController : Framework.Base.MonoBehaviourBase
    {
        [Header("References")]
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private Gameplay.Combat.Health health;

        [Header("Settings")]
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private LayerMask playerLayer;

        private GameObject player;

        public GameObject Player => player;

        protected override void Awake()
        {
            base.Awake();

            enemyAI = GetComponent<EnemyAI>();
            health = GetComponent<Gameplay.Combat.Health>();
        }

        protected override void Update()
        {
            base.Update();
            DetectPlayer();
        }

        private void DetectPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);

            if (hits.Length > 0)
            {
                player = hits[0].gameObject;
                enemyAI?.SetTarget(player);
            }
            else
            {
                player = null;
                enemyAI?.SetTarget(null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
