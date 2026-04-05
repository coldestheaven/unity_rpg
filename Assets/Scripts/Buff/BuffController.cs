using System;
using System.Collections.Generic;
using Core.Stats;
using Gameplay.Combat;
using UnityEngine;

namespace RPG.Buff
{
    /// <summary>
    /// 通用 Buff 控制器 — 挂载在任何可接受 Buff 的实体（玩家/敌人/NPC）上。
    ///
    /// 功能:
    ///   • 四种叠加模式（Refresh / Stack / Replace / Ignore）
    ///   • 周期伤害 (DoT) / 周期治疗 (HoT)
    ///   • 持续视觉特效管理
    ///   • 实现 <see cref="IPlayerStatModifierSource"/>，自动被
    ///     <see cref="Gameplay.Player.PlayerStatsRuntime"/> 扫描并纳入属性计算
    ///   • 向后兼容 <see cref="Gameplay.Player.PlayerBuffController"/> 签名
    ///
    /// 典型用法（技能图节点）:
    ///   var bc = target.GetComponent&lt;BuffController&gt;();
    ///   bc?.ApplyBuff(poisonData, level: 2, caster: casterTransform);
    ///
    /// 建议将此组件作为 PlayerBuffController 的替代（二者可共存）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuffController : MonoBehaviour, IBuffReceiver, IPlayerStatModifierSource
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<BuffInstance>   _buffs    = new List<BuffInstance>(8);
        private readonly List<BuffInstance>   _toRemove = new List<BuffInstance>(4);
        private DamageableBase                _damageable;  // cached for DoT/HoT

        // ── IBuffReceiver: Properties ─────────────────────────────────────────

        public IReadOnlyList<BuffInstance> ActiveBuffs => _buffs;

        // ── IBuffReceiver: Events ─────────────────────────────────────────────

        public event Action<BuffInstance>           OnBuffApplied;
        public event Action<BuffInstance>           OnBuffRemoved;
        public event Action<BuffInstance, float, float> OnBuffTick;

        // ── IPlayerStatModifierSource: Events ──────────────────────────────────

        public event Action ModifiersChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _damageable = GetComponent<DamageableBase>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _toRemove.Clear();

            for (int i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];

                // Countdown
                if (!buff.IsPermanent)
                {
                    buff.RemainingDuration -= dt;
                    if (buff.RemainingDuration <= 0f)
                    {
                        _toRemove.Add(buff);
                        continue;
                    }
                }

