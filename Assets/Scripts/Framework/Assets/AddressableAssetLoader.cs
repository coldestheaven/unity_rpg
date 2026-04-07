#if ADDRESSABLES_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace Framework.Assets
{
    /// <summary>
    /// 基于 Unity Addressables 的 <see cref="IAssetLoader"/> 实现。
    ///
    /// ■ 启用条件：
    ///   1. Package Manager 安装 <c>com.unity.addressables</c>（1.17+）。
    ///   2. Project Settings → Player → Scripting Define Symbols 添加 <c>ADDRESSABLES_ENABLED</c>。
    ///   3. 游戏启动时调用 <see cref="AssetService.UseAddressables"/>（GameManager.Awake 中）。
    ///
    /// ■ 与 Resources 的关键差异：
    ///   • <see cref="Load{T}"/> 使用 WaitForCompletion() 同步等待，会阻塞主线程一帧；
    ///     配置数据等小型资产可接受，大型资产请用 <see cref="LoadAsync{T}"/>。
    ///   • <see cref="Release"/> / <see cref="ReleaseAll"/> <b>必须调用</b>，否则内存泄漏。
    ///   • 地址对应 Addressables Groups 窗口中配置的 Address 字段，
    ///     可与 <see cref="AssetPaths"/> 中的常量保持一致。
    ///
    /// ■ 注意：此类仅在定义了 <c>ADDRESSABLES_ENABLED</c> 时才会编译，
    ///   未安装包时整个类体被预处理器剔除，不影响编译。
    /// </summary>
    public sealed class AddressableAssetLoader : IAssetLoader
    {
#if ADDRESSABLES_ENABLED

        // ── 句柄追踪（用于 Release）───────────────────────────────────────────
        // key = 加载到的资产引用，value = 对应的 AsyncOperationHandle
        private readonly Dictionary<UnityEngine.Object, AsyncOperationHandle> _assetHandles
            = new Dictionary<UnityEngine.Object, AsyncOperationHandle>(32);

        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _sceneHandles
            = new Dictionary<string, AsyncOperationHandle<SceneInstance>>(4);

        // ── IAssetLoader ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// 内部使用 <see cref="AsyncOperationHandle{T}.WaitForCompletion"/>（Addressables 1.17+）。
        /// 会在当前帧阻塞至加载完成，适合小型配置数据；大型资产请改用
        /// <see cref="LoadAsync{T}"/>。
        /// </remarks>
        public T Load<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetLoader] Load 地址为空。");
                return null;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            T asset = handle.WaitForCompletion();

            if (asset == null)
            {
                Debug.LogError($"[AddressableAssetLoader] 未找到资产 <{typeof(T).Name}> 地址: \"{address}\"。" +
                               "请确认已在 Addressables Groups 中配置该地址。");
                Addressables.Release(handle);
                return null;
            }

            _assetHandles[asset] = handle;
            return asset;
        }

        /// <inheritdoc/>
        public IEnumerator LoadAsync<T>(string address, Action<T> onLoaded) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetLoader] LoadAsync 地址为空。");
                onLoaded?.Invoke(null);
                yield break;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"[AddressableAssetLoader] 异步加载失败 <{typeof(T).Name}> 地址: \"{address}\"。" +
                               $"错误: {handle.OperationException?.Message}");
                Addressables.Release(handle);
                onLoaded?.Invoke(null);
                yield break;
            }

            _assetHandles[handle.Result] = handle;
            onLoaded?.Invoke(handle.Result);
        }

        /// <inheritdoc/>
        /// <remarks>必须为每个通过本加载器获得的资产调用此方法，否则引起内存泄漏。</remarks>
        public void Release(UnityEngine.Object asset)
        {
            if (asset == null) return;

            if (_assetHandles.TryGetValue(asset, out var handle))
            {
                Addressables.Release(handle);
                _assetHandles.Remove(asset);
            }
            else
            {
                Debug.LogWarning($"[AddressableAssetLoader] Release: 未找到资产 {asset.name} 的句柄，" +
                                 "可能已被释放或不由本加载器管理。");
            }
        }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            foreach (var handle in _assetHandles.Values)
                Addressables.Release(handle);
            _assetHandles.Clear();

            foreach (var handle in _sceneHandles.Values)
                Addressables.UnloadSceneAsync(handle);
            _sceneHandles.Clear();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 触发 <see cref="Addressables.DownloadDependenciesAsync"/> 预热远程 CDN 资产。
        /// 本地包（Packed/Local）无网络 IO，但依然能触发 AssetBundle 解包缓存。
        /// </remarks>
        public IEnumerator PreloadAsync(string address, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                onComplete?.Invoke();
                yield break;
            }

            var handle = Addressables.DownloadDependenciesAsync(address);
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                Debug.LogWarning($"[AddressableAssetLoader] 预加载部分失败: \"{address}\"。" +
                                 $"错误: {handle.OperationException?.Message}");

            Addressables.Release(handle);
            onComplete?.Invoke();
        }

        // ── 场景加载（Addressables 专属）────────────────────────────────────

        /// <summary>
        /// 异步加载 Addressables 场景。
        /// 场景 Address 需在 Addressables Groups 中配置（通常与 SceneManager 的场景名相同）。
        /// </summary>
        /// <param name="address">Addressables 场景地址。</param>
        /// <param name="mode">加载模式，默认替换当前场景。</param>
        /// <param name="onComplete">加载完成回调，参数为是否成功。</param>
        public IEnumerator LoadSceneAsync(string address,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<bool> onComplete = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableAssetLoader] LoadSceneAsync 地址为空。");
                onComplete?.Invoke(false);
                yield break;
            }

            var handle = Addressables.LoadSceneAsync(address, mode);
            yield return handle;

            bool success = handle.Status == AsyncOperationStatus.Succeeded;
            if (success)
                _sceneHandles[address] = handle;
            else
                Debug.LogError($"[AddressableAssetLoader] 场景加载失败: \"{address}\"。" +
                               $"错误: {handle.OperationException?.Message}");

            onComplete?.Invoke(success);
        }

        /// <summary>
        /// 卸载由 <see cref="LoadSceneAsync"/> 加载的 Addressables 场景。
        /// </summary>
        public IEnumerator UnloadSceneAsync(string address, Action<bool> onComplete = null)
        {
            if (!_sceneHandles.TryGetValue(address, out var handle))
            {
                Debug.LogWarning($"[AddressableAssetLoader] UnloadScene: 未找到场景句柄 \"{address}\"。");
                onComplete?.Invoke(false);
                yield break;
            }

            var unloadHandle = Addressables.UnloadSceneAsync(handle);
            yield return unloadHandle;

            bool success = unloadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success)
                _sceneHandles.Remove(address);
            else
                Debug.LogError($"[AddressableAssetLoader] 场景卸载失败: \"{address}\"。");

            Addressables.Release(unloadHandle);
            onComplete?.Invoke(success);
        }

