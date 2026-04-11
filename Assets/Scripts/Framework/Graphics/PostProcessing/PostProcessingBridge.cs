using Framework.Events;
using UnityEngine;

namespace Framework.Graphics.PostProcessing
{
    /// <summary>
    /// 将 EventBus 游戏事件自动映射到后处理效果变化。
    ///
    /// ■ 职责：
    ///   • 监听 <see cref="PlayerHealthChangedEvent"/> → 动态晕影
    ///   • 监听 <see cref="PlayerDiedEvent"/>          → 死亡灰度后处理
    ///   • 监听 <see cref="GameEndedEvent"/>           → 淡出效果
    ///   • 监听 <see cref="GamePausedEvent"/>          → 色彩减弱
    ///   • 监听 <see cref="SkillUsedEvent"/>           → Bloom 脉冲（终极技能）
    ///   • 监听 <see cref="EnemyDiedEvent"/>           → 受击色差反馈
    ///
    /// ■ 用法：
    ///   将此组件挂载到场景中与 <see cref="PostProcessingController"/> 相同的
    ///   GameObject 上（或任意常驻 GameObject）。
    ///   所有引用均通过 <see cref="PostProcessingController.Instance"/> 访问，
    ///   缺少 Controller 时静默忽略。
    ///
    /// ■ 扩展：
    ///   如需在 Boss 战开始时切换预设，只需在此添加对应事件订阅并调用
    ///   <c>PostProcessingController.Instance?.ToBoss()</c> 即可。
    /// </summary>
    public sealed class PostProcessingBridge : MonoBehaviour
    {
        [Header("URP 技能 Bloom")]
        [Tooltip("终极技能触发的 Bloom 叠加量。")]
        [SerializeField] private float _ultimateBloomAdd     = 5f;
        [Tooltip("普通技能触发的 Bloom 叠加量。")]
        [SerializeField] private float _normalSkillBloomAdd  = 1.5f;
        [Tooltip("Bloom 脉冲持续时间（秒）。")]
        [SerializeField] private float _bloomPulseDuration   = 0.35f;

        [Header("受击晕影")]
        [Tooltip("受击时晕影叠加强度。")]
        [SerializeField] private float _hitVignetteAdd       = 0.35f;
        [Tooltip("受击晕影颜色（默认暗红色）。")]
        [SerializeField] private Color _hitVignetteColor     = new Color(0.6f, 0f, 0f, 1f);
        [Tooltip("受击晕影持续时间（秒）。")]
        [SerializeField] private float _hitVignetteDuration  = 0.25f;

        [Header("受击色差")]
        [Tooltip("受击时色差强度。")]
        [SerializeField] private float _hitChromaticPeak     = 0.5f;
        [Tooltip("受击色差持续时间（秒）。")]
        [SerializeField] private float _hitChromaticDuration = 0.3f;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Subscribe<PlayerDiedEvent>         (OnPlayerDied);
            EventBus.Subscribe<GameEndedEvent>          (OnGameEnded);
            EventBus.Subscribe<GamePausedEvent>         (OnGamePaused);
            EventBus.Subscribe<GameResumedEvent>        (OnGameResumed);
            EventBus.Subscribe<SkillUsedEvent>          (OnSkillUsed);
            EventBus.Subscribe<EnemyDiedEvent>          (OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Unsubscribe<PlayerDiedEvent>         (OnPlayerDied);
            EventBus.Unsubscribe<GameEndedEvent>          (OnGameEnded);
            EventBus.Unsubscribe<GamePausedEvent>         (OnGamePaused);
            EventBus.Unsubscribe<GameResumedEvent>        (OnGameResumed);
            EventBus.Unsubscribe<SkillUsedEvent>          (OnSkillUsed);
            EventBus.Unsubscribe<EnemyDiedEvent>          (OnEnemyDied);
        }

        // ── 事件处理 ──────────────────────────────────────────────────────────

        /// <summary>血量变化 → 更新边缘晕影强度。</summary>
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (evt.MaxHealth <= 0f) return;
            float pct = Mathf.Clamp01(evt.CurrentHealth / evt.MaxHealth);
            PostProcessingController.Instance?.SetHealthVignette(pct);

            // 受到伤害时（血量降低）同时触发受击晕影和色差脉冲
            if (pct < 1f)
            {
                PostProcessingController.Instance?.PulseVignette(
                    _hitVignetteColor, _hitVignetteAdd, _hitVignetteDuration);
                PostProcessingController.Instance?.PulseChromatic(
                    _hitChromaticPeak, _hitChromaticDuration);
            }
        }

        /// <summary>玩家死亡 → 切换为死亡预设（去饱和 + 重度晕影）。</summary>
        private void OnPlayerDied(PlayerDiedEvent _)
            => PostProcessingController.Instance?.ToDeath();

        /// <summary>游戏结束 → 恢复正常预设。</summary>
        private void OnGameEnded(GameEndedEvent _)
            => PostProcessingController.Instance?.ToNormal(duration: 1.5f);

        /// <summary>游戏暂停 → 轻微色差效果（提示状态变化）。</summary>
        private void OnGamePaused(GamePausedEvent _)
            => PostProcessingController.Instance?.PulseChromatic(0.15f, 0.2f);

        /// <summary>游戏继续 → 恢复正常预设。</summary>
        private void OnGameResumed(GameResumedEvent _)
            => PostProcessingController.Instance?.ToNormal(duration: 0.3f);

        /// <summary>技能使用 → Bloom 脉冲（终极技能更强）。</summary>
        private void OnSkillUsed(SkillUsedEvent evt)
        {
            float addBloom = evt.IsUltimate ? _ultimateBloomAdd : _normalSkillBloomAdd;
            PostProcessingController.Instance?.PulseBloom(addBloom, _bloomPulseDuration);
        }

        /// <summary>敌人死亡 → 轻微色差反馈（击杀爽快感）。</summary>
        private void OnEnemyDied(EnemyDiedEvent _)
            => PostProcessingController.Instance?.PulseChromatic(0.25f, 0.2f);
    }
}
