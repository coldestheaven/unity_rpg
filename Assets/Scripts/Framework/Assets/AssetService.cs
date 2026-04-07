using System;
using System.Collections;
using UnityEngine;

namespace Framework.Assets
{
    /// <summary>
    /// 全局资源加载服务入口（服务定位器模式）。
    ///
    /// ■ 默认使用 <see cref="ResourcesAssetLoader"/>，无需任何配置即可工作。
    /// ■ 切换到 Addressables：
    ///   1. Package Manager 安装 com.unity.addressables（1.17+）
    ///   2. Player Settings → Scripting Define Symbols 添加 ADDRESSABLES_ENABLED
    ///   3. GameManager.Awake() 中调用 <see cref="UseAddressables"/>
    ///
    /// 用法：
    /// <code>
    ///   // 同步（ScriptableObject 数据、小型资产）
    ///   var db = AssetService.Load&lt;ItemDatabase&gt;(AssetPaths.Data.ItemDatabase);
    ///
    ///   // 异步（音频、大型贴图）
    ///   yield return AssetService.LoadAsync&lt;AudioClip&gt;(AssetPaths.Audio.BgmMain, c => _bgm = c);
    ///
    ///   // Addressables 预热（Loading 界面使用）
    ///   yield return AssetService.PreloadAsync(AssetPaths.Audio.BgmMain);
    ///
    ///   // 场景切换后释放
    ///   AssetService.ReleaseAll();
    /// </code>
    /// </summary>
    public static class AssetService
    {
        private static IAssetLoader _loader;

        /// <summary>
        /// 当前活跃的资源加载器。
        /// 未设置时懒加载默认的 <see cref="ResourcesAssetLoader"/>。
        /// </summary>
        public static IAssetLoader Loader => _loader ??= new ResourcesAssetLoader();

