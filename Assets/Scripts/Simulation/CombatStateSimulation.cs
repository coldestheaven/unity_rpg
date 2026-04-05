using System;
using System.Collections.Generic;

namespace RPG.Simulation
{
    // ──────────────────────────────────────────────────────────────────────────
    // DoT (Damage-over-Time) record
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a single Damage-over-Time effect queued on the logic thread.
    /// All fields are plain value types — no Unity dependency.
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

        /// <summary>
        /// Invoked on the logic thread after each damage tick.
        /// Subscribers MUST marshal to the main thread via MainThreadDispatcher.
        /// Args: (tickDamage, remainingTicks)
        /// </summary>
        public event Action<float, int> OnTick;

        public DoTEffect(string id, HealthSimulation target, float damagePerTick,
                         float tickInterval, int maxTicks)
        {
            Id = id;
            Target = target;
            DamagePerTick = damagePerTick;
            TickInterval = tickInterval;
            MaxTicks = maxTicks;
            TicksRemaining = maxTicks;
        }

        internal void FireTick(float damage) => OnTick?.Invoke(damage, TicksRemaining);
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

        private long _totalHits;
        private float _totalDamageDealt;
        private float _totalHealingDone;
        private long _totalKills;
        private float _peakDamage;

        public long TotalHits { get { lock (_lock) return _totalHits; } }
        public float TotalDamageDealt { get { lock (_lock) return _totalDamageDealt; } }
        public float TotalHealingDone { get { lock (_lock) return _totalHealingDone; } }
        public long TotalKills { get { lock (_lock) return _totalKills; } }
        public float PeakDamage { get { lock (_lock) return _peakDamage; } }

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
                _totalHits = 0;
                _totalDamageDealt = 0f;
                _totalHealingDone = 0f;
                _totalKills = 0;
                _peakDamage = 0f;
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
    ///   3. Hook into <see cref="HealthSimulation"/> events to auto-record stats.
    ///
    /// The simulation does NOT own <see cref="HealthSimulation"/> instances — those are
    /// owned by individual <c>DamageableBase</c> MonoBehaviours.  This class coordinates
    /// cross-entity, time-driven effects only.
    ///
    /// Usage:
    ///   GameSimulation.Instance.Combat.AddDoT(doTEffect);
    ///   GameSimulation.Instance.Combat.Stats.TotalDamageDealt;
    /// </summary>
    public sealed class CombatStateSimulation
    {
        private readonly object _dotLock = new object();
        private readonly List<DoTEffect> _activeDoTs = new List<DoTEffect>(32);
        private readonly List<DoTEffect> _pendingAdd = new List<DoTEffect>(8);
        private readonly List<string> _pendingRemove = new List<string>(8);

        public CombatSessionStats Stats { get; } = new CombatSessionStats();

        // ── DoT management ────────────────────────────────────────────────────

        /// <summary>
        /// Registers a DoT effect.  Thread-safe — can be called from the main thread
        /// (e.g. on skill hit) or from the logic thread.
        /// </summary>
        public void AddDoT(DoTEffect effect)
        {
            if (effect == null) return;
            lock (_dotLock) { _pendingAdd.Add(effect); }
        }

        /// <summary>
        /// Cancels a DoT effect by its <see cref="DoTEffect.Id"/>.  Thread-safe.
        /// </summary>
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
            // Merge pending adds/removes outside iteration
            lock (_dotLock)
            {
                foreach (var effect in _pendingAdd)
                    _activeDoTs.Add(effect);
                _pendingAdd.Clear();

                foreach (var id in _pendingRemove)
                    _activeDoTs.RemoveAll(e => e.Id == id);
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

                    // The DoT damage snapshot has no defense or elemental data;
                    // we apply it as a fixed amount to bypass resistances.
                    // Call ApplyHeal with negative would be wrong — instead we
                    // use a minimal snapshot: 0 defense, multiplier=1, not invincible.
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

            // DoT damage is already the "final resolved" amount decided at application
            // (e.g. spell that already accounted for base power).  Bypass the defense/
            // elemental snapshot so ticks are consistent regardless of target changes.
            dot.Target.ApplyDirectRaw(dmg);

            dot.FireTick(dmg);
            Stats.RecordDamage(dmg);
        }

        // ── Stat hooks ────────────────────────────────────────────────────────

        /// <summary>
        /// Call when a <see cref="HealthSimulation"/> resolves a non-DoT hit so the
        /// global stats are updated automatically.
        /// </summary>
        public void RecordHit(float damage) => Stats.RecordDamage(damage);

        /// <summary>Called when a <see cref="HealthSimulation"/> entity dies.</summary>
        public void RecordKill() => Stats.RecordKill();

        /// <summary>Called when healing is applied to any entity.</summary>
        public void RecordHeal(float amount) => Stats.RecordHeal(amount);
    }
}
