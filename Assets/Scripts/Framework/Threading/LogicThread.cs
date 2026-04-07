using System;
using System.Collections.Concurrent;
using System.Threading;
using Framework.Diagnostics;
using UnityEngine;

namespace Framework.Threading
{
    /// <summary>
    /// Manages a dedicated background thread for game logic processing.
    ///
    /// Work items are submitted via <see cref="Enqueue"/> and executed sequentially in
    /// FIFO order on the background thread.  The thread blocks when idle so it uses no
    /// CPU when there is nothing to process.
    ///
    /// Thread safety:
    ///   - <see cref="Enqueue"/> is thread-safe.
    ///   - <see cref="Dispose"/> is idempotent and waits for the thread to finish (2 s timeout).
    ///
    /// Usage:
    ///   var thread = new LogicThread();
    ///   thread.Start();
    ///   thread.Enqueue(() => { /* pure C# work */ });
    ///   thread.Dispose(); // on application quit
    /// </summary>
    public sealed class LogicThread : IDisposable
    {
        private readonly BlockingCollection<Action> _workQueue;
        private readonly Thread _thread;
        private volatile bool _running;
        private bool _disposed;

        public bool IsRunning => _running;
        public int PendingWorkCount => _workQueue.Count;
        public string ThreadName { get; }

        public LogicThread(string name = "GameLogicThread", int capacity = 512)
        {
            ThreadName = name;
            _workQueue = new BlockingCollection<Action>(capacity);
            _thread = new Thread(RunLoop)
            {
                Name = name,
                IsBackground = true,        // won't prevent application exit
                Priority = ThreadPriority.BelowNormal  // yield to render thread
            };
        }

        /// <summary>Starts the background thread. Call once after construction.</summary>
        public void Start()
        {
            if (_running || _disposed) return;
            _running = true;
            _thread.Start();
        }

        /// <summary>
        /// Enqueues a unit of work.  Returns <c>false</c> if the thread has stopped or
        /// the queue is full (at capacity).  Thread-safe.
        /// </summary>
        public bool Enqueue(Action work)
        {
            if (work == null || !_running || _disposed) return false;
            try
            {
                return _workQueue.TryAdd(work, millisecondsTimeout: 0);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void RunLoop()
        {
            while (_running)
            {
                try
                {
                    // TryTake with timeout allows the loop to check _running periodically
                    // even when the queue is empty.
                    if (_workQueue.TryTake(out Action work, millisecondsTimeout: 100))
                    {
                        ProfilerMarkers.LogicThread_WorkItem.Begin();
                        work();
                        ProfilerMarkers.LogicThread_WorkItem.End();
                    }
                }
                catch (InvalidOperationException)
                {
                    // CompleteAdding was called — drain remaining items then exit.
                    while (_workQueue.TryTake(out Action remaining))
                    {
                        try { remaining(); }
                        catch (Exception e)
                        {
                            Debug.LogError($"[{ThreadName}] Exception while draining: {e}");
                        }
                    }
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{ThreadName}] Unhandled exception: {e}");
                }
            }
        }

        /// <summary>
        /// Stops accepting new work, waits up to 2 s for the thread to finish,
        /// then releases resources.  Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _running = false;

            try { _workQueue.CompleteAdding(); }
            catch (ObjectDisposedException) { }

            if (_thread.IsAlive)
                _thread.Join(millisecondsTimeout: 2000);

            _workQueue.Dispose();
        }
    }
}
