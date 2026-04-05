using UnityEngine;
using System;
using Gameplay.Combat;
using Framework.Events;
using RPG.Simulation;

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
        /// Writes the authoritative cooldown value received from the logic-thread simulation.
        /// Only call from the main thread (via MainThreadDispatcher).
        /// </summary>
        public void SyncCooldown(float remaining)
        {
            RemainingCooldown = remaining;
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
        [Header("技能栏（直接引用）")]
        [Tooltip("直接拖入 SkillData 资产。优先级高于 skillSlotIds。")]
        public SkillData[] skillSlots = new SkillData[4];

        [Header("技能栏（ID 加载）")]
        [Tooltip("当对应 skillSlots[i] 为空时，从 GameDataService.Skills 按此 ID 加载。" +
                 "数组长度应与 skillSlots 相同。格式: skill_fireball")]
        public string[] skillSlotIds = new string[4];

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
            ResolveSkillSlotIds();
            InitializeSkills();
        }

        private void Start()
        {
            playerTransform = transform;
            animator = GetComponent<Animator>();
            BindToSimulation();
        }

        /// <summary>
        /// 对 skillSlots 中为 null 的格子，尝试按 skillSlotIds[i] 从 GameDataService 加载。
        /// </summary>
        private void ResolveSkillSlotIds()
        {
            if (skillSlotIds == null || skillSlotIds.Length == 0) return;

            // Ensure array length matches
            if (skillSlots == null || skillSlots.Length == 0)
                skillSlots = new SkillData[skillSlotIds.Length];

            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] != null) continue;           // already assigned
                if (i >= skillSlotIds.Length) continue;
                string id = skillSlotIds[i];
                if (string.IsNullOrEmpty(id)) continue;

                var loaded = RPG.Data.GameDataService.Instance?.Skills?.GetById(id);
                if (loaded != null)
                    skillSlots[i] = loaded;
                else
                    Debug.LogWarning($"[SkillController] skillSlotIds[{i}]='{id}' 未在 SkillDatabase 中找到。", this);
            }
        }

        private void Update()
        {
            UpdateCooldowns();
            HandleSkillInput();
        }

        /// <summary>
        /// Subscribe to the logic-thread simulation so cooldown state stays in sync
        /// with the background tick without polling in Update().
        /// </summary>
        private void BindToSimulation()
        {
            var sim = GameSimulation.Instance;
            if (sim == null) return;

            // Resize the simulation slot count to match the inspector configuration.
            // (SkillCooldownSimulation is constructed with the configured slot count
            //  in GameManager, so this is just a safety check.)
            sim.Skills.OnCooldownChanged += (slot, remaining) =>
            {
                // Sync back to SkillInstance so existing UI code reading
                // skillInstance.RemainingCooldown still works correctly.
                Framework.Threading.MainThreadDispatcher.Dispatch(() =>
                {
                    if (slot >= 0 && slot < skillInstances.Length)
                        skillInstances[slot]?.SyncCooldown(remaining);
                });
            };
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
            // When the logic-thread simulation is running, cooldowns are ticked there
            // at ~60 Hz and synced back to SkillInstance via MainThreadDispatcher.
            // Fall back to main-thread ticking only when no simulation is available
            // (e.g. during editor tests or before GameManager initialises).
            if (GameSimulation.Instance != null) return;

            float deltaTime = Time.deltaTime;
            foreach (var skillInstance in skillInstances)
                skillInstance?.UpdateCooldown(deltaTime);
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
        /// 尝试使用技能。
        ///
        /// 当逻辑线程仿真运行时，冷却/法力的 <em>状态变更</em> 提交给逻辑线程；
        /// 动画、音效、伤害等表现操作立即在主线程执行。
        /// 冷却/法力检查在主线程做乐观预检（线程安全只读），
        /// 真正的状态写入由逻辑线程的 TryActivate 保证原子性。
        /// </summary>
        public bool TryUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillInstances.Length) return false;

            SkillInstance skillInstance = skillInstances[slotIndex];
            if (skillInstance == null) return false;

            float manaCost = skillInstance.SkillData.GetManaCost(skillInstance.Level);
            float cooldownDuration = skillInstance.SkillData.GetCooldown(skillInstance.Level);

            var sim = GameSimulation.Instance;
            if (sim != null)
            {
                // Optimistic pre-check on main thread using thread-safe reads.
                if (sim.Skills.IsOnCooldown(slotIndex))
                {
                    Debug.Log($"Skill {slotIndex} is on cooldown (sim)");
                    return false;
                }
                if (sim.Skills.Mana < manaCost)
                {
                    Debug.Log("Not enough mana (sim)");
                    return false;
                }

                // Commit authoritative state change on the logic thread.
                // TryActivate is atomic and re-validates internally.
                int capturedSlot = slotIndex;
                sim.EnqueueWork(() => sim.Skills.TryActivate(capturedSlot, cooldownDuration, manaCost));
            }
            else
            {
                // Fallback: direct check and mutation on main thread.
                if (skillInstance.IsOnCooldown)
                {
                    Debug.Log($"Skill {slotIndex} is on cooldown");
                    return false;
                }
                if (!HasEnoughMana(manaCost))
                {
                    Debug.Log("Not enough mana");
                    return false;
                }

                skillInstance.StartCooldown();
                ConsumeMana(manaCost);
            }

            // Presentation: always runs on the main thread.
            ExecuteSkill(skillInstance);
            OnSkillUsed?.Invoke(slotIndex);

            Framework.Events.EventBus.Publish(new Framework.Events.SkillUsedEvent(
                skillInstance.SkillData.name, skillInstance.SkillData.skillName, slotIndex, skillInstance.Level));

            return true;
        }

        /// <summary>
        /// 执行技能逻辑
        /// If SkillData.executionStrategy is assigned, delegates to that strategy (Strategy Pattern).
        /// Otherwise falls back to the legacy switch on SkillType for backward compatibility.
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

            // Graph Pattern: node graph takes highest priority
            if (skillData.skillGraph != null)
            {
                RPG.Skills.Graph.SkillGraphExecutor.Execute(
                    skillData.skillGraph,
                    skillData,
                    skillInstance.Level,
                    playerTransform);
                return;
            }

            // Strategy Pattern: delegate to assigned SO strategy if present
            if (skillData.executionStrategy != null)
            {
                var context = new SkillExecutionContext(
                    skillData,
                    skillInstance.Level,
                    playerTransform,
                    GetFacingDirection());

                skillData.executionStrategy.Execute(context);
                return;
            }

            // Legacy fallback: switch on SkillType
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

            Framework.Events.EventBus.Publish(new Framework.Events.SkillUsedEvent(
                skillData.name, skillData.skillName, -1, 1, ultimate: true));
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
        /// 检查法力是否足够（优先读取逻辑层仿真状态）
        /// </summary>
        private bool HasEnoughMana(float amount)
        {
            var sim = GameSimulation.Instance;
            return sim != null ? sim.Skills.Mana >= amount : currentMana >= amount;
        }

        /// <summary>
        /// 消耗法力（仅在无仿真时作为后备，仿真路径由 TryActivate 处理）
        /// </summary>
        private void ConsumeMana(float amount)
        {
            currentMana -= (int)amount;
        }

        /// <summary>
        /// 恢复法力 — 同步到逻辑层仿真
        /// </summary>
        public void RestoreMana(float amount)
        {
            var sim = GameSimulation.Instance;
            if (sim != null)
                sim.EnqueueWork(() => sim.Skills.RestoreMana(amount));
            else
                currentMana += (int)amount;
        }

        /// <summary>
        /// 获取技能冷却进度 [0,1]，1 = 可用。
        /// 读取逻辑层仿真的冷却值（线程安全），无仿真时回退到 SkillInstance 本地值。
        /// </summary>
        public float GetSkillCooldownProgress(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillInstances.Length) return 0f;

            SkillInstance skillInstance = skillInstances[slotIndex];
            if (skillInstance?.SkillData == null) return 0f;

            float maxCooldown = skillInstance.SkillData.GetCooldown(skillInstance.Level);
            if (maxCooldown <= 0f) return 1f;

            var sim = GameSimulation.Instance;
            float remaining = sim != null
                ? sim.Skills.GetCooldown(slotIndex)
                : skillInstance.RemainingCooldown;

            return remaining <= 0f ? 1f : 1f - (remaining / maxCooldown);
        }
    }

}
