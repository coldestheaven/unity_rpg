using UnityEngine;

namespace Gameplay.Enemy
{
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyPresenter : Framework.Base.MonoBehaviourBase
    {
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private EnemyAttack enemyAttack;
        [SerializeField] private Gameplay.Combat.Health health;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        protected override void Awake()
        {
            base.Awake();
            enemyAI = GetComponent<EnemyAI>();
            enemyAttack = GetComponent<EnemyAttack>();
            health = GetComponent<Gameplay.Combat.Health>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (enemyAttack != null)
            {
                enemyAttack.OnAttackStarted += HandleAttackStarted;
                enemyAttack.OnAttackFinished += HandleAttackFinished;
            }
        }

        private void OnDisable()
        {
            if (enemyAttack != null)
            {
                enemyAttack.OnAttackStarted -= HandleAttackStarted;
                enemyAttack.OnAttackFinished -= HandleAttackFinished;
            }
        }

        protected override void Update()
        {
            base.Update();
            UpdateAnimation();
            UpdateFacing();
        }

        private void UpdateAnimation()
        {
            if (animator == null || enemyAI == null)
            {
                return;
            }

            animator.SetInteger("State", (int)enemyAI.CurrentState);
            animator.SetBool("IsDead", health != null && health.IsDead);
            animator.SetBool("IsMoving", enemyAI.IsMoving);
            animator.SetBool("IsAttacking", enemyAI.IsAttacking);
        }

        private void UpdateFacing()
        {
            if (spriteRenderer == null || enemyAI == null)
            {
                return;
            }

            if (Mathf.Abs(enemyAI.FacingDirectionX) > 0.01f)
            {
                spriteRenderer.flipX = enemyAI.FacingDirectionX < 0f;
            }
        }

        private void HandleAttackStarted(float damage)
        {
            animator?.SetTrigger("Attack");
            animator?.SetBool("IsAttacking", true);
        }

        private void HandleAttackFinished()
        {
            animator?.SetBool("IsAttacking", false);
        }
    }
}