                // Tick effects
                if (buff.HasTick)
                {
                    buff.TickTimer -= dt;
                    if (buff.TickTimer <= 0f)
                    {
                        buff.TickTimer = buff.Data.tickInterval;
                        ProcessTick(buff);
                    }
                }
            }

            // Remove expired buffs
            if (_toRemove.Count > 0)
            {
                bool statsChanged = false;
                foreach (var expired in _toRemove)
                {
                    InternalRemove(expired);
                    if (expired.Data.statModifier.HasAnyValue() ||
                        expired.Data.statPerLevel.HasAnyValue())
                        statsChanged = true;
                }
                if (statsChanged)
                    ModifiersChanged?.Invoke();
            }
        }

        // ── IBuffReceiver: Apply ───────────────────────────────────────────────

        public BuffInstance ApplyBuff(
            BuffData data,
            int level             = 1,
            Transform caster      = null,
            float durationOverride = -1f)
        {
            if (data == null) return null;

            string id = string.IsNullOrEmpty(data.buffId) ? data.name : data.buffId;

            // Find existing instance
            BuffInstance existing = GetBuff(id);

            switch (data.stackMode)
            {
                case BuffStackMode.Ignore:
                    if (existing != null) return existing;
                    break;

                case BuffStackMode.Refresh:
                    if (existing != null)
                    {
                        existing.Refresh(level);
                        OnBuffApplied?.Invoke(existing);
                        return existing;
                    }
                    break;

                case BuffStackMode.Replace:
                    if (existing != null)
                        InternalRemove(existing);
                    break;

                case BuffStackMode.Stack:
                    if (existing != null)
                    {
                        if (existing.AddStack())
                        {
                            existing.Refresh(level);
                            ModifiersChanged?.Invoke();
                            OnBuffApplied?.Invoke(existing);
                        }
                        return existing;
                    }
                    break;
            }

            // Create new instance
            var inst = new BuffInstance(data, level, caster, durationOverride);

            // Persistent VFX
            if (data.persistentEffectPrefab != null)
            {
                inst.PersistentEffect = Instantiate(
                    data.persistentEffectPrefab, transform.position, Quaternion.identity, transform);
            }

            // Apply-on-add VFX
            if (data.applyEffectPrefab != null)
            {
                var vfx = Instantiate(data.applyEffectPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 5f);
            }

            _buffs.Add(inst);

            bool affectsStats = data.statModifier.HasAnyValue() || data.statPerLevel.HasAnyValue();
            if (affectsStats)
                ModifiersChanged?.Invoke();

            OnBuffApplied?.Invoke(inst);
            return inst;
        }

        // ── IBuffReceiver: Remove ─────────────────────────────────────────────

        public bool RemoveBuff(string buffId)
        {
            var inst = GetBuff(buffId);
            if (inst == null) return false;
            InternalRemove(inst);
            ModifiersChanged?.Invoke();
            return true;
        }

        public void RemoveAllBuffs()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
                InternalRemove(_buffs[i]);
            ModifiersChanged?.Invoke();
        }

        public void RemoveAllDebuffs()   => RemoveAllByCategory(BuffCategory.Debuff);
        public void RemoveAllByCategory(BuffCategory cat)
        {
            bool changed = false;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].Data.category == cat)
                {
                    InternalRemove(_buffs[i]);
                    changed = true;
                }
            }
            if (changed) ModifiersChanged?.Invoke();
        }

        // ── IBuffReceiver: Query ──────────────────────────────────────────────

        public bool HasBuff(string buffId)
        {
            string id = buffId ?? "";
            for (int i = 0; i < _buffs.Count; i++)
            {
                string bId = _buffs[i].Data.buffId;
                if (string.IsNullOrEmpty(bId)) bId = _buffs[i].Data.name;
                if (bId == id) return true;
            }
            return false;
        }

        public BuffInstance GetBuff(string buffId)
        {
            string id = buffId ?? "";
            for (int i = 0; i < _buffs.Count; i++)
            {
                string bId = _buffs[i].Data.buffId;
                if (string.IsNullOrEmpty(bId)) bId = _buffs[i].Data.name;
                if (bId == id) return _buffs[i];
            }
            return null;
        }

        // ── IPlayerStatModifierSource: Apply ──────────────────────────────────

        /// <summary>
        /// 累加所有有效 Buff 的属性修改到 <paramref name="stats"/>。
        /// 由 <see cref="Gameplay.Player.PlayerStatsRuntime"/> 调用。
        /// </summary>
        public void ApplyModifiers(ref PlayerStatBlock stats)
        {
            for (int i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                var mod  = buff.Data.GetStatModifier(buff.Level);
                float multiplier = buff.Stacks;  // Stack 模式叠加属性

                stats.Add(
                    mod.MaxHealth    * multiplier,
                    mod.AttackDamage * multiplier,
                    mod.Defense      * multiplier,
                    mod.MoveSpeed    * multiplier);
            }
        }

        // ── Backward compatibility (PlayerBuffController signature) ───────────

        /// <summary>
        /// 向后兼容接口 — 用 legacy 参数施加匿名 Buff。
        /// 新代码应使用 <see cref="ApplyBuff(BuffData, int, Transform, float)"/>。
        /// </summary>
        public void ApplyBuff(string sourceName, PlayerStatBlock modifier, float duration)
        {
            if (duration <= 0f) return;

            // Create a runtime-only BuffData wrapper
            var tempData = ScriptableObject.CreateInstance<BuffData>();
            tempData.buffId       = sourceName;
            tempData.displayName  = sourceName;
            tempData.duration     = duration;
            tempData.statModifier = modifier;
            tempData.stackMode    = BuffStackMode.Replace;

            ApplyBuff(tempData, 1, null, duration);

            // Immediately destroy the temp asset (don't leak ScriptableObjects)
            Destroy(tempData);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void InternalRemove(BuffInstance inst)
        {
            if (!_buffs.Remove(inst)) return;

            // Destroy persistent VFX
            if (inst.PersistentEffect != null)
                Destroy(inst.PersistentEffect);

            // Play remove VFX
            if (inst.Data.removeEffectPrefab != null)
            {
                var vfx = Instantiate(inst.Data.removeEffectPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 5f);
            }

            OnBuffRemoved?.Invoke(inst);
        }

        private void ProcessTick(BuffInstance buff)
        {
            float dmg  = buff.Data.GetTickDamage(buff.Level) * buff.Stacks;
            float heal = buff.Data.GetTickHeal(buff.Level)   * buff.Stacks;

            if (dmg > 0f && _damageable != null && !_damageable.IsDead)
            {
                var combatType = CombatDamageTypeMapper.FromSkillDamageType(buff.Data.tickDamageType);
                var info = new DamageInfo(
                    dmg,
                    buff.Caster != null ? buff.Caster.position : transform.position,
                    buff.Caster?.gameObject,
                    combatType,
                    CombatHitKind.DamageOverTime,
                    true);
                CombatResolver.TryApplyDamage(gameObject, info);
            }

            if (heal > 0f && _damageable != null && !_damageable.IsDead)
            {
                _damageable.Heal(heal);
            }

            OnBuffTick?.Invoke(buff, dmg, heal);
        }
    }

    // ── 扩展方法 ───────────────────────────────────────────────────────────────

    internal static class PlayerStatBlockExt
    {
        internal static bool HasAnyValue(this PlayerStatBlock b)
            => b.MaxHealth != 0f || b.AttackDamage != 0f ||
               b.Defense   != 0f || b.MoveSpeed    != 0f;
    }
}
