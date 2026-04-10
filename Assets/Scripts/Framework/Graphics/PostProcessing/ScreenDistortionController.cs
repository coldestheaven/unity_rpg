using System.Collections;
using UnityEngine;

namespace Framework.Graphics.PostProcessing
{
    // ============================================================
    // 共享静态状态（ScreenDistortionFeature 读取）
    // ============================================================

    /// <summary>
    /// 屏幕空间扰曲效果共享状态桥。
    ///
    /// <see cref="ScreenDistortionController"/> 修改此状态，
    /// <see cref="ScreenDistortionFeature"/> 每帧读取并提交 GPU 参数。
    /// </summary>
    public static class ScreenDistortionState
    {
        /// <summary>扰曲中心点（归一化屏幕坐标，左下角为原点）。</summary>
        public static Vector2 Center    = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 冲击波半径（归一化，0 = 中心，1 = 屏幕边缘）。
        /// 由 C# 协程每帧递增以模拟向外扩散。
        /// </summary>
        public static float   Radius    = 0f;

        /// <summary>扰曲强度（0 = 无，0.05 = 明显）。</summary>
        public static float   Strength  = 0f;

        /// <summary>冲击波环宽（归一化，越小环越窄）。</summary>
        public static float   RingWidth = 0.08f;

        /// <summary>当 Strength > 阈值时 Feature 才提交渲染通道。</summary>
        public static bool    IsActive  => Strength > 0.001f;

        /// <summary>重置为静止状态。</summary>
        public static void Reset()
        {
            Center    = new Vector2(0.5f, 0.5f);
            Radius    = 0f;
            Strength  = 0f;
            RingWidth = 0.08f;
        }
    }

    // ============================================================
    // 运行时控制器 MonoBehaviour
    // ============================================================

    /// <summary>
    /// 屏幕空间扰曲效果控制器。
    ///
    /// ■ 功能：
    ///   提供基于物理世界坐标或屏幕坐标的冲击波/扰曲触发接口：
    ///   <list type="bullet">
    ///     <item><see cref="TriggerExplosion"/> — 从世界坐标发出的冲击波环</item>
    ///     <item><see cref="TriggerScreenCenter"/> — 屏幕中心的放射状扰曲</item>
    ///     <item><see cref="TriggerDistortion"/> — 自定义全参数扰曲</item>
    ///   </list>
    ///
    /// ■ 前提：
    ///   URP Renderer 已添加 <see cref="ScreenDistortionFeature"/>；
    ///   <c>RPGScreenDistortion.shader</c> 已包含在 "Always Included Shaders" 中。
    ///
    /// ■ 架构：
    ///   通过静态 <see cref="ScreenDistortionState"/> 与 Feature 通信，
    ///   无直接对象引用。
    /// </summary>
    public sealed class ScreenDistortionController : MonoBehaviour
    {
        public static ScreenDistortionController Instance { get; private set; }

        [Header("爆炸冲击波（默认参数）")]
        [Tooltip("冲击波最大扰曲强度（0.02~0.06 为参考范围）。")]
        [SerializeField, Range(0f, 0.2f)] private float _explosionStrength = 0.04f;
        [Tooltip("冲击波扩散至屏幕边缘的时间（秒）。")]
        [SerializeField, Min(0.1f)]       private float _explosionDuration  = 0.5f;
        [Tooltip("冲击波环宽（归一化，越小越窄）。")]
        [SerializeField, Range(0.01f, 0.3f)] private float _explosionRingWidth = 0.08f;

        [Header("全屏震动（默认参数）")]
        [Tooltip("全屏扰曲强度。")]
        [SerializeField, Range(0f, 0.2f)] private float _shakeStrength  = 0.02f;
        [Tooltip("全屏扰曲持续时间（秒）。")]
        [SerializeField, Min(0.1f)]       private float _shakeDuration   = 0.3f;

        private Camera _mainCam;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _mainCam = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ScreenDistortionState.Reset();
            }
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// 从世界坐标位置触发爆炸冲击波（径向扩散环）。
        /// </summary>
        /// <param name="worldPos">爆炸中心世界坐标。</param>
        public void TriggerExplosion(Vector3 worldPos)
        {
            var cam = _mainCam != null ? _mainCam : Camera.main;
            Vector3 screen = cam != null
                ? cam.WorldToViewportPoint(worldPos)
                : new Vector3(0.5f, 0.5f, 1f);

            Vector2 center = new Vector2(
                Mathf.Clamp01(screen.x),
                Mathf.Clamp01(screen.y));

            TriggerDistortion(center, _explosionStrength,
                _explosionDuration, _explosionRingWidth,
                expanding: true);
        }

        /// <summary>
        /// 从屏幕中心触发放射状扰曲（技能施放、强力攻击）。
        /// </summary>
        public void TriggerScreenCenter()
            => TriggerDistortion(new Vector2(0.5f, 0.5f),
                _shakeStrength, _shakeDuration, 0.15f, expanding: false);

        /// <summary>
        /// 自定义扰曲效果。
        /// </summary>
        /// <param name="screenCenter">归一化屏幕坐标（左下角 (0,0)，右上角 (1,1)）。</param>
        /// <param name="strength">扰曲强度。</param>
        /// <param name="duration">持续时间（秒）。</param>
        /// <param name="ringWidth">环宽（归一化）。</param>
        /// <param name="expanding">是否从中心向外扩散（冲击波环）；false = 静态衰减扰曲。</param>
        public void TriggerDistortion(Vector2 screenCenter, float strength,
            float duration, float ringWidth = 0.08f, bool expanding = true)
        {
            StopAllCoroutines();
            StartCoroutine(expanding
                ? ExpandingRingCoroutine(screenCenter, strength, duration, ringWidth)
                : FadeOutCoroutine(screenCenter, strength, duration, ringWidth));
        }

        // ── 私有协程 ──────────────────────────────────────────────────────────

        /// <summary>冲击波环：半径从 0 扩散到 1，同时强度逐渐衰减。</summary>
        private static IEnumerator ExpandingRingCoroutine(
            Vector2 center, float strength, float duration, float ringWidth)
        {
            ScreenDistortionState.Center    = center;
            ScreenDistortionState.RingWidth = ringWidth;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                ScreenDistortionState.Radius   = t;
                // 强度先快速升至峰值，再缓慢衰减，模拟冲击波
                float peakT = Mathf.SmoothStep(0f, 1f, t);
                ScreenDistortionState.Strength = strength * (1f - peakT * peakT);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ScreenDistortionState.Strength = 0f;
        }

        /// <summary>静态扰曲：强度直接从峰值线性衰减到 0。</summary>
        private static IEnumerator FadeOutCoroutine(
            Vector2 center, float strength, float duration, float ringWidth)
        {
            ScreenDistortionState.Center    = center;
            ScreenDistortionState.Radius    = 0f;
            ScreenDistortionState.RingWidth = ringWidth;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                ScreenDistortionState.Strength = strength * (1f - t * t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ScreenDistortionState.Strength = 0f;
        }
    }
}
