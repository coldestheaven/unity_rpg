using UnityEngine;

namespace Framework.Graphics
{
    /// <summary>
    /// Shader Warmup 配置资产（ScriptableObject）。
    ///
    /// 在 Inspector 中将项目所有 <see cref="ShaderVariantCollection"/> 拖入
    /// <see cref="collections"/> 数组，然后由 <see cref="ShaderManager"/> 在
    /// Loading 界面完成预热，消除首次渲染时的 Shader 编译卡帧。
    ///
    /// ■ 创建方式：Assets → Create → RPG/Graphics/Shader Warmup Config
    /// ■ 使用方式：将资产拖入场景中 ShaderManager 的 Config 字段；
    ///   或放在 <c>Resources/Graphics/ShaderWarmupConfig.asset</c> 自动加载。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ShaderWarmupConfig",
        menuName  = "RPG/Graphics/Shader Warmup Config")]
    public sealed class ShaderWarmupConfig : ScriptableObject
    {
        // ── 配置字段 ──────────────────────────────────────────────────────────

        [Header("Shader Variant Collections")]
        [Tooltip("按优先级顺序排列。Progressive 模式下每帧预热一个 Collection。")]
        [SerializeField] public ShaderVariantCollection[] collections
            = System.Array.Empty<ShaderVariantCollection>();

        [Header("Warmup Mode")]
        [Tooltip("Immediate：启动时一次性预热（可能短暂卡顿）。\n" +
                 "Progressive：逐帧预热，适合在 Loading 界面显示进度条。")]
        [SerializeField] public WarmupMode mode = WarmupMode.Progressive;

        [Tooltip("每帧最多预热的 Collection 数量（Progressive 模式有效）。\n" +
                 "1 = 最平滑；增大可缩短总预热时间。")]
        [SerializeField, Range(1, 8)]
        public int collectionsPerFrame = 1;

        [Tooltip("场景启动时自动开始预热（挂载 ShaderManager 后无需手动调用）。")]
        [SerializeField] public bool warmupOnStart = true;

        // ── 枚举 ─────────────────────────────────────────────────────────────

        public enum WarmupMode
        {
            /// <summary>当帧内一次性完成所有 Collection 的预热，简单但可能卡一帧。</summary>
            Immediate,
            /// <summary>逐帧预热，适合在 Loading 界面以进度条展示。</summary>
            Progressive,
        }
    }
}
