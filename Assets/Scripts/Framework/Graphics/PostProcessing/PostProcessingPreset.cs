using UnityEngine;

namespace Framework.Graphics.PostProcessing
{
    /// <summary>
    /// 后处理预设资产（ScriptableObject）。
    ///
    /// 每个预设描述一组 URP Volume 参数目标值，由
    /// <see cref="PostProcessingController"/> 在过渡协程中插值应用。
    ///
    /// ■ 创建方式：Assets → Create → RPG/Graphics/Post-Processing Preset
    ///
    /// 推荐预设：
    /// <list type="bullet">
    ///   <item>PPPreset_Normal   — 正常游戏状态</item>
    ///   <item>PPPreset_Combat   — 战斗中（提升 Bloom + 轻晕影）</item>
    ///   <item>PPPreset_Boss     — Boss 战（强化色调 + 色差）</item>
    ///   <item>PPPreset_Death    — 玩家死亡（去饱和 + 重晕影）</item>
    ///   <item>PPPreset_Cutscene — 过场（低噪点、深景深）</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(
        fileName = "PPPreset",
        menuName  = "RPG/Graphics/Post-Processing Preset")]
    public sealed class PostProcessingPreset : ScriptableObject
    {
        // ── Bloom ─────────────────────────────────────────────────────────────

        [Header("泛光（Bloom）")]
        [Tooltip("泛光强度。推荐正常 1~2，技能施放 4~8。")]
        [SerializeField, Min(0f)] public float BloomIntensity = 1f;

        [Tooltip("像素亮度必须超过此阈值才产生泛光（0 = 所有像素都泛光）。")]
        [SerializeField, Min(0f)] public float BloomThreshold = 0.9f;

        [Tooltip("泛光散射范围（0 = 紧凑，1 = 弥散）。")]
        [SerializeField, Range(0f, 1f)] public float BloomScatter = 0.7f;

        [Tooltip("泛光色调（白色 = 不偏色）。")]
        [SerializeField] public Color BloomTint = Color.white;

        // ── 色调调整（Color Adjustments） ─────────────────────────────────────

        [Header("色调调整")]
        [Tooltip("饱和度偏移（-100 = 灰度，0 = 原始，100 = 过饱和）。")]
        [SerializeField, Range(-100f, 100f)] public float Saturation = 0f;

        [Tooltip("对比度偏移（负值降低对比，正值提升对比）。")]
        [SerializeField, Range(-100f, 100f)] public float Contrast = 0f;

        [Tooltip("色相偏移（-180~180 度，通常保持 0）。")]
        [SerializeField, Range(-180f, 180f)] public float HueShift = 0f;

        [Tooltip("整体颜色滤镜（白色 = 不偏色，红色 = 暖调，蓝色 = 冷调）。")]
        [SerializeField] public Color ColorFilter = Color.white;

        [Tooltip("曝光补偿（EV，0 = 不调整）。")]
        [SerializeField, Range(-5f, 5f)] public float PostExposure = 0f;

        // ── 晕影（Vignette） ──────────────────────────────────────────────────

        [Header("晕影（Vignette）")]
        [Tooltip("晕影强度（0 = 无，0.6 = 明显）。")]
        [SerializeField, Range(0f, 1f)] public float VignetteIntensity = 0f;

        [Tooltip("晕影颜色（通常为黑色或血红色）。")]
        [SerializeField] public Color VignetteColor = Color.black;

        [Tooltip("晕影羽化柔和度（越大越渐变）。")]
        [SerializeField, Range(0f, 1f)] public float VignetteSmoothness = 0.2f;

        [Tooltip("晕影圆角程度（0 = 椭圆，1 = 矩形）。")]
        [SerializeField, Range(0f, 1f)] public float VignetteRoundness = 1f;

        // ── 色差（Chromatic Aberration） ──────────────────────────────────────

        [Header("色差（Chromatic Aberration）")]
        [Tooltip("色差强度（0 = 无，0.5 = 明显）。用于受击、过渡冲击感。")]
        [SerializeField, Range(0f, 1f)] public float ChromaticIntensity = 0f;

        // ── 胶片噪点（Film Grain） ────────────────────────────────────────────

        [Header("胶片噪点（Film Grain）")]
        [Tooltip("噪点强度（0 = 无，常用于 Boss 或特殊区域氛围）。")]
        [SerializeField, Range(0f, 1f)] public float FilmGrainIntensity = 0f;

        [Tooltip("噪点响应（影响噪点分布，推荐 0.8~1）。")]
        [SerializeField, Range(0f, 1f)] public float FilmGrainResponse = 0.8f;

        // ── 镜头畸变（Lens Distortion） ───────────────────────────────────────

        [Header("镜头畸变（Lens Distortion）")]
        [Tooltip("镜头畸变强度（负值 = 桶形，正值 = 枕形）。")]
        [SerializeField, Range(-0.5f, 0.5f)] public float LensDistortionIntensity = 0f;

        // ── 过渡控制 ──────────────────────────────────────────────────────────

        [Header("过渡")]
        [Tooltip("切换到此预设的默认过渡时长（秒）。")]
        [SerializeField, Min(0f)] public float DefaultTransitionDuration = 0.5f;
    }
}
