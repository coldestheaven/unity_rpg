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
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private EnemyReward reward;

        [Header("Settings")]
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private LayerMask playerLayer;

        private GameObject player;

        public GameObject Player => player;
        public Gameplay.Combat.Health Health => health;
        public EnemyAttack Attack => attack;
        public float DetectionRange => detectionRange;
        public bool HasTarget => player != null;

        protected override void Awake()
        {
            base.Awake();

            enemyAI = GetComponent<EnemyAI>();
            health = GetComponent<Gameplay.Combat.Health>();
            attack = GetComponent<EnemyAttack>();
            reward = GetComponent<EnemyReward>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (health != null && health.IsDead) return;
            DetectPlayer();
        }

        private void DetectPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);

            if (hits.Length > 0)
            {
                player = GetClosestTarget(hits);
                enemyAI?.SetTarget(player);
            }
            else
            {
                player = null;
                enemyAI?.SetTarget(null);
            }
        }

        private GameObject GetClosestTarget(Collider2D[] hits)
        {
            GameObject bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = hit.gameObject;
                }
            }

            return bestTarget;
        }

        private void HandleDeath()
        {
            player = null;
            enemyAI?.SetTarget(null);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
