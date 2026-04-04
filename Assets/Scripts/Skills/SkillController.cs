using UnityEngine;
using System;
using Gameplay.Combat;
using Framework.Events;

namespace RPG.Skills
{
    /// <summary>
    /// 技能实例 - 运行时技能状态
    /// </summary>
    public class SkillInstance
    {
        public SkillData SkillData { get; private set; }
        public int Level { get; private set; }
        public float RemainingCooldown { get; private set; }
        public bool IsOnCooldown => RemainingCooldown > 0;
        public bool IsUnlocked { get; private set; }

        public event Action<float> OnCooldownChanged;
        public event Action<int> OnLevelChanged;

        public SkillInstance(SkillData skillData, int startLevel = 1)
        {
            SkillData = skillData;
            Level = startLevel;
            RemainingCooldown = 0f;
            IsUnlocked = true;
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill()
        {
            if (IsOnCooldown || !IsUnlocked)
            {
                return false;
            }

            StartCooldown();
            return true;
        }

        /// <summary>
        /// 开始冷却
        /// </summary>
        public void StartCooldown()
        {
            RemainingCooldown = SkillData.GetCooldown(Level);
        }

        /// <summary>
        /// 更新冷却
        /// </summary>
        public void UpdateCooldown(float deltaTime)
        {
            if (IsOnCooldown)
            {
                RemainingCooldown = Mathf.Max(0f, RemainingCooldown - deltaTime);
                OnCooldownChanged?.Invoke(RemainingCooldown);
            }
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public bool LevelUp()
        {
            if (!SkillData.CanLevelUp(Level))
            {
                return false;
            }

            Level++;
            OnLevelChanged?.Invoke(Level);
            return true;
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            RemainingCooldown = 0f;
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }

        /// <summary>
        /// 解锁/锁定技能
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
        }
    }

    /// <summary>
    /// 技能控制器 - 管理所有技能
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        [Header("技能栏")]
        public SkillData[] skillSlots = new SkillData[4];

        private SkillInstance[] skillInstances;
        private Transform playerTransform;
        private Animator animator;
        private int currentMana;

        public int SkillSlotCount => skillSlots.Length;
        public SkillInstance this[int index] => skillInstances[index];

        public event Action<int> OnSkillUsed;
        public event Action<int> OnSkillUnlocked;

        private void Awake()
        {
            InitializeSkills();
        }

        private void Start()
        {
            playerTransform = transform;
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            UpdateCooldowns();
            HandleSkillInput();
        }

