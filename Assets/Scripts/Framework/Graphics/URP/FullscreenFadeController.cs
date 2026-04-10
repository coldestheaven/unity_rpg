using System.Collections;
using UnityEngine;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// 驱动 <see cref="FullscreenFadeState"/>，提供协程式的淡入 / 淡出动画。
    ///
    /// <para>
    /// 挂载于场景中的全局管理对象（如 GameManager 所在的 DontDestroyOnLoad 节点）。
    /// 本类不依赖 URP 包，可在 URP_ENABLED 未定义时正常编译（淡化效果无视觉输出，但逻辑不报错）。
    /// </para>
    ///
    /// 使用示例：
    /// <code>
    /// // 1 秒淡入黑屏
    /// yield return StartCoroutine(FadeController.FadeIn(1f));
    ///
    /// // 切换场景后 0.5 秒淡出
    /// yield return StartCoroutine(FadeController.FadeOut(0.5f));
    ///
    /// // 快速设置固定值（无过渡动画）
    /// FadeController.SetInstant(0f);   // 完全透明
    /// FadeController.SetInstant(1f);   // 完全遮挡
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RPG/Graphics/Fullscreen Fade Controller")]
    public sealed class FullscreenFadeController : MonoBehaviour
    {
        [Tooltip("淡入 / 淡出默认用时（秒）。")]
        [SerializeField, Min(0.01f)] private float _defaultDuration = 0.5f;

        [Tooltip("默认淡化颜色。")]
        [SerializeField] private Color _defaultColor = Color.black;

        // 允许外部访问以便于全局引用
        public static FullscreenFadeController Instance { get; private set; }

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                FullscreenFadeState.Reset();
            }
        }

        // ── 协程 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// 从当前透明度过渡到 <paramref name="targetAlpha"/>（协程版，支持 yield return）。
        /// </summary>
        public IEnumerator FadeTo(float targetAlpha, float duration = -1f,
            Color? color = null)
        {
            if (color.HasValue) FullscreenFadeState.Color = color.Value;

            float realDuration = duration > 0 ? duration : _defaultDuration;
            float startAlpha   = FullscreenFadeState.Alpha;
            float elapsed      = 0f;

            while (elapsed < realDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                FullscreenFadeState.Alpha = Mathf.Lerp(startAlpha, targetAlpha,
                    elapsed / realDuration);
                yield return null;
            }

            FullscreenFadeState.Alpha = targetAlpha;
        }

        /// <summary>从透明淡化至完全遮挡（Alpha 0 → 1）。</summary>
        public IEnumerator FadeIn(float duration = -1f, Color? color = null)
        {
            if (color.HasValue) FullscreenFadeState.Color = color.Value;
            else if (FullscreenFadeState.Alpha < 0.01f)
                FullscreenFadeState.Color = _defaultColor;
            yield return FadeTo(1f, duration);
        }

        /// <summary>从完全遮挡淡化至透明（Alpha 1 → 0）。</summary>
        public IEnumerator FadeOut(float duration = -1f)
            => FadeTo(0f, duration);

        // ── 即时控制 ──────────────────────────────────────────────────────────

        /// <summary>立即设置透明度（无过渡动画）。</summary>
        public void SetInstant(float alpha, Color? color = null)
        {
            if (color.HasValue) FullscreenFadeState.Color = color.Value;
            FullscreenFadeState.Alpha = Mathf.Clamp01(alpha);
        }

        /// <summary>停止当前正在运行的淡化协程并立即设置透明度。</summary>
        public void StopAndSet(float alpha)
        {
            StopAllCoroutines();
            SetInstant(alpha);
        }

        // ── 便捷启动方法（非协程，用于普通方法调用） ────────────────────────

        /// <summary>启动淡入协程（无需 yield return）。</summary>
        public void StartFadeIn(float duration = -1f, Color? color = null)
            => StartCoroutine(FadeIn(duration, color));

        /// <summary>启动淡出协程（无需 yield return）。</summary>
        public void StartFadeOut(float duration = -1f)
            => StartCoroutine(FadeOut(duration));
    }
}
