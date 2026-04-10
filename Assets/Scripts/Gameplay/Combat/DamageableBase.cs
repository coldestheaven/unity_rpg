using System;
using Framework.Base;
using Framework.Interfaces;
using Framework.Presentation;
using RPG.Simulation;
using UnityEngine;

namespace Gameplay.Combat
{
    /// <summary>
    /// 可接收伤害实体的标记接口。
    /// 通过 <see cref="DamageInfo"/> 传入完整的伤害上下文。
    /// </summary>
    public interface IDamageReceiver
    {
        void ReceiveDamage(DamageInfo damageInfo);
    }

    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 所有可受伤实体的抽象基类。
    ///
    /// <para><b>线程架构（Command 模式，逻辑层与表现层完全分离）：</b><br/>
    /// 伤害 / 治疗请求在主线程捕获快照，随后提交到 <see cref="HealthSimulation"/>（逻辑线程）。
    /// 仿真计算完成后，将结果封装为 <see cref="PresentationCommand"/> 写入
    /// <see cref="PresentationCommandQueue"/>（零 GC，零耦合）。
    /// <see cref="PresentationDispatcher"/> 每帧在主线程取队并调用本类对应的
    /// <see cref="IEntityPresenter"/> 方法，完成 HP 更新与视觉反馈。</para>
    ///
    /// <para><b>降级路径（无仿真实例时）：</b><br/>
    /// 当 <see cref="GameSimulation.Instance"/> 为 null（编辑器测试、初始化阶段）时，
    /// 伤害与治疗在主线程同步计算，不经过逻辑线程。</para>
    ///
    /// <para><b>实体注册：</b><br/>
    /// Awake → <see cref="EntityPresentRegistry.Register"/> ；
    /// OnDestroy → <see cref="EntityPresentRegistry.Unregister"/>。</para>
    /// </summary>
    public abstract class DamageableBase : MonoBehaviourBase, IDamageable, IKillable,
                                           IDamageReceiver, IEntityPresenter
    {
        // ── Inspector 字段 ────────────────────────────────────────────────────

        [SerializeField] protected float maxHealth     = 100f;
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float defense       = 0f;

        // ── 公开属性 ──────────────────────────────────────────────────────────

        public float CurrentHealth => currentHealth;
        public float MaxHealth     => maxHealth;
        public float Defense       => defense;
        public bool  IsDead        { get; protected set; }

        // ── 事件 ──────────────────────────────────────────────────────────────

        /// <summary>成功受伤后触发，参数为最终伤害量。</summary>
        public event Action<float> OnDamaged;
        /// <summary>成功治疗后触发，参数为实际回复量。</summary>
        public event Action<float> OnHealed;
        /// <summary>死亡时触发（每次死亡只触发一次）。</summary>
        public event Action        OnDied;

        // ── 内部字段 ──────────────────────────────────────────────────────────

        private DamageHandler    _damageChain;
        private HealthSimulation _healthSim;
        private int              _entityId;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            _entityId     = gameObject.GetInstanceID();
            _damageChain  = BuildDamageChain();
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
            _healthSim    = new HealthSimulation(maxHealth, currentHealth, _entityId);

            EntityPresentRegistry.Register(_entityId, this);
        }

        protected virtual void OnDestroy()
        {
            EntityPresentRegistry.Unregister(_entityId);
        }

        // ── IEntityPresenter — 主线程回调（由 PresentationDispatcher 驱动） ──

        /// <inheritdoc/>
        public void ApplyDamageResolved(float finalDamage, float remainingHP,
            float srcX, float srcY, float srcZ, int damageType, int hitKind)
        {
            currentHealth = remainingHP;
            NotifyHealthChanged();
            OnDamaged?.Invoke(finalDamage);

            var info = new DamageInfo(
                finalDamage,
                new Vector3(srcX, srcY, srcZ),
                sourceObject: null,
                (CombatDamageType)damageType,
                (CombatHitKind)hitKind);
            OnDamageTaken(finalDamage, info);
        }

        /// <inheritdoc/>
        public void ApplyHealed(float amount, float newHP)
        {
            currentHealth = newHP;
            NotifyHealthChanged();
            OnHealed?.Invoke(amount);
            OnHealedInternal(amount);
        }

        /// <inheritdoc/>
        public void ApplyEntityDied(float killingDamage)
        {
            if (!IsDead) Die();
        }

        /// <inheritdoc/>
        public void ApplyDoTTick(float tickDamage, int remainingTicks)
            => OnDoTTick(tickDamage, remainingTicks);

        // ── 伤害处理管线（可重写） ────────────────────────────────────────────

        /// <summary>
        /// 构建本实体的伤害处理链。默认使用 <see cref="DamagePipeline.BuildDefault"/>。
        /// 重写以安装自定义链（如加入无敌帧节点、元素抗性节点）。
        /// </summary>
        protected virtual DamageHandler BuildDamageChain() => DamagePipeline.BuildDefault();

        // ── 公开入口 ──────────────────────────────────────────────────────────

        /// <summary>简化伤害接口，等价于发送一次物理攻击类型的 <see cref="DamageInfo"/>。</summary>
        public virtual void TakeDamage(float damage, Vector3 attackerPosition)
        {
            ReceiveDamage(DamageInfo.Physical(damage, attackerPosition));
        }

