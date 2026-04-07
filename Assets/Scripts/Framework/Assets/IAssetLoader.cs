using System;
using System.Collections;
using UnityEngine;

namespace Framework.Assets
{
    /// <summary>
    /// 资源加载抽象接口。
    ///
    /// 设计目标：
    ///   • 屏蔽底层加载机制（Resources / Addressables / AssetBundle），
    ///     调用方无需关心资源来自哪里。
    ///   • 支持同步加载（快速，适合 ScriptableObject 数据）和
    ///     异步加载（适合大型贴图、音频、场景）。
    ///   • Release 语义兼容 Addressables：Resources 实现为空操作，
    ///     切换到 Addressables 时只需换实现，调用方代码不变。
    ///
    /// 用法：
    /// <code>
    ///   // 同步
    ///   var data = AssetService.Load&lt;SkillData&gt;(AssetPaths.Data.SkillDatabase);
    ///
    ///   // 异步（协程）
    ///   yield return AssetService.LoadAsync&lt;AudioClip&gt;(AssetPaths.Audio.BgmMain, clip => bgm = clip);
    ///
    ///   // 释放（Addressables 必须调用；Resources 下为空操作）
    ///   AssetService.Release(clip);
    /// </code>
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 同步加载资源。
        /// Resources 实现：立即返回；Addressables 实现：仅从缓存读取。
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object 子类型。</typeparam>
        /// <param name="path">资源路径（相对于 Resources 根目录，或 Addressables 地址）。</param>
        /// <returns>加载到的资源，失败时返回 null 并记录错误日志。</returns>
        T Load<T>(string path) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源，通过回调返回结果。
        /// 返回值为协程 <see cref="IEnumerator"/>，由调用方用 StartCoroutine 驱动。
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object 子类型。</typeparam>
        /// <param name="path">资源路径。</param>
        /// <param name="onLoaded">加载完成后的回调，参数为加载到的资源（失败时为 null）。</param>
        IEnumerator LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object;

        /// <summary>
        /// 释放资源句柄。
        /// Addressables 必须调用以避免内存泄漏；Resources 实现为空操作。
        /// </summary>
        void Release(UnityEngine.Object asset);

        /// <summary>
        /// 释放所有已追踪的资源句柄（场景切换时调用）。
        /// Resources 实现触发 <see cref="Resources.UnloadUnusedAssets"/>。
        /// </summary>
        void ReleaseAll();
    }
}