        // ── 配置 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 替换底层加载实现。应在游戏启动（Awake/初始化）阶段调用一次。
        /// </summary>
        /// <param name="loader">新的 <see cref="IAssetLoader"/> 实现，不可为 null。</param>
        public static void SetLoader(IAssetLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader),
                "[AssetService] loader 不可为 null。");
        }

        /// <summary>
        /// 编辑器下使用 <see cref="EditorSimulatedAssetLoader"/>，
        /// 无需构建 AssetBundle 即可模拟加载（基于 AssetDatabase）。
        /// Player 构建中调用此方法会打印错误并回退到 Resources 加载器。
        /// </summary>
        /// <param name="verbose">是否打印每次加载的详细日志（调试路径映射时开启）。</param>
        /// <example>
        /// <code>
        ///   // GameManager.Awake()
        ///   #if UNITY_EDITOR
        ///       AssetService.UseEditorSimulation();
        ///   #else
        ///       AssetService.UseAssetBundles();
        ///   #endif
        /// </code>
        /// </example>
        public static void UseEditorSimulation(bool verbose = false)
        {
#if UNITY_EDITOR
            SetLoader(new EditorSimulatedAssetLoader(verbose));
            Debug.Log("[AssetService] 已切换到 EditorSimulatedAssetLoader（AssetDatabase 模拟）。");
#else
            Debug.LogError("[AssetService] UseEditorSimulation() 仅限编辑器使用，" +
                           "已回退到 ResourcesAssetLoader。");
            SetLoader(new ResourcesAssetLoader());
#endif
        }

        /// <summary>
        /// 根据运行环境自动选择最合适的加载器：
        /// <list type="bullet">
        ///   <item>编辑器 → <see cref="EditorSimulatedAssetLoader"/>（AssetDatabase 模拟）</item>
        ///   <item>Player → <see cref="AssetBundleAssetLoader"/>（真实 AB 加载）</item>
        /// </list>
        /// 适合懒得写 <c>#if UNITY_EDITOR</c> 判断的快捷场景。
        /// </summary>
        /// <param name="bundleRootPath">
        ///   Player 下的 Bundle 根目录；<c>null</c> 时使用 StreamingAssets/AssetBundles。
        /// </param>
        public static void UseAutoLoader(string bundleRootPath = null)
        {
#if UNITY_EDITOR
            UseEditorSimulation();
#else
            UseAssetBundles(bundleRootPath);
#endif
        }

        /// <summary>
        /// 一键切换到 <see cref="AddressableAssetLoader"/>。
        /// 需要先安装 com.unity.addressables 包并定义 ADDRESSABLES_ENABLED。
        /// 在 GameManager.Awake() 的最顶部调用，确保在任何 Load 之前生效。
        /// </summary>
        public static void UseAddressables()
        {
            SetLoader(new AddressableAssetLoader());
            Debug.Log("[AssetService] 已切换到 AddressableAssetLoader。");
        }

        /// <summary>
        /// 一键切换到 <see cref="AssetBundleAssetLoader"/>。
        /// 在 GameManager.Awake() 的最顶部调用，确保在任何 Load 之前生效。
        /// </summary>
        /// <param name="bundleRootPath">
        ///   Bundle 文件根目录。<c>null</c> 时使用
        ///   <c>Application.streamingAssetsPath/AssetBundles</c>。
        /// </param>
        /// <param name="loadManifest">
        ///   是否自动加载 <see cref="AssetBundleManifest"/> 以启用依赖解析（推荐开启）。
        /// </param>
        /// <example>
        /// <code>
        ///   // GameManager.Awake()
        ///   AssetService.UseAssetBundles();                        // 默认路径 + 自动 Manifest
        ///   AssetService.UseAssetBundles(customPath, false);       // 自定义路径，跳过 Manifest
        /// </code>
        /// </example>
        public static void UseAssetBundles(string bundleRootPath = null, bool loadManifest = true)
        {
            AssetBundleManifest manifest = null;
            if (loadManifest)
            {
                string root = string.IsNullOrEmpty(bundleRootPath)
                    ? System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "AssetBundles")
                    : bundleRootPath;
                manifest = AssetBundleAssetLoader.LoadManifest(root);
            }

            SetLoader(new AssetBundleAssetLoader(bundleRootPath, manifest));
            Debug.Log($"[AssetService] 已切换到 AssetBundleAssetLoader。" +
                      $"Root={bundleRootPath ?? "StreamingAssets/AssetBundles"}, " +
                      $"Manifest={(manifest != null ? "已加载" : "未加载")}");
        }

        // ── 同步加载 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 同步加载资源。适合 ScriptableObject、配置数据等小型资产。
        /// Addressables 后端使用 WaitForCompletion()，会短暂阻塞主线程。
        /// </summary>
        public static T Load<T>(string path) where T : UnityEngine.Object
            => Loader.Load<T>(path);

        // ── 异步加载 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 异步加载资源。返回 <see cref="IEnumerator"/>，
        /// 由 <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> 驱动。
        /// </summary>
        /// <example>
        /// <code>
        ///   StartCoroutine(AssetService.LoadAsync&lt;Texture2D&gt;("UI/Background", tex => bg = tex));
        /// </code>
        /// </example>
        public static IEnumerator LoadAsync<T>(string path, Action<T> onLoaded)
            where T : UnityEngine.Object
            => Loader.LoadAsync(path, onLoaded);

        // ── 预加载 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 预加载资源依赖（Addressables CDN 资产下载/缓存；Resources 为空操作）。
        /// 适合在 Loading 界面提前拉取下一场景所需的远程资产。
        /// </summary>
        /// <example>
        /// <code>
        ///   // 进入 Loading 界面时预热战斗场景资产
        ///   yield return AssetService.PreloadAsync(AssetPaths.Audio.BgmCombat, onComplete: null);
        /// </code>
        /// </example>
        public static IEnumerator PreloadAsync(string path, Action onComplete = null)
            => Loader.PreloadAsync(path, onComplete);

        // ── 释放 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 释放资源句柄。Addressables 必须调用；Resources 实现为空操作。
        /// </summary>
        public static void Release(UnityEngine.Object asset)
            => Loader.Release(asset);

        /// <summary>
        /// 释放所有已追踪资源（场景切换时调用）。
        /// </summary>
        public static void ReleaseAll()
            => Loader.ReleaseAll();
    }
}
