using System;
using System.Diagnostics;
using Framework.Threading;
using UnityEngine;

namespace RPG.Simulation
{
    /// <summary>
    /// Root coordinator for the logic layer.
    ///
    /// Owns and manages the lifecycle of the background <see cref="LogicThread"/> and all
    /// simulation sub-systems.  It self-perpetuates a tick loop at <see cref="TargetTickHz"/>
    /// (default 30 Hz, configurable at runtime via <see cref="SetTickRate"/>).
    ///
    /// Lifetime:
    ///   Created by <c>GameManager.Awake()</c> → started immediately → disposed in
    ///   <c>GameManager.OnDestroy()</c> to cleanly join the background thread.
    ///
    /// Threading contract:
    ///   - Mutations of sub-system state run on the logic thread.
    ///   - Events fired from sub-systems run on the logic thread.
    ///   - Any subscriber that needs to call Unity APIs MUST use
    ///     <see cref="MainThreadDispatcher.Dispatch"/> to marshal back to the main thread.
    ///   - <see cref="ProgressSimulation"/> and <see cref="SkillCooldownSimulation"/>
    ///     are internally locked, so reads from the main thread are safe.
    ///
    /// Usage:
    ///   GameSimulation.Instance.Progress.AddExperience(50f);   // fires on logic thread
    ///   GameSimulation.Instance.EnqueueWork(() => { ... });    // custom logic-thread work
    ///   GameSimulation.Instance.SetTickRate(60);               // change Hz at runtime
    /// </summary>
    public sealed class GameSimulation : IDisposable
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GameSimulation Instance { get; private set; }

        // ── Sub-systems ───────────────────────────────────────────────────────
        public ProgressSimulation Progress { get; }
        public SkillCooldownSimulation Skills { get; }
        public CombatStateSimulation Combat { get; }

        // ── Thread infrastructure ─────────────────────────────────────────────
        private readonly LogicThread _logicThread;
        private readonly Stopwatch _clock;
        private long _lastTickMs;
        private volatile bool _ticking;

        // ── Tick-rate configuration ───────────────────────────────────────────
        // Stored as target interval in milliseconds; volatile so Tick() always
        // reads the latest value without locking.
        private volatile int _targetIntervalMs;

        /// <summary>Current logic thread tick rate in Hz (read-only snapshot).</summary>
        public int TargetTickHz => _targetIntervalMs > 0 ? 1000 / _targetIntervalMs : 0;

        /// <summary>Allowed Hz range for <see cref="SetTickRate"/>.</summary>
        public const int MinTickHz = 1;
        public const int MaxTickHz = 120;

        // ─────────────────────────────────────────────────────────────────────

        /// <param name="skillSlotCount">Number of skill hotbar slots.</param>
        /// <param name="maxMana">Player maximum mana.</param>
        /// <param name="tickRateHz">
        ///   Target logic-thread tick rate in Hz.  Lower values reduce CPU overhead;
        ///   higher values improve responsiveness of cooldown timers and DoT ticks.
        ///   Default: 30 Hz (33 ms interval). Clamped to [<see cref="MinTickHz"/>,
        ///   <see cref="MaxTickHz"/>].
        /// </param>
        public GameSimulation(int skillSlotCount = 4, float maxMana = 100f, int tickRateHz = 30)
        {
            if (Instance != null)
                UnityEngine.Debug.LogWarning(
                    "[GameSimulation] A previous instance was not disposed. Overwriting.");

            Progress = new ProgressSimulation();
            Skills = new SkillCooldownSimulation(skillSlotCount, maxMana);
            Combat = new CombatStateSimulation();
            _logicThread = new LogicThread("GameLogicThread");
            _clock = Stopwatch.StartNew();

            SetTickRate(tickRateHz);
            Instance = this;
        }

        /// <summary>
        /// Changes the logic-thread tick rate at runtime.  Thread-safe.
        /// Takes effect on the next tick cycle (within one current-interval window).
        /// </summary>
        /// <param name="hz">Target rate in Hz, clamped to [<see cref="MinTickHz"/>, <see cref="MaxTickHz"/>].</param>
        public void SetTickRate(int hz)
        {
            int clamped = Math.Max(MinTickHz, Math.Min(MaxTickHz, hz));
            _targetIntervalMs = 1000 / clamped;
            UnityEngine.Debug.Log($"[GameSimulation] Tick rate set to {clamped} Hz ({_targetIntervalMs} ms/tick).");
        }

        /// <summary>
        /// Starts the background thread and begins the self-perpetuating tick loop.
        /// Call once after construction (typically from <c>GameManager.Awake()</c>).
        /// </summary>
        public void Start()
        {
            if (_ticking) return;
            _ticking = true;
            _logicThread.Start();
            ScheduleNextTick();
            UnityEngine.Debug.Log("[GameSimulation] Logic thread started.");
        }

        /// <summary>
        /// Submits arbitrary work to the logic thread.  Thread-safe.
        /// Returns <c>false</c> if the simulation has been disposed or the queue is full.
        /// </summary>
        public bool EnqueueWork(Action work) => _logicThread.Enqueue(work);

        // ── Internal tick loop ────────────────────────────────────────────────

        private void ScheduleNextTick() => _logicThread.Enqueue(Tick);

        private void Tick()
        {
            long nowMs = _clock.ElapsedMilliseconds;
            float deltaSeconds = (nowMs - _lastTickMs) / 1000f;
            _lastTickMs = nowMs;

            // Clamp to avoid huge spikes after pauses or hitches
            deltaSeconds = Math.Min(deltaSeconds, 0.1f);

            // ── Tick all sub-systems ──────────────────────────────────────────
            Skills.Tick(deltaSeconds);
            Combat.Tick(deltaSeconds);
            // Progress has no time-based updates; mutations are event-driven.

            // ── Pace the tick rate ────────────────────────────────────────────
            // Read _targetIntervalMs once — volatile guarantees a fresh value.
            long elapsed = _clock.ElapsedMilliseconds - nowMs;
            int sleepMs = Math.Max(0, _targetIntervalMs - (int)elapsed);
            if (sleepMs > 0)
                System.Threading.Thread.Sleep(sleepMs);

            // ── Schedule next tick (self-perpetuating) ────────────────────────
            if (_ticking)
                ScheduleNextTick();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Stops the tick loop, waits for the background thread to finish, and releases
        /// all resources.  Safe to call from any thread.
        /// </summary>
        public void Dispose()
        {
            _ticking = false;
            _logicThread.Dispose();

            if (Instance == this)
                Instance = null;

            UnityEngine.Debug.Log("[GameSimulation] Logic thread stopped.");
        }
    }
}
