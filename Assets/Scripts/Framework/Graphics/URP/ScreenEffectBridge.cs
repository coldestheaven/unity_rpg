using System.Collections;
using Framework.Events;
using UnityEngine;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// 将 <see cref="EventBus"/> 游戏事件映射到 URP 屏幕特效（淡化 / 轮廓）。
    ///
    /// <para>
    /// 职责单一：监听游戏事件 → 调用 <see cref="FullscreenFadeController"/> 或
    /// 其他 URP 控制器，无直接渲染逻辑。
    /// </para>
    ///
    /// 挂载位置：与 GameManager、<see cref="FullscreenFadeController"/> 相同的
    /// DontDestroyOnLoad GameObject。
    ///
    /// 已处理的事件：
    /// <list type="table">
    ///   <item><see cref="GameEventId.GameEnded"/>  → 慢速黑屏淡入</item>
    ///   <item><see cref="GameEventId.GameLoaded"/> → 从黑屏淡出</item>
    ///   <item><see cref="GameEventId.PlayerDied"/> → 红色短闪 + 黑屏</item>
    ///   <item><see cref="GameEventId.GamePaused"/> → 淡灰色半透明叠加</item>
    ///   <item><see cref="GameEventId.GameResumed"/>→ 清除叠加</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RPG/Graphics/Screen Effect Bridge")]
    [RequireComponent(typeof(FullscreenFadeController))]
    public sealed class ScreenEffectBridge : MonoBehaviour
    {
        [Header("淡化时长（秒）")]
        [SerializeField, Min(0f)] private float _gameOverFadeDuration  = 1.5f;
        [SerializeField, Min(0f)] private float _loadFadeOutDuration   = 0.8f;
        [SerializeField, Min(0f)] private float _deathFlashDuration    = 0.4f;
        [SerializeField, Min(0f)] private float _pauseFadeDuration     = 0.3f;

        [Header("淡化颜色")]
        [SerializeField] private Color _gameOverColor  = new Color(0.05f, 0f, 0f, 1f);
        [SerializeField] private Color _deathFlashColor= new Color(0.8f, 0f, 0f, 0.55f);
        [SerializeField] private Color _pauseColor     = new Color(0f, 0f, 0f, 0.4f);

        private FullscreenFadeController _fade;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
            => _fade = GetComponent<FullscreenFadeController>();

        private void OnEnable()
        {
            EventBus.Subscribe<GameEndedEvent>  (OnGameEnded);
            EventBus.Subscribe<GameLoadedEvent> (OnGameLoaded);
            EventBus.Subscribe<PlayerDiedEvent> (OnPlayerDied);
            EventBus.Subscribe<GamePausedEvent> (OnGamePaused);
            EventBus.Subscribe<GameResumedEvent>(OnGameResumed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEndedEvent>  (OnGameEnded);
            EventBus.Unsubscribe<GameLoadedEvent> (OnGameLoaded);
            EventBus.Unsubscribe<PlayerDiedEvent> (OnPlayerDied);
            EventBus.Unsubscribe<GamePausedEvent> (OnGamePaused);
            EventBus.Unsubscribe<GameResumedEvent>(OnGameResumed);
        }

        // ── 事件处理 ──────────────────────────────────────────────────────────

        private void OnGameEnded(GameEndedEvent _)
            => _fade.StartFadeIn(_gameOverFadeDuration, _gameOverColor);

        private void OnGameLoaded(GameLoadedEvent _)
        {
            // 先确保初始黑屏，再淡出（防止画面闪烁）
            _fade.StopAndSet(1f);
            _fade.StartFadeOut(_loadFadeOutDuration);
        }

        private void OnPlayerDied(PlayerDiedEvent _)
            => StartCoroutine(DeathFlashSequence());

        private void OnGamePaused(GamePausedEvent _)
            => _fade.StartFadeIn(_pauseFadeDuration, _pauseColor);

        private void OnGameResumed(GameResumedEvent _)
            => _fade.StartFadeOut(_pauseFadeDuration);

        // ── 复合序列 ──────────────────────────────────────────────────────────

        /// <summary>死亡序列：红色短闪 → 黑屏（持续至复活或游戏结束时 FadeOut）。</summary>
        private IEnumerator DeathFlashSequence()
        {
            // 快速红色淡入
            yield return _fade.FadeIn(_deathFlashDuration, _deathFlashColor);

            // 短暂保持红色
            yield return new WaitForSecondsRealtime(_deathFlashDuration);

            // 过渡到黑屏（等待复活事件触发 FadeOut）
            yield return _fade.FadeTo(1f, _deathFlashDuration * 2f, Color.black);
        }
    }
}
