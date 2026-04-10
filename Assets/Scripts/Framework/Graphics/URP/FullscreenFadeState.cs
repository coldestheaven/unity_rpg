using UnityEngine;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// <see cref="FullscreenFadeFeature"/>（ScriptableRendererFeature，ScriptableObject）
    /// 与 <see cref="FullscreenFadeController"/>（MonoBehaviour，场景对象）之间的共享状态桥梁。
    ///
    /// <para>
    /// URP RendererFeature 是 ScriptableObject，无法直接持有 MonoBehaviour 引用；
    /// 此类作为静态单例提供两者之间的运行时通信通道，避免场景引用耦合。
    /// </para>
    ///
    /// 线程安全：仅在 Unity 主线程访问。
    /// </summary>
    public static class FullscreenFadeState
    {
        /// <summary>淡化颜色（RGBA）。</summary>
        public static Color Color = Color.black;

        /// <summary>
        /// 淡化强度 [0, 1]。<br/>
        /// 0 = 完全透明（不影响画面）；1 = 完全覆盖为 <see cref="Color"/>。
        /// </summary>
        public static float Alpha = 0f;

        /// <summary>当 <see cref="Alpha"/> &gt; 0.001 时返回 true（用于提前剔除渲染 Pass）。</summary>
        public static bool IsActive => Alpha > 0.001f;

        /// <summary>重置为默认（透明黑色）。</summary>
        public static void Reset()
        {
            Color = Color.black;
            Alpha = 0f;
        }
    }
}
