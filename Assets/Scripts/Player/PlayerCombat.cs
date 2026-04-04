using UnityEngine;
using Framework.Events;
using Framework.Interfaces;

namespace RPG.Player
{
    /// <summary>
    /// 玩家战斗系统 - 从PlayerController中分离
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("攻击设置")]
        public GameObject attackHitbox;
        public float attackRange = 1.5f;
        public float attackDamage = 25;
        public float attackCooldown = 1f;
        public float attackDuration = 0.3f;

        [Header("连击设置")]
        public int maxCombo = 3;
        public float comboWindow = 0.5f;

        [Header("攻击特效")]
        public GameObject attackEffect;
        public LayerMask enemyLayer;

        private PlayerInput input;
        private Animator animator;
        private PlayerMovement movement;

        private bool canAttack = true;
        private bool isAttacking;
        private int currentCombo;
        private float lastAttackTime;

        public bool IsAttacking => isAttacking;
        public int CurrentCombo => currentCombo;

        public event System.Action<int> OnAttack;
        public event System.Action<int> OnHit;
        public event System.Action OnComboReset;

        private void Awake()
        {
            input = GetComponent<PlayerInput>();
            animator = GetComponent<Animator>();
            movement = GetComponent<PlayerMovement>();

            if (attackHitbox != null)
            {
                attackHitbox.SetActive(false);
            }
        }

        private void Update()
        {
            HandleAttackInput();
            CheckComboWindow();
        }

        private void HandleAttackInput()
        {
            if (input.AttackPressed && canAttack && movement.CanMove)
            {
                Attack();
            }
        }

        public void Attack()
        {
            if (!canAttack || isAttacking) return;

            isAttacking = true;
            canAttack = false;
            currentCombo++;

            if (currentCombo > maxCombo)
            {
                currentCombo = 1;
            }

            OnAttack?.Invoke(currentCombo);
            lastAttackTime = Time.time;

            EnableAttackHitbox();
            animator.SetInteger("ComboCount", currentCombo);
            animator.SetTrigger("Attack");
            PlayAttackEffect();

            Invoke(nameof(EndAttack), attackDuration);
            Invoke(nameof(ResetAttackCooldown), attackCooldown);

            EventManager.Instance?.TriggerEvent("PlayerAttacked", new AttackEventArgs
            {
                comboCount = currentCombo,
                damage = (int)attackDamage
            });
        }

        private void EnableAttackHitbox()
        {
            if (attackHitbox != null)
            {
                attackHitbox.SetActive(true);
            }

            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
            foreach (var enemy in enemies)
            {
                var damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage, transform.position);
                    OnHit?.Invoke((int)attackDamage);
                }
            }
        }

        private void DisableAttackHitbox()
        {
            if (attackHitbox != null)
            {
                attackHitbox.SetActive(false);
            }
        }

        private void PlayAttackEffect()
        {
            if (attackEffect != null)
            {
                GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
                Destroy(effect, 0.5f);
            }
        }

        private void CheckComboWindow()
        {
            if (isAttacking && Time.time - lastAttackTime > comboWindow)
            {
                ResetCombo();
            }
        }

        private void ResetCombo()
        {
            currentCombo = 0;
            animator.SetInteger("ComboCount", 0);
            OnComboReset?.Invoke();
        }

        private void EndAttack()
        {
            DisableAttackHitbox();
            isAttacking = false;
        }

        private void ResetAttackCooldown()
        {
            canAttack = true;
        }

        public void SetAttackDamage(int damage)
        {
            attackDamage = damage;
        }

        public void SetAttackRange(float range)
        {
            attackRange = range;
        }

        public void SetAttackCooldown(float cooldown)
        {
            attackCooldown = cooldown;
        }

        public void ResetCombat()
        {
            isAttacking = false;
            canAttack = true;
            currentCombo = 0;
            DisableAttackHitbox();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

    [System.Serializable]
    public class AttackEventArgs
    {
        public int comboCount;
        public int damage;
    }
}
