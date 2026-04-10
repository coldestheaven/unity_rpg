using System.Collections;
using UnityEngine;

namespace Framework.Graphics.PostProcessing
{
    // ============================================================
    // 共享静态状态（HitFlashFeature ScriptableRendererFeature 读取）
    // ============================================================

    /// <summary>
    /// 受击闪光效果共享状态桥。
    ///
    /// <see cref="HitFlashController"/>（MonoBehaviour）通过此类修改参数，
    /// <see cref="HitFlashFeature"/>（ScriptableRendererFeature）读取并渲染。
    /// 二者解耦，无需直接引用。
    /// </summary>
    public static class HitFlashState
    {
        /// <summary>当前闪光颜色。</summary>
        public static Color  FlashColor     = Color.white;

        /// <summary>
        /// 当前闪光强度 [0, 1]。
        /// 0 = 完全透明（不渲染），1 = 全屏覆盖该颜色。
        /// </summary>
        public static float  FlashIntensity = 0f;

        /// <summary>当 Intensity > 阈值时 Feature 才提交渲染通道。</summary>
        public static bool   IsActive       => FlashIntensity > 0.002f;

        /// <summary>重置为不活跃状态。</summary>
        public static void Reset()
        {
            FlashColor     = Color.white;
            FlashIntensity = 0f;
        }
    }

    // ============================================================
    // 运行时控制器 MonoBehaviour
    // ============================================================

    /// <summary>
    /// 受击全屏闪光效果控制器。
    ///
    /// ■ 功能：
    ///   提供三种预置触发快捷方法：
    ///   <list type="bullet">
    ///     <item><see cref="TriggerHit"/>   — 普通受击（白色）</item>
    ///     <item><see cref="TriggerCrit"/>  — 暴击（橙黄色，更强）</item>
    ///     <item><see cref="TriggerDeath"/> — 死亡（红色渐出）</item>
    ///     <item><see cref="TriggerHeal"/>  — 治疗（绿色，更柔）</item>
    ///     <item><see cref="TriggerFlash"/> — 自定义颜色 + 强度 + 时长</item>
    ///   </list>
    ///
    /// ■ 前提：
    ///   URP Renderer 已添加 <see cref="HitFlashFeature"/>；
    ///   <c>RPGHitFlash.shader</c> 已包含在 "Always Included Shaders" 中。
    ///
    /// ■ 架构：
    ///   通过静态 <see cref="HitFlashState"/> 与 <see cref="HitFlashFeature"/>
    ///   通信，无直接对象引用，适合跨场景使用。
    /// </summary>
    public sealed class HitFlashController : MonoBehaviour
    {
        public static HitFlashController Instance { get; private set; }

        [Header("普通受击")]
        [SerializeField] private Color  _hitColor         = Color.white;
        [SerializeField, Range(0f, 1f)] private float _hitPeak   = 0.45f;
        [SerializeField, Min(0f)]       private float _hitDur    = 0.22f;

        [Header("暴击")]
        [SerializeField] private Color  _critColor        = new Color(1f, 0.8f, 0.1f);
        [SerializeField, Range(0f, 1f)] private float _critPeak  = 0.65f;
        [SerializeField, Min(0f)]       private float _critDur   = 0.30f;

        [Header("死亡")]
        [SerializeField] private Color  _deathColor       = new Color(0.6f, 0f, 0f);
        [SerializeField, Range(0f, 1f)] private float _deathPeak = 0.75f;
        [SerializeField, Min(0f)]       private float _deathDur  = 0.60f;

        [Header("治疗")]
        [SerializeField] private Color  _healColor        = new Color(0.3f, 1f, 0.4f);
        [SerializeField, Range(0f, 1f)] private float _healPeak  = 0.30f;
        [SerializeField, Min(0f)]       private float _healDur   = 0.35f;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                HitFlashState.Reset();
            }
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>普通受击闪光（白色）。</summary>
        public void TriggerHit()
            => TriggerFlash(_hitColor, _hitPeak, _hitDur);

        /// <summary>暴击闪光（橙黄色，更强更久）。</summary>
        public void TriggerCrit()
            => TriggerFlash(_critColor, _critPeak, _critDur);

        /// <summary>死亡闪光（暗红色，较长渐出）。</summary>
        public void TriggerDeath()
            => TriggerFlash(_deathColor, _deathPeak, _deathDur, curve: FlashCurve.Linear);

        /// <summary>治疗闪光（绿色，柔和）。</summary>
        public void TriggerHeal()
            => TriggerFlash(_healColor, _healPeak, _healDur, curve: FlashCurve.EaseOut);

        /// <summary>
        /// 自定义闪光效果。
        /// </summary>
        /// <param name="color">闪光颜色。</param>
        /// <param name="peak">峰值强度 [0, 1]。</param>
        /// <param name="duration">总持续时间（秒）。</param>
        /// <param name="curve">亮度衰减曲线。</param>
        public void TriggerFlash(Color color, float peak = 0.4f, float duration = 0.25f,
                                 FlashCurve curve = FlashCurve.QuadOut)
        {
            StopAllCoroutines();
            StartCoroutine(FlashCoroutine(color, peak, duration, curve));
        }

        // ── 私有实现 ──────────────────────────────────────────────────────────

        private static IEnumerator FlashCoroutine(
            Color color, float peak, float duration, FlashCurve curve)
        {
            HitFlashState.FlashColor = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                HitFlashState.FlashIntensity = peak * EvaluateCurve(curve, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            HitFlashState.FlashIntensity = 0f;
        }

        private static float EvaluateCurve(FlashCurve curve, float t)
        {
            return curve switch
            {
                FlashCurve.Linear  => 1f - t,
                FlashCurve.QuadOut => (1f - t) * (1f - t),
                FlashCurve.EaseOut => Mathf.Pow(1f - t, 3f),
                _                  => 1f - t,
            };
        }
    }

    /// <summary>闪光强度随时间的衰减曲线类型。</summary>
    public enum FlashCurve
    {
        /// <summary>线性衰减，均匀淡出。</summary>
        Linear,
        /// <summary>二次衰减，前期快后期慢（受击常用）。</summary>
        QuadOut,
        /// <summary>三次衰减，更柔和的淡出（治疗常用）。</summary>
        EaseOut,
    }
}