        /// <summary>
        /// 所有入站伤害的统一入口。
        ///
        /// <para>逻辑线程路径：在主线程捕获快照，提交给 <see cref="HealthSimulation"/>；
        /// 结果经 <see cref="PresentationCommandQueue"/> 异步回调（通常在下一帧）。</para>
        ///
        /// <para>降级路径：无仿真实例时，在主线程同步计算并应用。</para>
        /// </summary>
        public virtual void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!CanReceiveDamage(damageInfo)) return;

            var sim = GameSimulation.Instance;
            if (sim != null)
                DispatchDamageToSim(sim, damageInfo);
            else
                ApplyDamageDirect(damageInfo);
        }

        /// <summary>在主线程触发治疗，路径同 <see cref="ReceiveDamage"/>。</summary>
        public virtual void Heal(float amount)
        {
            if (IsDead) return;

            var sim = GameSimulation.Instance;
            if (sim != null)
                DispatchHealToSim(sim, amount);
            else
                ApplyHealDirect(amount);
        }

        /// <summary>立即触发死亡（每次死亡仅执行一次）。</summary>
        public virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDied?.Invoke();
            OnDeathInternal();
        }

        /// <summary>
        /// 复活并恢复到 <paramref name="healthPercent"/> 比例的生命值。
        /// 同步还原仿真层状态（<see cref="HealthSimulation.Restore"/>）。
        /// </summary>
        public virtual void Revive(float healthPercent = 1f)
        {
            IsDead        = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            _healthSim?.Restore(currentHealth, maxHealth);
            NotifyHealthChanged();
            OnRevived();
        }

        /// <summary>将生命值重置为满值（等价于 <c>Revive(1f)</c>，语义更明确）。</summary>
        public virtual void ResetHealth()
        {
            IsDead        = false;
            currentHealth = maxHealth;
            _healthSim?.ResetToFull();
            NotifyHealthChanged();
            OnRevived();
        }

        /// <summary>
        /// 修改最大生命值上限。
        /// <paramref name="restoreToMax"/> = true 时同时将当前 HP 恢复至满值。
        /// </summary>
        public virtual void SetMaxHealth(float value, bool restoreToMax = false)
        {
            maxHealth     = Mathf.Max(1f, value);
            currentHealth = restoreToMax ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
            _healthSim?.SetMax(maxHealth, restoreToMax);
            NotifyHealthChanged();
        }

        /// <summary>设置防御值（最低为 0）。</summary>
        public virtual void SetDefense(float value)
            => defense = Mathf.Max(0f, value);

        // ── 可重写的生命周期钩子 ──────────────────────────────────────────────

        /// <summary>
        /// 判断当前是否可以接受伤害。
        /// <para>注意：<c>_healthSim.IsDead</c> 可能比 <c>IsDead</c> 提前至多 1 帧变为 true
        /// （异步 Command 延迟），这可正确阻挡逻辑层已死亡但表现层尚未更新时的后续伤害。</para>
        /// </summary>
        protected virtual bool CanReceiveDamage(DamageInfo damageInfo)
            => !IsDead && (_healthSim == null || !_healthSim.IsDead);

        /// <summary>通过伤害处理链计算最终伤害量（降级路径使用）。</summary>
        protected virtual float ResolveDamage(DamageInfo damageInfo)
            => _damageChain?.Handle(damageInfo.Amount, damageInfo, this) ?? 0f;

        // ── 子类事件通知（空实现，子类按需重写） ─────────────────────────────

        /// <summary>HP 发生变化后调用（伤害 / 治疗 / 复活均会触发）。</summary>
        protected virtual void NotifyHealthChanged()     { }

        /// <summary>受到伤害后的附加逻辑（击退、受击特效等）。</summary>
        protected virtual void OnDamageTaken(float damage, DamageInfo damageInfo) { }

        /// <summary>治疗后的附加逻辑。</summary>
        protected virtual void OnHealedInternal(float amount)  { }

        /// <summary>DoT 周期跳伤时的附加逻辑。</summary>
        protected virtual void OnDoTTick(float tickDamage, int remainingTicks)    { }

        /// <summary>复活 / 重置血量后的附加逻辑（可用于重启 AI、重置动画等）。</summary>
        protected virtual void OnRevived()               { }

        /// <summary>死亡时的附加逻辑（播放死亡动画、掉落物品等）。</summary>
        protected virtual void OnDeathInternal()         { }

        // ── 私有调度辅助 ─────────────────────────────────────────────────────

        private void DispatchDamageToSim(GameSimulation sim, DamageInfo damageInfo)
        {
            var snapshot  = new EntityCombatSnapshot(this, damageInfo);
            var healthSim = _healthSim;
            sim.EnqueueWork(() => healthSim.ApplyDamage(snapshot));
        }

        private void ApplyDamageDirect(DamageInfo damageInfo)
        {
            float applied = ResolveDamage(damageInfo);
            currentHealth = Mathf.Max(0f, currentHealth - applied);
            NotifyHealthChanged();
            OnDamaged?.Invoke(applied);
            OnDamageTaken(applied, damageInfo);
            if (currentHealth <= 0f) Die();
        }

        private void DispatchHealToSim(GameSimulation sim, float amount)
        {
            var healthSim = _healthSim;
            sim.EnqueueWork(() => healthSim.ApplyHeal(amount));
        }

        private void ApplyHealDirect(float amount)
        {
            float applied = Mathf.Max(0f, amount);
            currentHealth = Mathf.Min(maxHealth, currentHealth + applied);
            NotifyHealthChanged();
            OnHealed?.Invoke(applied);
            OnHealedInternal(applied);
        }
    }
}
