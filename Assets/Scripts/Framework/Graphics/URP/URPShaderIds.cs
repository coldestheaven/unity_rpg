using UnityEngine;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// 缓存 URP 扩展所用的所有 Shader 属性 ID。
    /// 使用 <c>static readonly int</c> 替代运行时 <c>Shader.PropertyToID("...")</c>，
    /// 避免每次 Material.SetXxx 调用时的字符串哈希开销（每次约 60–100 ns）。
    /// </summary>
    internal static class URPShaderIds
    {
        // ── 轮廓（Outline） ────────────────────────────────────────────────────
        internal static readonly int OutlineColor    = Shader.PropertyToID("_OutlineColor");
        internal static readonly int OutlineWidth    = Shader.PropertyToID("_OutlineWidth");
        internal static readonly int MaskTex         = Shader.PropertyToID("_MaskTex");
        internal static readonly int MaskTexelSize   = Shader.PropertyToID("_MaskTexelSize");

        // ── 全屏淡化（Fade） ────────────────────────────────────────────────────
        internal static readonly int FadeColor       = Shader.PropertyToID("_FadeColor");
        internal static readonly int FadeAlpha       = Shader.PropertyToID("_FadeAlpha");
    }
}
