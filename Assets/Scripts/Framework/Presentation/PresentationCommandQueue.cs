using System.Collections.Concurrent;

namespace Framework.Presentation
{
    /// <summary>
    /// Thread-safe FIFO queue that bridges the logic thread and the Unity main thread.
    ///
    /// Design:
    ///   • The logic thread calls <see cref="Enqueue"/> (lock-free, ConcurrentQueue).
    ///   • The main thread calls <see cref="TryDequeue"/> inside
    ///     <see cref="PresentationDispatcher.Update"/>.
    ///   • <see cref="PresentationCommand"/> is a struct — items are stored by value,
    ///     so no boxing and no per-enqueue heap allocation occur.
    ///
    /// The queue is intentionally global (static) so any simulation sub-system can
    /// enqueue commands without holding a reference to a MonoBehaviour.
    /// </summary>
    public static class PresentationCommandQueue
    {
        private static readonly ConcurrentQueue<PresentationCommand> _queue
            = new ConcurrentQueue<PresentationCommand>();

        /// <summary>
        /// Enqueues a command. Thread-safe; call from any thread.
        /// </summary>
        public static void Enqueue(in PresentationCommand command)
            => _queue.Enqueue(command);

        /// <summary>
        /// Attempts to dequeue the next command.
        /// Returns <c>false</c> when the queue is empty.
        /// Call only from the Unity main thread.
        /// </summary>
        public static bool TryDequeue(out PresentationCommand command)
            => _queue.TryDequeue(out command);

        /// <summary>Approximate number of pending commands.</summary>
        public static int Count => _queue.Count;
    }
}
