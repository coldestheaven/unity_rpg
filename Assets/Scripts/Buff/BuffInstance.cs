using UnityEngine;

namespace RPG.Buff
{
    /// <summary>
    /// 运行时 Buff 实例 — 由 <see cref="BuffController"/> 持有。
    ///
    /// 包含定义（<see cref="BuffData"/>）、当前计时状态、施加者引用
    /// 和持久化特效的 GameObject 引用。
    /// </summary>
    public sealed class BuffInstance
    {
        // ── 数据 ──────────────────────────────────────────────────────────────

        /// <summary>Buff 的原始数据定义。</summary>
        public readonly BuffData Data;

        /// <summary>施加时的技能等级（影响属性修改量和 tick 伤害）。</summary>
        public readonly int Level;

        /// <summary>施加者（可为 null：来自环境/陷阱等）。</summary>
        public readonly Transform Caster;

        // ── 状态 ──────────────────────────────────────────────────────────────

        /// <summary>剩余持续时间（秒）。≤0 时过期；永久 Buff 保持 float.MaxValue。</summary>
        public float RemainingDuration;

        /// <summary>当前叠层数（Stack 模式有效，其他模式固定为 1）。</summary>
        public int Stacks = 1;

        /// <summary>距下次 tick 触发的倒计时。</summary>
        public float TickTimer;

        // ── 特效引用 ──────────────────────────────────────────────────────────

        /// <summary>持久化特效 GameObject（Buff 移除时会被销毁）。</summary>
        public GameObject PersistentEffect;

        // ── 属性（便捷计算） ──────────────────────────────────────────────────

        public bool IsPermanent    => Data.duration <= 0f;
        public bool IsExpired      => !IsPermanent && RemainingDuration <= 0f;
        public bool HasTick        => Data.tickInterval > 0f &&
                                      (Data.tickDamage > 0f || Data.tickHeal > 0f);

        // ── 构造 ──────────────────────────────────────────────────────────────

        public BuffInstance(BuffData data, int level, Transform caster, float durationOverride = -1f)
        {
            Data             = data;
            Level            = level;
            Caster           = caster;
            RemainingDuration = durationOverride > 0f
                ? durationOverride
                : data.IsPermanent() ? float.MaxValue : data.GetDuration(level);
            TickTimer        = data.tickInterval > 0f ? data.tickInterval : float.MaxValue;
        }

        // ── 操作 ──────────────────────────────────────────────────────────────

        /// <summary>刷新持续时间（Refresh 模式时调用）。</summary>
        public void Refresh(int newLevel = -1)
        {
            int lv = newLevel >= 1 ? newLevel : Level;
            RemainingDuration = IsPermanent ? float.MaxValue : Data.GetDuration(lv);
        }

        /// <summary>增加叠层（Stack 模式时调用）。</summary>
        public bool AddStack()
        {
            if (Stacks >= Data.maxStacks) return false;
            Stacks++;
            return true;
        }
    }

    // ── 扩展方法 ───────────────────────────────────────────────────────────────

    internal static class BuffDataExt
    {
        internal static bool IsPermanent(this BuffData d) => d.duration <= 0f;
    }
}
