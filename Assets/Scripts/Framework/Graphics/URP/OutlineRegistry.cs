using System.Collections.Generic;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// 当前帧需要渲染轮廓的实体注册表（静态，无 GC 分配）。
    ///
    /// <para>
    /// <see cref="OutlineController"/> 在 <c>OnEnable</c>/<c>OnDisable</c> 时调用
    /// <see cref="Register"/> / <see cref="Unregister"/>；
    /// <see cref="OutlineFeature"/> 的渲染 Pass 在 <c>AddRenderPasses</c> 阶段读取
    /// <see cref="Active"/> 列表并决定是否入队。
    /// </para>
    ///
    /// 线程安全：仅在 Unity 主线程调用（渲染线程不直接访问此类）。
    /// </summary>
    public static class OutlineRegistry
    {
        private static readonly List<OutlineController> _active = new List<OutlineController>(16);

        /// <summary>当前已激活轮廓的控制器只读列表。</summary>
        public static IReadOnlyList<OutlineController> Active => _active;

        /// <summary>当前激活的轮廓控制器数量（0 时 <see cref="OutlineFeature"/> 跳过渲染）。</summary>
        public static int Count => _active.Count;

        /// <summary>注册一个轮廓控制器（重复注册自动忽略）。</summary>
        public static void Register(OutlineController controller)
        {
            if (controller != null && !_active.Contains(controller))
                _active.Add(controller);
        }

        /// <summary>注销一个轮廓控制器。</summary>
        public static void Unregister(OutlineController controller)
            => _active.Remove(controller);

        /// <summary>清空所有注册项（场景卸载时可选调用）。</summary>
        public static void Clear() => _active.Clear();
    }
}