#else
        // ── 未安装包时的安全占位符 ──────────────────────────────────────────────

        private const string PackageWarning =
            "[AddressableAssetLoader] Addressables 包未安装或 ADDRESSABLES_ENABLED 未定义。\n" +
            "请执行：\n" +
            "  1. Package Manager → 搜索 'Addressables' → Install\n" +
            "  2. Project Settings → Player → Scripting Define Symbols → 添加 ADDRESSABLES_ENABLED\n" +
            "  3. 游戏初始化时调用 AssetService.UseAddressables()";

        public T Load<T>(string address) where T : UnityEngine.Object
        {
            Debug.LogError(PackageWarning);
            return null;
        }

        public System.Collections.IEnumerator LoadAsync<T>(string address, Action<T> onLoaded)
            where T : UnityEngine.Object
        {
            Debug.LogError(PackageWarning);
            onLoaded?.Invoke(null);
            yield break;
        }

        public void Release(UnityEngine.Object asset) => Debug.LogError(PackageWarning);

        public void ReleaseAll() => Debug.LogError(PackageWarning);

        public System.Collections.IEnumerator PreloadAsync(string address, Action onComplete = null)
        {
            Debug.LogError(PackageWarning);
            onComplete?.Invoke();
            yield break;
        }
#endif
    }
}
