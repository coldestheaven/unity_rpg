using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Framework.Threading
{
    /// <summary>
    /// Singleton MonoBehaviour that drains a thread-safe action queue every frame.
    ///
    /// Background threads (e.g. the logic thread) call <see cref="Dispatch"/> to schedule
    /// work that must run on the Unity main thread — such as setting transforms, firing
    /// EventBus events, or touching any other Unity API.
    ///
    /// Architecture:
    ///   Logic Thread  ──Dispatch(action)──►  _pending queue
    ///   Main Thread   ◄── Update() drains ──  _pending queue  →  action()
    ///
    /// Place this component on a persistent GameObject (DontDestroyOnLoad) so it is
    /// always available to receive dispatches between scenes.
    /// </summary>
    public sealed class MainThreadDispatcher : Framework.Base.SingletonMonoBehaviour<MainThreadDispatcher>
    {
        // ConcurrentQueue is the only thread-safe, lock-free option that doesn't
        // block the caller.  Capacity is unbounded but action objects are tiny.
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();

        // Separate static reference so background threads never touch the
        // Unity property (which checks Application.isPlaying under a lock).
        private static MainThreadDispatcher _current;

        protected override void Awake()
        {
            base.Awake();
            _current = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_current == this) _current = null;
        }

        private void Update()
        {
            // Drain all pending actions in a single frame slice.
            // Using a local snapshot count avoids an infinite loop if an action
            // itself enqueues more work (those run next frame).
            int count = _pending.Count;
            for (int i = 0; i < count; i++)
            {
                if (!_pending.TryDequeue(out Action action)) break;
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MainThreadDispatcher] Exception in dispatched action: {e}");
                }
            }
        }

        /// <summary>
        /// Schedules <paramref name="action"/> to run on the Unity main thread on the
        /// next available frame.  Thread-safe — call from any thread.
        /// </summary>
        public static void Dispatch(Action action)
        {
            if (action == null) return;

            if (_current != null)
            {
                _current._pending.Enqueue(action);
            }
            else
            {
                Debug.LogWarning(
                    "[MainThreadDispatcher] Dispatch called but no instance exists. " +
                    "Add MainThreadDispatcher to a persistent scene object.");
            }
        }

        /// <summary>Returns the approximate number of pending actions.</summary>
        public static int PendingCount => _current?._pending.Count ?? 0;
    }
}
