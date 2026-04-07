using UnityEngine;

namespace Framework.Core.Utils
{
    /// <summary>
    /// 主线程物理查询工具类 — 所有方法使用 NonAlloc 变体 + 单一共享缓冲区，
    /// 彻底消除 <c>Physics2D.OverlapCircleAll</c> 每次调用产生的 <c>Collider2D[]</c> GC 分配。
    ///
    /// ■ 用法（替换 OverlapCircleAll）：
    /// <code>
    /// int count = PhysicsHelper.OverlapCircle(transform.position, radius, enemyMask);
    /// for (int i = 0; i &lt; count; i++)
    /// {
    ///     var col = PhysicsHelper.Buffer[i];
    ///     // ...处理 col...
    /// }
    /// </code>
    ///
    /// ■ 注意：
    ///   - 仅限主线程调用。
    ///   - 每次调用会覆盖 <see cref="Buffer"/>；不要跨帧或嵌套调用后继续引用旧结果。
    ///   - 缓冲区上限 <see cref="Capacity"/> = 128，足以覆盖常规场景密度。
    ///     如需更大范围批量查询，请自行传入外部数组并直接调用 NonAlloc API。
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>共享缓冲区容量。</summary>
        public const int Capacity = 128;

        /// <summary>共享结果缓冲区（主线程写入，调用方只读）。</summary>
        public static readonly Collider2D[] Buffer = new Collider2D[Capacity];

        // ── 2D ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 主线程 NonAlloc OverlapCircle 查询。返回命中数量；结果写入 <see cref="Buffer"/>。
        /// </summary>
        public static int OverlapCircle(Vector2 center, float radius, int layerMask)
            => Physics2D.OverlapCircleNonAlloc(center, radius, Buffer, layerMask);

        /// <inheritdoc cref="OverlapCircle(Vector2,float,int)"/>
        public static int OverlapCircle(Vector2 center, float radius, LayerMask layerMask)
            => Physics2D.OverlapCircleNonAlloc(center, radius, Buffer, layerMask);

        /// <summary>
        /// 主线程 NonAlloc OverlapCircle 查询（不过滤层）。
        /// </summary>
        public static int OverlapCircle(Vector2 center, float radius)
            => Physics2D.OverlapCircleNonAlloc(center, radius, Buffer);

        // ── 3D（如有需要可扩展）───────────────────────────────────────────────

        /// <summary>
        /// 主线程 NonAlloc OverlapSphere 查询（3D）。返回命中数量；结果写入 <see cref="Buffer3D"/>。
        /// </summary>
        public static readonly Collider[] Buffer3D = new Collider[Capacity];

        /// <inheritdoc cref="Buffer3D"/>
        public static int OverlapSphere(Vector3 center, float radius, int layerMask)
            => Physics.OverlapSphereNonAlloc(center, radius, Buffer3D, layerMask);
    }
}
