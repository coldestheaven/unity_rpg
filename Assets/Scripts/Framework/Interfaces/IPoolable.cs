namespace Framework.Interfaces
{
    /// <summary>
    /// Interface for objects managed by an object pool (Object Pool pattern).
    ///
    /// Implement on any MonoBehaviour that needs to be recycled rather than destroyed.
    /// Called by the pool when the object is retrieved or returned.
    ///
    /// Usage:
    ///   public class EnemyBullet : MonoBehaviour, IPoolable
    ///   {
    ///       public void OnGetFromPool()  { /* reset state, enable colliders */ }
    ///       public void OnReleaseToPool() { /* clear velocity, disable visuals */ }
    ///   }
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called immediately after the object is retrieved from the pool.</summary>
        void OnGetFromPool();

        /// <summary>Called just before the object is returned to the pool.</summary>
        void OnReleaseToPool();
    }
}
