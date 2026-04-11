using System;
using System.Collections;
using Framework.Core.Patterns;
using UnityEngine;

#if URP_ENABLED
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Framework.Graphics.PostProcessing
{
    /// <summary>
    /// 运行时后处理控制器（单例 MonoBehaviour）。
    ///
    /// ■ 功能：
    ///   1. 管理 URP Global Volume 中的 Bloom / Vignette / ColorAdjustments /
    ///      ChromaticAberration / FilmGrain / LensDistortion 效果参数。
    ///   2. 支持预设切换（<see cref="PostProcessingPreset"/> ScriptableObject）
    ///      并通过协程在指定时长内平滑插值。
    ///   3. 提供脉冲方法（<see cref="PulseBloom"/>、<see cref="PulseChromatic"/>）
    ///      用于技能施放、受击等即时视觉反馈。
    ///   4. 根据血量百分比动态调整晕影强度。
    ///
    /// ■ 使用前提：
    ///   • 场景中需要一个挂载了 Volume 组件的 GameObject，
    ///     Volume 的 Profile 已添加所需的 Volume 效果组件。
    ///   • 已安装 URP 包并添加 URP_ENABLED 宏定义（见 RPG/Graphics/URP Setup）。
    ///
    /// ■ 零依赖降级：
    ///   未定义 URP_ENABLED 时，所有方法均为空操作，代码正常编译。
    /// </summary>
    public sealed class PostProcessingController : Singleton<PostProcessingController>
    {
        // ── Inspector 字段 ────────────────────────────────────────────────────

#if URP_ENABLED
        [Header("URP Volume")]
        [Tooltip("场景中的全局 Volume 组件（通常为 DontDestroyOnLoad 节点上）。")]
        [SerializeField] private Volume _globalVolume;
#endif

        [Header("状态预设")]
        [SerializeField] private PostProcessingPreset _normalPreset;
        [SerializeField] private PostProcessingPreset _combatPreset;
        [SerializeField] private PostProcessingPreset _bossPreset;
        [SerializeField] private PostProcessingPreset _deathPreset;
        [SerializeField] private PostProcessingPreset _cutscenePreset;

        [Header("低血量晕影")]
        [Tooltip("血量低于此比例时开始强化晕影（0~1）。")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold   = 0.35f;
        [Tooltip("血量 = 0 时叠加的最大晕影强度。")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthMaxVignette = 0.55f;
        [Tooltip("低血量晕影颜色（默认暗红色）。")]
        [SerializeField] private Color _lowHealthVignetteColor = new Color(0.5f, 0f, 0f, 1f);

        // ── 运行时状态 ────────────────────────────────────────────────────────

        /// <summary>当前应用的预设（只读）。</summary>
        public PostProcessingPreset CurrentPreset { get; private set; }

        private float _baseVignetteIntensity;
        private float _healthVignetteBonus;

#if URP_ENABLED
        private Bloom               _bloom;
        private Vignette            _vignette;
        private ChromaticAberration _chromatic;
        private ColorAdjustments    _colorAdj;
        private FilmGrain           _filmGrain;
        private LensDistortion      _lensDistortion;
#endif

        // ── 生命周期 ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            CacheEffects();
            if (_normalPreset != null)
                ApplyPresetImmediate(_normalPreset);
        }

        private void CacheEffects()
        {
#if URP_ENABLED
            if (_globalVolume == null || _globalVolume.profile == null) return;
            var profile = _globalVolume.profile;
            profile.TryGet(out _bloom);
            profile.TryGet(out _vignette);
            profile.TryGet(out _chromatic);
            profile.TryGet(out _colorAdj);
            profile.TryGet(out _filmGrain);
            profile.TryGet(out _lensDistortion);
#endif
        }

        // ── 预设切换 ──────────────────────────────────────────────────────────

        /// <summary>平滑过渡到指定预设（使用预设自身的 DefaultTransitionDuration）。</summary>
        public void ApplyPreset(PostProcessingPreset preset)
            => ApplyPreset(preset, preset != null ? preset.DefaultTransitionDuration : 0.5f);

        /// <summary>以指定时长平滑过渡到目标预设。</summary>
        public void ApplyPreset(PostProcessingPreset preset, float duration)
        {
            if (preset == null) return;
            StopAllCoroutines();
            CurrentPreset = preset;
            if (duration <= 0f) { ApplyPresetImmediate(preset); return; }
            StartCoroutine(TransitionCoroutine(preset, duration));
        }

        /// <summary>立即应用预设，无过渡动画。</summary>
        public void ApplyPresetImmediate(PostProcessingPreset preset)
        {
            if (preset == null) return;
            CurrentPreset            = preset;
            _baseVignetteIntensity   = preset.VignetteIntensity;
            WriteValues(preset, lerpT: 1f,
                fromBloom: 0, fromThreshold: 0, fromScatter: 0, fromBloomTint: Color.white,
                fromSat: 0, fromContrast: 0, fromHue: 0, fromFilter: Color.white, fromExp: 0,
                fromVig: 0, fromVigCol: Color.black, fromVigSmooth: 0,
                fromChromatic: 0, fromGrain: 0, fromGrainResp: 0, fromLens: 0);
        }

        // ── 便捷预设切换 ──────────────────────────────────────────────────────

        public void ToNormal  (float duration = -1f) => ApplyPresetWithDuration(_normalPreset,   duration);
        public void ToCombat  (float duration = -1f) => ApplyPresetWithDuration(_combatPreset,   duration);
        public void ToBoss    (float duration = -1f) => ApplyPresetWithDuration(_bossPreset,     duration);
        public void ToDeath   (float duration = -1f) => ApplyPresetWithDuration(_deathPreset,    duration);
        public void ToCutscene(float duration = -1f) => ApplyPresetWithDuration(_cutscenePreset, duration);

        private void ApplyPresetWithDuration(PostProcessingPreset preset, float overrideDuration)
        {
            if (preset == null) return;
            float dur = overrideDuration >= 0f ? overrideDuration : preset.DefaultTransitionDuration;
            ApplyPreset(preset, dur);
        }

        // ── 动态参数控制 ──────────────────────────────────────────────────────

        /// <summary>
        /// 根据血量百分比动态叠加晕影。<br/>
        /// 血量低于 <c>_lowHealthThreshold</c> 时晕影逐渐增强。
        /// </summary>
        /// <param name="healthPercent">[0, 1]，0 = 濒死，1 = 满血。</param>
        public void SetHealthVignette(float healthPercent)
        {
            if (healthPercent >= _lowHealthThreshold)
            {
                _healthVignetteBonus = 0f;
            }
            else
            {
                float t = 1f - healthPercent / _lowHealthThreshold;
                _healthVignetteBonus = t * t * _lowHealthMaxVignette;
            }
            ApplyVignette();
        }

        /// <summary>
        /// 色差瞬间脉冲（技能命中、受击冲击）。
        /// </summary>
        public void PulseChromatic(float peak = 0.7f, float duration = 0.3f)
            => StartCoroutine(PulseCoroutine(duration,
                t => SetChromatic(peak * Mathf.Pow(1f - t, 2f))));

        /// <summary>
        /// Bloom 瞬间脉冲（魔法技能施放的光爆效果）。
        /// </summary>
        public void PulseBloom(float addIntensity = 4f, float duration = 0.4f)
        {
            float baseBloom = CurrentPreset?.BloomIntensity ?? 1f;
            StartCoroutine(PulseCoroutine(duration,
                t => SetBloom(baseBloom + addIntensity * (1f - t))));
        }

        /// <summary>
        /// 晕影脉冲（受击时血色晕影快速闪现后淡出）。
        /// </summary>
        public void PulseVignette(Color color, float addIntensity = 0.5f, float duration = 0.35f)
        {
            float baseVig = _baseVignetteIntensity + _healthVignetteBonus;
            StartCoroutine(PulseCoroutine(duration, t =>
            {
                float v = baseVig + addIntensity * Mathf.Pow(1f - t, 2f);
#if URP_ENABLED
                if (_vignette == null) return;
                _vignette.intensity.Override(Mathf.Clamp01(v));
                _vignette.color.Override(Color.Lerp(color,
                    CurrentPreset?.VignetteColor ?? Color.black, t));
#endif
            }));
        }

        // ── 私有实现 ──────────────────────────────────────────────────────────

        private IEnumerator TransitionCoroutine(PostProcessingPreset dst, float duration)
        {
#if URP_ENABLED
            // 捕获起始值
            float  fromBloom      = _bloom?           .intensity  .value ?? 0f;
            float  fromThreshold  = _bloom?           .threshold  .value ?? 0.9f;
            float  fromScatter    = _bloom?           .scatter    .value ?? 0.7f;
            Color  fromBloomTint  = _bloom != null ? (Color)_bloom.tint.value : Color.white;
            float  fromSat        = _colorAdj?        .saturation .value ?? 0f;
            float  fromContrast   = _colorAdj?        .contrast   .value ?? 0f;
            float  fromHue        = _colorAdj?        .hueShift   .value ?? 0f;
            Color  fromFilter     = _colorAdj != null ? (Color)_colorAdj.colorFilter.value : Color.white;
            float  fromExp        = _colorAdj?        .postExposure.value ?? 0f;
            float  fromVig        = _vignette?        .intensity  .value ?? 0f;
            Color  fromVigCol     = _vignette != null ? (Color)_vignette.color.value : Color.black;
            float  fromVigSmooth  = _vignette?        .smoothness .value ?? 0.2f;
            float  fromChromatic  = _chromatic?       .intensity  .value ?? 0f;
            float  fromGrain      = _filmGrain?       .intensity  .value ?? 0f;
            float  fromGrainResp  = _filmGrain?       .response   .value ?? 0.8f;
            float  fromLens       = _lensDistortion?  .intensity  .value ?? 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                elapsed += Time.deltaTime;
                WriteValues(dst, t, fromBloom, fromThreshold, fromScatter, fromBloomTint,
                    fromSat, fromContrast, fromHue, fromFilter, fromExp,
                    fromVig, fromVigCol, fromVigSmooth, fromChromatic,
                    fromGrain, fromGrainResp, fromLens);
                yield return null;
            }

            // 写入最终精确值
            _baseVignetteIntensity = dst.VignetteIntensity;
            WriteValues(dst, 1f, fromBloom, fromThreshold, fromScatter, fromBloomTint,
                fromSat, fromContrast, fromHue, fromFilter, fromExp,
                fromVig, fromVigCol, fromVigSmooth, fromChromatic,
                fromGrain, fromGrainResp, fromLens);
#else
            yield break;
#endif
        }

        private void WriteValues(PostProcessingPreset dst, float lerpT,
            float fromBloom, float fromThreshold, float fromScatter, Color fromBloomTint,
            float fromSat, float fromContrast, float fromHue, Color fromFilter, float fromExp,
            float fromVig, Color fromVigCol, float fromVigSmooth,
            float fromChromatic, float fromGrain, float fromGrainResp, float fromLens)
        {
#if URP_ENABLED
            float vigTarget = dst.VignetteIntensity + _healthVignetteBonus;

            _bloom?.intensity    .Override(Mathf.Lerp(fromBloom,     dst.BloomIntensity, lerpT));
            _bloom?.threshold    .Override(Mathf.Lerp(fromThreshold, dst.BloomThreshold, lerpT));
            _bloom?.scatter      .Override(Mathf.Lerp(fromScatter,   dst.BloomScatter,   lerpT));
            _bloom?.tint         .Override(Color.Lerp(fromBloomTint, dst.BloomTint,      lerpT));

            _colorAdj?.saturation  .Override(Mathf.Lerp(fromSat,      dst.Saturation,    lerpT));
            _colorAdj?.contrast    .Override(Mathf.Lerp(fromContrast, dst.Contrast,      lerpT));
            _colorAdj?.hueShift    .Override(Mathf.Lerp(fromHue,      dst.HueShift,      lerpT));
            _colorAdj?.colorFilter .Override(Color.Lerp(fromFilter,   dst.ColorFilter,   lerpT));
            _colorAdj?.postExposure.Override(Mathf.Lerp(fromExp,      dst.PostExposure,  lerpT));

            _vignette?.intensity  .Override(Mathf.Lerp(fromVig,      vigTarget,              lerpT));
            _vignette?.color      .Override(Color.Lerp(fromVigCol,   dst.VignetteColor,      lerpT));
            _vignette?.smoothness .Override(Mathf.Lerp(fromVigSmooth,dst.VignetteSmoothness, lerpT));

            _chromatic?.intensity.Override(Mathf.Lerp(fromChromatic, dst.ChromaticIntensity, lerpT));

            _filmGrain?.intensity.Override(Mathf.Lerp(fromGrain,     dst.FilmGrainIntensity, lerpT));
            _filmGrain?.response .Override(Mathf.Lerp(fromGrainResp, dst.FilmGrainResponse,  lerpT));

            _lensDistortion?.intensity.Override(Mathf.Lerp(fromLens, dst.LensDistortionIntensity, lerpT));
#endif
        }

        private void ApplyVignette()
        {
#if URP_ENABLED
            if (_vignette == null) return;
            float finalIntensity = Mathf.Clamp01(_baseVignetteIntensity + _healthVignetteBonus);
            _vignette.intensity.Override(finalIntensity);
            if (_healthVignetteBonus > 0.01f)
                _vignette.color.Override(Color.Lerp(
                    CurrentPreset?.VignetteColor ?? Color.black,
                    _lowHealthVignetteColor,
                    _healthVignetteBonus / _lowHealthMaxVignette));
#endif
        }

        private void SetBloom(float intensity)
        {
#if URP_ENABLED
            _bloom?.intensity.Override(Mathf.Max(0f, intensity));
#endif
        }

        private void SetChromatic(float intensity)
        {
#if URP_ENABLED
            _chromatic?.intensity.Override(Mathf.Clamp01(intensity));
#endif
        }

        private IEnumerator PulseCoroutine(float duration, Action<float> setter)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                setter(elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            setter(1f);
        }
    }
}