        private void InitializeSkills()
        {
            skillInstances = new SkillInstance[skillSlots.Length];

            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] != null)
                {
                    skillInstances[i] = new SkillInstance(skillSlots[i]);
                }
            }
        }

        private void UpdateCooldowns()
        {
            float deltaTime = Time.deltaTime;

            foreach (var skillInstance in skillInstances)
            {
                if (skillInstance != null)
                {
                    skillInstance.UpdateCooldown(deltaTime);
                }
            }
        }

        private void HandleSkillInput()
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                KeyCode hotkey = skillSlots[i]?.hotkey ?? KeyCode.Alpha1 + i;

                if (Input.GetKeyDown(hotkey))
                {
                    TryUseSkill(i);
                }
            }
        }

        /// <summary>
        /// 尝试使用技能
        /// </summary>
        public bool TryUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillInstances.Length)
            {
                return false;
            }

            SkillInstance skillInstance = skillInstances[slotIndex];
            if (skillInstance == null)
            {
                return false;
            }

            // 检查冷却
            if (skillInstance.IsOnCooldown)
            {
                Debug.Log($"Skill {slotIndex} is on cooldown");
                return false;
            }

            // 检查法力
            float manaCost = skillInstance.SkillData.GetManaCost(skillInstance.Level);
            if (!HasEnoughMana(manaCost))
            {
                Debug.Log($"Not enough mana");
                return false;
            }

            // 使用技能
            if (skillInstance.UseSkill())
            {
                ConsumeMana(manaCost);
                ExecuteSkill(skillInstance);
                OnSkillUsed?.Invoke(slotIndex);

                EventManager.Instance?.TriggerEvent("SkillUsed", new SkillUsedEventArgs
                {
                    skillName = skillInstance.SkillData.skillName,
                    slotIndex = slotIndex,
                    level = skillInstance.Level
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行技能逻辑
        /// </summary>
        private void ExecuteSkill(SkillInstance skillInstance)
        {
            SkillData skillData = skillInstance.SkillData;

            // 播放动画
            animator?.SetTrigger("Skill");

            // 播放音效
            if (skillData.castSound != null)
            {
                AudioSource.PlayClipAtPoint(skillData.castSound, transform.position);
            }

            // 根据技能类型执行不同逻辑
            switch (skillData.skillType)
            {
                case SkillType.Active:
                    ExecuteActiveSkill(skillData, skillInstance.Level);
                    break;
                case SkillType.Ultimate:
                    ExecuteUltimateSkill(skillData, skillInstance.Level);
                    break;
                case SkillType.Toggle:
                    ExecuteToggleSkill(skillData);
                    break;
                default:
                    Debug.LogWarning($"Skill type {skillData.skillType} not implemented");
                    break;
            }
        }

        private void ExecuteActiveSkill(SkillData skillData, int skillLevel)
        {
            // 创建技能效果
            if (skillData.skillEffectPrefab != null)
            {
                GameObject effect = Instantiate(skillData.skillEffectPrefab, transform.position, Quaternion.identity);

                // 设置技能方向
                Vector3 direction = GetFacingDirection();
                effect.transform.forward = direction;

                // 传递技能数据到效果脚本
                var skillEffect = effect.GetComponent<SkillEffect>();
                if (skillEffect != null)
                {
                    skillEffect.Initialize(skillData, playerTransform, skillLevel);
                }

                // 自动销毁
                Destroy(effect, 5f);
            }

            // 应用伤害
            ApplySkillDamage(skillData, skillLevel);
        }

        private void ExecuteUltimateSkill(SkillData skillData, int skillLevel)
        {
            // 终极技能可能需要特殊处理
            ExecuteActiveSkill(skillData, skillLevel);

            // 触发终极技能事件
            EventManager.Instance?.TriggerEvent("UltimateSkillUsed", new SkillUsedEventArgs
            {
                skillName = skillData.skillName,
                slotIndex = -1,
                level = 1
            });
        }

        private void ExecuteToggleSkill(SkillData skillData)
        {
            // 开关技能逻辑
            Debug.Log($"Toggle skill: {skillData.skillName}");
        }

        private void ApplySkillDamage(SkillData skillData, int skillLevel)
        {
            // 根据目标类型寻找目标
            Collider2D[] targets = FindTargets(skillData);
            DamageInfo damageInfo = new DamageInfo(
                skillData.GetDamage(skillLevel),
                transform.position,
                gameObject,
                CombatDamageTypeMapper.FromSkillDamageType(skillData.damageType),
                CombatHitKind.Skill);

            foreach (var target in targets)
            {
                CombatResolver.TryApplyDamage(target, damageInfo);
            }
        }

        private Collider2D[] FindTargets(SkillData skillData)
        {
            LayerMask targetLayer = LayerMask.GetMask("Enemy");

            switch (skillData.targetType)
            {
                case SkillTargetType.Enemy:
                    return Physics2D.OverlapCircleAll(transform.position, skillData.range, targetLayer);

                case SkillTargetType.Self:
                    return new Collider2D[] { GetComponent<Collider2D>() };

                case SkillTargetType.Area:
                    // 需要指定区域中心
                    return Physics2D.OverlapCircleAll(transform.position, skillData.areaRadius, targetLayer);

                default:
                    return new Collider2D[0];
            }
        }

        private Vector3 GetFacingDirection()
        {
            // 获取玩家朝向
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                return Vector3.left;
            }
            return Vector3.right;
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public bool LevelUpSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillInstances.Length)
            {
                return false;
            }

            return skillInstances[slotIndex]?.LevelUp() ?? false;
        }

        /// <summary>
        /// 解锁技能
        /// </summary>
        public void UnlockSkill(int slotIndex, SkillData skillData)
        {
            if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            {
                return;
            }

            skillSlots[slotIndex] = skillData;
            skillInstances[slotIndex] = new SkillInstance(skillData);
            OnSkillUnlocked?.Invoke(slotIndex);
        }

        /// <summary>
        /// 检查法力是否足够
        /// </summary>
        private bool HasEnoughMana(float amount)
        {
            return currentMana >= amount;
        }

        /// <summary>
        /// 消耗法力
        /// </summary>
        private void ConsumeMana(float amount)
        {
            currentMana -= (int)amount;
        }

        /// <summary>
        /// 恢复法力
        /// </summary>
        public void RestoreMana(float amount)
        {
            currentMana += (int)amount;
        }

        /// <summary>
        /// 获取技能冷却进度
        /// </summary>
        public float GetSkillCooldownProgress(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillInstances.Length)
            {
                return 0f;
            }

            SkillInstance skillInstance = skillInstances[slotIndex];
            if (skillInstance == null || skillInstance.SkillData == null)
            {
                return 0f;
            }

            float maxCooldown = skillInstance.SkillData.GetCooldown(skillInstance.Level);
            if (maxCooldown <= 0)
            {
                return 1f;
            }

            return 1f - (skillInstance.RemainingCooldown / maxCooldown);
        }
    }

    [System.Serializable]
    public class SkillUsedEventArgs
    {
        public string skillName;
        public int slotIndex;
        public int level;
    }
}
