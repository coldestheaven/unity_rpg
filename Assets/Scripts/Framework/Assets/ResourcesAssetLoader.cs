using System;
using System.Collections;
using UnityEngine;

namespace Framework.Assets
{
    /// <summary>
    /// 基于 Unity <see cref="Resources"/> API 的 <see cref="IAssetLoader"/> 实现。
    ///
    /// • 同步加载使用 <see cref="Resources.Load{T}"/>。
    /// • 异步加载使用 <see cref="Resources.LoadAsync{T}"/>（返回 IEnumerator，
    ///   由调用方通过 StartCoroutine 驱动）。
    /// • Release 为空操作（Resources 无需显式释放单个资源）。
    /// • ReleaseAll 触发 <see cref="Resources.UnloadUnusedAssets"/> 并异步等待完成。
    ///
    /// 注意：此实现是单线程、主线程的；请勿在逻辑线程中调用。
    /// </summary>
    public sealed class ResourcesAssetLoader : IAssetLoader
    {
        // ── IAssetLoader ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ResourcesAssetLoader] Load 路径为空。");
                return null;
            }

            var asset = Resources.Load<T>(path);
            if (asset == null)
                Debug.LogError($"[ResourcesAssetLoader] 未找到资源 <{typeof(T).Name}> 路径: \"{path}\"。" +
                               "请确认文件位于 Assets/Resources/ 下且路径拼写正确。");
            return asset;
        }

        /// <inheritdoc/>
        public IEnumerator LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ResourcesAssetLoader] LoadAsync 路径为空。");
                onLoaded?.Invoke(null);
                yield break;
            }

            ResourceRequest request = Resources.LoadAsync<T>(path);
            yield return request;

            if (request.asset == null)
            {
                Debug.LogError($"[ResourcesAssetLoader] 异步加载失败 <{typeof(T).Name}> 路径: \"{path}\"。");
                onLoaded?.Invoke(null);
                yield break;
            }

            onLoaded?.Invoke(request.asset as T);
        }

        /// <inheritdoc/>
        /// <remarks>Resources 系统无需显式释放，此处为空操作以保持接口一致性。</remarks>
        public void Release(UnityEngine.Object asset) { }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
