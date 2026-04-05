using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Buff
{
    /// <summary>
    /// 任何能接受 Buff / Debuff 的实体实现此接口。
    /// <see cref="BuffController"/> 是标准实现；也可自定义用于 Boss、环境等。
    /// </summary>
    public interface IBuffReceiver
    {
        // ── 查询 ──────────────────────────────────────────────────────────────

        bool HasBuff(string buffId);
        BuffInstance GetBuff(string buffId);
        IReadOnlyList<BuffInstance> ActiveBuffs { get; }

        // ── 施加 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 施加 Buff。根据 <see cref="BuffData.stackMode"/> 决定叠加行为。
        /// </summary>
        /// <param name="data">Buff 定义资产。</param>
        /// <param name="level">施加时的技能等级（影响数值成长）。</param>
        /// <param name="caster">施加者（可为 null）。</param>
        /// <param name="durationOverride">覆盖持续时间（≤0 = 使用 BuffData.duration）。</param>
        /// <returns>施加/更新后的 Buff 实例；如被 Ignore 则返回现有实例。</returns>
        BuffInstance ApplyBuff(BuffData data,
                               int level            = 1,
                               Transform caster     = null,
                               float durationOverride = -1f);

        // ── 移除 ──────────────────────────────────────────────────────────────

        bool RemoveBuff(string buffId);
        void RemoveAllBuffs();
        void RemoveAllDebuffs();
        void RemoveAllByCategory(BuffCategory category);

        // ── 事件 ──────────────────────────────────────────────────────────────

        event Action<BuffInstance> OnBuffApplied;
        event Action<BuffInstance> OnBuffRemoved;

        /// <summary>每次 tick 效果触发时触发（buff实例, 本次伤害, 本次治疗）。</summary>
        event Action<BuffInstance, float, float> OnBuffTick;
    }
}
