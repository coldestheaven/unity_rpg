using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Framework.Events;
using RPG.Core;

namespace RPG.Scene
{
    // ──────────────────────────────────────────────────────────────────────────
    // SceneController
    //
    // 职责：
    //   • 异步加载/卸载场景（支持淡入淡出过渡）。
    //   • 维护全局 SpawnPoint 注册表，供传送门和存档还原使用。
    //   • 发布 SceneLoadStarted / SceneLoadCompleted / SceneUnloaded 事件。
    //
    // 使用方式：
    //   SceneController.Instance.LoadScene("ForestMap");
    //   SceneController.Instance.LoadScene("DungeonMap", "portal_entrance");
    //   SceneController.Instance.LoadSceneAdditive("UI_Overlay");
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 场景管理器 — 统一处理场景切换、淡入淡出、出生点管理。
    /// </summary>
    public sealed class SceneController : Singleton<SceneController>
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("过渡设置")]
        [SerializeField] private float _fadeDuration = 0.4f;

        [Header("加载界面")]
        [Tooltip("若非 null，加载中显示此 GameObject（进度条面板）。")]
        [SerializeField] private GameObject _loadingScreenPrefab;

        // ── 公开属性 ──────────────────────────────────────────────────────────

        /// <summary>当前正在加载（防止重复触发）。</summary>
        public bool IsLoading { get; private set; }

        /// <summary>0..1，仅在 IsLoading 期间有效。</summary>
        public float LoadingProgress { get; private set; }

        /// <summary>当前活跃场景名称。</summary>
        public string CurrentScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // ── 回调 ─────────────────────────────────────────────────────────────

        public event Action<float>  OnLoadingProgress;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneUnloaded;

        // ── SpawnPoint 注册表（静态，跨场景注册） ────────────────────────────

        private static readonly Dictionary<string, SpawnPoint> _spawnPoints
            = new Dictionary<string, SpawnPoint>(8);

        public static void RegisterSpawnPoint(SpawnPoint sp)
        {
            if (sp == null || string.IsNullOrEmpty(sp.SpawnId)) return;
            _spawnPoints[sp.SpawnId] = sp;
        }

        public static void UnregisterSpawnPoint(SpawnPoint sp)
        {
            if (sp == null || string.IsNullOrEmpty(sp.SpawnId)) return;
            _spawnPoints.Remove(sp.SpawnId);
        }

        /// <summary>返回注册的出生点，不存在时返回 null。</summary>
        public static SpawnPoint GetSpawnPoint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _spawnPoints.TryGetValue(id, out var sp);
            return sp;
        }

        // ── 场景加载（主接口） ────────────────────────────────────────────────

        /// <summary>
        /// 替换式加载场景（Fade → Load → Fade In）。
        /// </summary>
        /// <param name="sceneName">Build Settings 中的场景名称。</param>
        /// <param name="spawnPointId">目标出生点 ID（null = Default）。</param>
        /// <param name="transitionType">传给 SceneLoadStartedEvent 的字符串标签。</param>
        public void LoadScene(string sceneName,
                              string spawnPointId  = null,
                              string transitionType = "fade")
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneController] 已有场景正在加载，忽略本次请求。");
                return;
            }
            StartCoroutine(LoadSceneRoutine(sceneName, spawnPointId, transitionType, additive: false));
        }

        /// <summary>叠加式加载场景（不卸载当前场景）。</summary>
        public void LoadSceneAdditive(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName, null, "additive", additive: true));
        }

        /// <summary>卸载叠加加载的场景。</summary>
        public void UnloadScene(string sceneName)
        {
            StartCoroutine(UnloadSceneRoutine(sceneName));
        }

        // ── 内部协程 ──────────────────────────────────────────────────────────

        private IEnumerator LoadSceneRoutine(string sceneName,
                                             string spawnPointId,
                                             string transitionType,
                                             bool   additive)
        {
            IsLoading       = true;
            LoadingProgress = 0f;

            EventBus.Publish(new SceneLoadStartedEvent(sceneName, transitionType));

            // ① 淡出
            yield return StartCoroutine(FadeOut());

            // ② 显示加载界面
            GameObject loadingScreen = null;
            if (_loadingScreenPrefab != null)
                loadingScreen = Instantiate(_loadingScreenPrefab);

            // ③ 异步加载
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op   = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                LoadingProgress = op.progress;
                OnLoadingProgress?.Invoke(LoadingProgress);
                yield return null;
            }

            LoadingProgress = 1f;
            OnLoadingProgress?.Invoke(1f);

            // ④ 稍等一帧，让加载界面显示 100%
            yield return null;

            // ⑤ 激活场景
            op.allowSceneActivation = true;
            yield return op;

            // ⑥ 销毁加载界面
            if (loadingScreen != null)
                Destroy(loadingScreen);

            // ⑦ 还原出生点
            if (!additive)
                ApplySpawnPoint(spawnPointId);

            // ⑧ 淡入
            yield return StartCoroutine(FadeIn());

            IsLoading = false;

            EventBus.Publish(new SceneLoadCompletedEvent(sceneName));
            OnSceneLoaded?.Invoke(sceneName);
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            if (op == null) yield break;

            while (!op.isDone)
                yield return null;

            EventBus.Publish(new SceneUnloadedEvent(sceneName));
            OnSceneUnloaded?.Invoke(sceneName);
        }

        // ── 出生点还原 ────────────────────────────────────────────────────────

        private static void ApplySpawnPoint(string spawnId)
        {
            // 先尝试具名出生点，回退到 Default
            var sp = GetSpawnPoint(spawnId) ?? GetSpawnPoint("Default");
            if (sp == null) return;

            var pc = Gameplay.Player.PlayerController.Instance;
            if (pc == null)
            {
                Debug.LogWarning("[SceneController] 找不到 PlayerController，无法还原出生点。");
                return;
            }
            pc.transform.SetPositionAndRotation(sp.GetPosition(), sp.GetFacing());
        }

        // ── 淡入淡出（使用 FullscreenFadeController 或降级实现） ─────────────

        private IEnumerator FadeOut()
        {
#if URP_ENABLED
            var fade = Framework.Graphics.PostProcessing.FullscreenFadeController.Instance;
            if (fade != null)
            {
                fade.FadeOut(_fadeDuration);
                yield return new WaitForSeconds(_fadeDuration);
                yield break;
            }
#endif
            yield return new WaitForSeconds(_fadeDuration * 0.5f);
        }

        private IEnumerator FadeIn()
        {
#if URP_ENABLED
            var fade = Framework.Graphics.PostProcessing.FullscreenFadeController.Instance;
            if (fade != null)
            {
                fade.FadeIn(_fadeDuration);
                yield return new WaitForSeconds(_fadeDuration);
                yield break;
            }
#endif
            yield return new WaitForSeconds(_fadeDuration * 0.5f);
        }

        // ── 工具方法 ──────────────────────────────────────────────────────────

        /// <summary>重载当前场景（常用于调试 / 死亡后重玩）。</summary>
        public void ReloadCurrentScene() => LoadScene(CurrentScene);

        /// <summary>安全检查：Build Settings 中是否存在此场景。</summary>
        public static bool SceneExists(string sceneName)
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility
                              .GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return true;
            }
            return false;
        }
    }
}
