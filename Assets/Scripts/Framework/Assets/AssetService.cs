using System;
using System.Collections;
using UnityEngine;

namespace Framework.Assets
{
    /// <summary>
    /// 全局资源加载服务入口（服务定位器模式）。
    ///
    /// ■ 默认使用 <see cref="ResourcesAssetLoader"/>，无需任何配置即可工作。
    /// ■ 要切换到 Addressables 或 AssetBundle，在游戏启动时调用
    ///   <see cref="SetLoader"/> 注入自定义实现，其他代码无需修改。
    ///
    /// 用法：
    /// <code>
    ///   // 同步（ScriptableObject 数据、小型资产）
    ///   var db = AssetService.Load&lt;ItemDatabase&gt;(AssetPaths.Data.ItemDatabase);
    ///
    ///   // 异步（音频、大型贴图）
    ///   yield return AssetService.LoadAsync&lt;AudioClip&gt;(AssetPaths.Audio.BgmMain, c => _bgm = c);
    ///
    ///   // 切换实现（游戏初始化时一次性调用）
    ///   AssetService.SetLoader(new AddressableAssetLoader());
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

        // ── 同步加载 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 同步加载资源。适合 ScriptableObject、配置数据等小型资产。
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
