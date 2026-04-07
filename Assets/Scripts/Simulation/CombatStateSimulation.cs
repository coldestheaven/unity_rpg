using System.Collections.Generic;
using Framework.Presentation;

namespace RPG.Simulation
{
    // ──────────────────────────────────────────────────────────────────────────
    // DoT (Damage-over-Time) record
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a single Damage-over-Time effect managed on the logic thread.
    /// All fields are plain value types — no Unity dependency.
    ///
    /// Presentation coupling is handled by <see cref="CombatStateSimulation.ApplyDoTTick"/>
    /// which enqueues a <see cref="PresCommandId.DoTTick"/> command after each damage
    /// application.  No C# events required.
    /// </summary>
    public sealed class DoTEffect
    {
        public readonly string Id;
        public readonly HealthSimulation Target;
        public readonly float DamagePerTick;
        public readonly float TickInterval;
        public readonly int MaxTicks;
        public int TicksRemaining;
        public float TimeSinceLastTick;

        public DoTEffect(string id, HealthSimulation target, float damagePerTick,
                         float tickInterval, int maxTicks)
        {
            Id             = id;
            Target         = target;
            DamagePerTick  = damagePerTick;
            TickInterval   = tickInterval;
            MaxTicks       = maxTicks;
            TicksRemaining = maxTicks;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CombatSessionStats — thread-safe cumulative statistics
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulated per-session combat statistics.
    /// Updated on the logic thread; read accessors are lock-protected.
    /// </summary>
    public sealed class CombatSessionStats
    {
        private readonly object _lock = new object();

        private long  _totalHits;
        private float _totalDamageDealt;
        private float _totalHealingDone;
        private long  _totalKills;
        private float _peakDamage;

        public long  TotalHits        { get { lock (_lock) return _totalHits; } }
        public float TotalDamageDealt { get { lock (_lock) return _totalDamageDealt; } }
        public float TotalHealingDone { get { lock (_lock) return _totalHealingDone; } }
        public long  TotalKills       { get { lock (_lock) return _totalKills; } }
        public float PeakDamage       { get { lock (_lock) return _peakDamage; } }

        internal void RecordDamage(float amount)
        {
            lock (_lock)
            {
                _totalHits++;
                _totalDamageDealt += amount;
                if (amount > _peakDamage) _peakDamage = amount;
            }
        }

        internal void RecordHeal(float amount)
        {
            lock (_lock) { _totalHealingDone += amount; }
        }

        internal void RecordKill()
        {
            lock (_lock) { _totalKills++; }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _totalHits        = 0;
                _totalDamageDealt = 0f;
                _totalHealingDone = 0f;
                _totalKills       = 0;
                _peakDamage       = 0f;
            }
        }

        public (long hits, float damage, float healing, long kills, float peak) GetSnapshot()
        {
            lock (_lock)
                return (_totalHits, _totalDamageDealt, _totalHealingDone, _totalKills, _peakDamage);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CombatStateSimulation
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Global combat coordinator that runs on the logic thread.
    ///
    /// Responsibilities:
    ///   1. Tick active DoT effects and route damage through <see cref="HealthSimulation"/>.
    ///   2. Accumulate session statistics (hits, damage dealt, kills) without blocking render.
    ///
    /// Presentation coupling (Command pattern):
    ///   After each DoT tick, a <see cref="PresCommandId.DoTTick"/> command is enqueued
    ///   to <see cref="PresentationCommandQueue"/>.  No C# events, no subscribers,
    ///   no MainThreadDispatcher calls.
    /// </summary>
    public sealed class CombatStateSimulation
    {
        private readonly object _dotLock = new object();
        private readonly List<DoTEffect> _activeDoTs  = new List<DoTEffect>(32);
        private readonly List<DoTEffect> _pendingAdd  = new List<DoTEffect>(8);
        private readonly List<string>   _pendingRemove = new List<string>(8);

        public CombatSessionStats Stats { get; } = new CombatSessionStats();

        // ── DoT management ────────────────────────────────────────────────────

        /// <summary>Registers a DoT effect. Thread-safe.</summary>
        public void AddDoT(DoTEffect effect)
        {
            if (effect == null) return;
            lock (_dotLock) { _pendingAdd.Add(effect); }
        }

        /// <summary>Cancels a DoT effect by its <see cref="DoTEffect.Id"/>. Thread-safe.</summary>
        public void RemoveDoT(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (_dotLock) { _pendingRemove.Add(id); }
        }

        // ── Tick (called by GameSimulation each logic frame) ──────────────────

        /// <summary>
        /// Advances all active DoT effects by <paramref name="deltaTime"/> seconds.
        /// Called on the logic thread; must not touch Unity APIs.
        /// </summary>
        public void Tick(float deltaTime)
        {
            lock (_dotLock)
            {
                foreach (var effect in _pendingAdd)
                    _activeDoTs.Add(effect);
                _pendingAdd.Clear();

                // 无 lambda / RemoveAll 分配：倒序遍历，O(n) swap-remove
                for (int ri = 0; ri < _pendingRemove.Count; ri++)
                {
                    string removeId = _pendingRemove[ri];
                    for (int di = _activeDoTs.Count - 1; di >= 0; di--)
                    {
                        if (_activeDoTs[di].Id == removeId)
                        {
                            _activeDoTs[di] = _activeDoTs[_activeDoTs.Count - 1];
                            _activeDoTs.RemoveAt(_activeDoTs.Count - 1);
                        }
                    }
                }
                _pendingRemove.Clear();
            }

            for (int i = _activeDoTs.Count - 1; i >= 0; i--)
            {
                var dot = _activeDoTs[i];
                dot.TimeSinceLastTick += deltaTime;

                if (dot.TimeSinceLastTick >= dot.TickInterval)
                {
                    dot.TimeSinceLastTick -= dot.TickInterval;
                    dot.TicksRemaining--;
                    ApplyDoTTick(dot);

                    if (dot.TicksRemaining <= 0)
                        _activeDoTs.RemoveAt(i);
                }
            }
        }

        private void ApplyDoTTick(DoTEffect dot)
        {
            if (dot.Target == null || dot.Target.IsDead) return;

            float dmg = dot.DamagePerTick;

            // ApplyDirectRaw bypasses defense/elemental — DoT damage is already the
            // resolved final amount decided at application time.
            // Note: ApplyDirectRaw does NOT record stats; we record them here to avoid
            // double-counting (ApplyDamage records via RecordHit, but ApplyDirectRaw
            // intentionally skips stats so the coordinator controls them).
            dot.Target.ApplyDirectRaw(dmg);
            Stats.RecordDamage(dmg);

            // Enqueue DoT-specific presentation command (e.g. ticking particle, remaining HUD)
            PresentationCommandQueue.Enqueue(
                PresentationCommand.DoTTick(dot.Target.EntityId, dmg, dot.TicksRemaining));
        }

        // ── Stat hooks (called by HealthSimulation on the logic thread) ───────

        public void RecordHit(float damage)  => Stats.RecordDamage(damage);
        public void RecordKill()             => Stats.RecordKill();
        public void RecordHeal(float amount) => Stats.RecordHeal(amount);
    }
}
