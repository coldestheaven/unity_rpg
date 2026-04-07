using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework.Assets
{
    /// <summary>
    /// 基于 Unity <see cref="AssetBundle"/> 的 <see cref="IAssetLoader"/> 实现。
    ///
    /// ■ 路径约定（<c>path</c> 参数格式）：
    ///   <code>
    ///     "bundleName/assetName"
    ///     // 例：AssetService.Load&lt;AudioClip&gt;("audio_bgm/bgm_main")
    ///     //     → 从 audio_bgm.bundle 中加载名为 bgm_main 的资产
    ///   </code>
    ///   对应 <see cref="AssetPaths"/> 常量：
    ///   <code>
    ///     public const string BgmMain = "audio_bgm/bgm_main";
    ///   </code>
    ///
    /// ■ Bundle 根目录默认为 <c>Application.streamingAssetsPath/AssetBundles/</c>。
    ///   通过构造函数 <paramref name="bundleRootPath"/> 自定义（热更新场景下可指向
    ///   <c>Application.persistentDataPath</c>）。
    ///
    /// ■ 依赖解析：
    ///   传入通过 <see cref="LoadManifest"/> 获取的 <see cref="AssetBundleManifest"/>，
    ///   加载任一 bundle 时会自动递归加载其所有依赖。
    ///
    /// ■ 生命周期：
    ///   • <see cref="Release"/> — 移除资产的 bundle 追踪（不卸载 bundle，便于复用）。
    ///   • <see cref="UnloadBundle"/> — 精确卸载单个 bundle。
    ///   • <see cref="ReleaseAll"/> — 卸载所有已缓存 bundle（场景切换时调用）。
    /// </summary>
    public sealed class AssetBundleAssetLoader : IAssetLoader
    {
        // ── 字段 ─────────────────────────────────────────────────────────────

        private readonly string              _rootPath;
        private readonly AssetBundleManifest _manifest;

        // bundleName → 已加载的 AssetBundle（缓存，避免重复磁盘 IO）
        private readonly Dictionary<string, AssetBundle> _bundleCache
            = new Dictionary<string, AssetBundle>(16, StringComparer.OrdinalIgnoreCase);

        // 已加载资产 → 所属 bundleName（用于 Release 精确定位）
        private readonly Dictionary<UnityEngine.Object, string> _assetBundleMap
            = new Dictionary<UnityEngine.Object, string>(64);

        // ── 构造 ─────────────────────────────────────────────────────────────

        /// <param name="bundleRootPath">
        ///   Bundle 文件所在根目录。
        ///   <c>null</c> 时使用 <c>Application.streamingAssetsPath/AssetBundles</c>。
        /// </param>
        /// <param name="manifest">
        ///   通过 <see cref="LoadManifest"/> 预加载的 Manifest，用于自动解析依赖。
        ///   不需要依赖解析时传 <c>null</c>。
        /// </param>
        public AssetBundleAssetLoader(string bundleRootPath = null, AssetBundleManifest manifest = null)
        {
            _rootPath = string.IsNullOrEmpty(bundleRootPath)
                ? Path.Combine(Application.streamingAssetsPath, "AssetBundles")
                : bundleRootPath;
            _manifest = manifest;
        }

        // ── 静态工具 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 从 Bundle 根目录加载 <see cref="AssetBundleManifest"/>。
        /// Manifest bundle 的文件名与其所在目录同名（Unity Build Pipeline 约定）。
        /// </summary>
        /// <param name="bundleRootPath">Bundle 根目录（含 manifest bundle 文件）。</param>
        /// <returns>
        ///   <see cref="AssetBundleManifest"/> 实例；失败时返回 <c>null</c>。
        /// </returns>
        /// <example>
        /// <code>
        ///   var manifest = AssetBundleAssetLoader.LoadManifest(
        ///       Application.streamingAssetsPath + "/AssetBundles");
        ///   AssetService.UseAssetBundles(bundleRootPath, manifest);
        /// </code>
        /// </example>
        public static AssetBundleManifest LoadManifest(string bundleRootPath)
        {
            // Manifest bundle 名 = 目录名（Build Pipeline 的约定）
            string dirName     = Path.GetFileName(bundleRootPath.TrimEnd('/', '\\'));
            string manifestPath = Path.Combine(bundleRootPath, dirName);

            var bundle = AssetBundle.LoadFromFile(manifestPath);
            if (bundle == null)
            {
                Debug.LogError($"[AssetBundleAssetLoader] 无法加载 Manifest Bundle: {manifestPath}");
                return null;
            }

            var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            bundle.Unload(false);   // 卸载 bundle 容器，保留 manifest 资产
            return manifest;
        }

        // ── IAssetLoader ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>path 格式：<c>"bundleName/assetName"</c></remarks>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (!ParsePath(path, out var bundleName, out var assetName)) return null;

            var bundle = GetOrLoadBundle(bundleName);
            if (bundle == null) return null;

            var asset = bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogError($"[AssetBundleAssetLoader] 未找到资产 <{typeof(T).Name}> " +
                               $"'{assetName}' in bundle '{bundleName}'。");
                return null;
            }

            _assetBundleMap[asset] = bundleName;
            return asset;
        }

        /// <inheritdoc/>
        /// <remarks>path 格式：<c>"bundleName/assetName"</c></remarks>
        public IEnumerator LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
        {
            if (!ParsePath(path, out var bundleName, out var assetName))
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            AssetBundle bundle = null;
            yield return LoadBundleAsync(bundleName, b => bundle = b);

            if (bundle == null)
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            var req = bundle.LoadAssetAsync<T>(assetName);
            yield return req;

            var asset = req.asset as T;
            if (asset == null)
            {
                Debug.LogError($"[AssetBundleAssetLoader] 未找到资产 <{typeof(T).Name}> " +
                               $"'{assetName}' in bundle '{bundleName}'。");
                onLoaded?.Invoke(null);
                yield break;
            }

            _assetBundleMap[asset] = bundleName;
            onLoaded?.Invoke(asset);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 移除资产的 bundle 关联追踪。bundle 本身不卸载（可继续为其他资产服务）。
        /// 需要卸载 bundle 时请用 <see cref="UnloadBundle"/>。
        /// </remarks>
        public void Release(UnityEngine.Object asset)
        {
            if (asset != null)
                _assetBundleMap.Remove(asset);
        }

        /// <inheritdoc/>
        /// <remarks>调用 <c>Unload(true)</c>，同时卸载 bundle 中已加载的实例。</remarks>
        public void ReleaseAll()
        {
            _assetBundleMap.Clear();
            foreach (var bundle in _bundleCache.Values)
                bundle.Unload(true);
            _bundleCache.Clear();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// AssetBundle 实现：将目标 bundle（及其依赖）预先加载进缓存，
        /// 后续 <see cref="Load{T}"/> 无需重复磁盘 IO。
        /// <para>path 可以只含 bundleName（省略 /assetName）。</para>
        /// </remarks>
        public IEnumerator PreloadAsync(string address, Action onComplete = null)
        {
            // 允许只传 bundleName（无斜线）
            ParsePath(address, out var bundleName, out _);
            yield return LoadBundleAsync(bundleName, _ => { });
            onComplete?.Invoke();
        }

        // ── AB 专属 API ───────────────────────────────────────────────────────

        /// <summary>已缓存（已加载）的 bundle 名称列表。</summary>
        public IReadOnlyCollection<string> LoadedBundles => _bundleCache.Keys;

        /// <summary>
        /// 精确卸载单个 bundle。
        /// </summary>
        /// <param name="bundleName">要卸载的 bundle 名。</param>
        /// <param name="unloadLoadedAssets">
        ///   <c>true</c>：同时卸载从此 bundle 加载的所有资产实例（慎用）。
        ///   <c>false</c>：仅卸载 bundle 容器，已加载资产继续有效。
        /// </param>
        public void UnloadBundle(string bundleName, bool unloadLoadedAssets = false)
        {
            if (_bundleCache.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadLoadedAssets);
                _bundleCache.Remove(bundleName);
            }
        }

        /// <summary>
        /// 异步加载单个 bundle（不加载资产）。等价于 <see cref="PreloadAsync"/> 的单 bundle 版本。
        /// </summary>
        public IEnumerator PreloadBundleAsync(string bundleName, Action<bool> onComplete = null)
        {
            AssetBundle bundle = null;
            yield return LoadBundleAsync(bundleName, b => bundle = b);
            onComplete?.Invoke(bundle != null);
        }

        // ── 内部实现 ──────────────────────────────────────────────────────────

        /// <summary>解析 "bundleName/assetName" 格式，允许省略 assetName。</summary>
        private static bool ParsePath(string path, out string bundleName, out string assetName)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AssetBundleAssetLoader] path 为空。");
                bundleName = assetName = string.Empty;
                return false;
            }

            int sep = path.IndexOf('/');
            if (sep <= 0)
            {
                // 只有 bundleName，assetName 与 bundleName 相同（允许省略）
                bundleName = path;
                assetName  = path;
            }
            else
            {
                bundleName = path.Substring(0, sep);
                assetName  = sep < path.Length - 1 ? path.Substring(sep + 1) : path;
            }
            return true;
        }

        private string BundleFilePath(string bundleName)
            => Path.Combine(_rootPath, bundleName);

        /// <summary>同步获取或加载 bundle（含依赖）。</summary>
        private AssetBundle GetOrLoadBundle(string bundleName)
        {
            if (_bundleCache.TryGetValue(bundleName, out var cached))
                return cached;

            LoadDependenciesSync(bundleName);

            var bundle = AssetBundle.LoadFromFile(BundleFilePath(bundleName));
            if (bundle == null)
            {
                Debug.LogError($"[AssetBundleAssetLoader] 同步加载 bundle 失败: " +
                               $"{BundleFilePath(bundleName)}");
                return null;
            }

            _bundleCache[bundleName] = bundle;
            return bundle;
        }

        private void LoadDependenciesSync(string bundleName)
        {
            if (_manifest == null) return;
            foreach (var dep in _manifest.GetAllDependencies(bundleName))
                GetOrLoadBundle(dep);
        }

        /// <summary>异步获取或加载 bundle（含依赖）。</summary>
        private IEnumerator LoadBundleAsync(string bundleName, Action<AssetBundle> onLoaded)
        {
            if (_bundleCache.TryGetValue(bundleName, out var cached))
            {
                onLoaded?.Invoke(cached);
                yield break;
            }

            // 先异步加载所有依赖
            if (_manifest != null)
            {
                foreach (var dep in _manifest.GetAllDependencies(bundleName))
                    yield return LoadBundleAsync(dep, _ => { });
            }

            var req = AssetBundle.LoadFromFileAsync(BundleFilePath(bundleName));
            yield return req;

            if (req.assetBundle == null)
            {
                Debug.LogError($"[AssetBundleAssetLoader] 异步加载 bundle 失败: " +
                               $"{BundleFilePath(bundleName)}");
                onLoaded?.Invoke(null);
                yield break;
            }

            _bundleCache[bundleName] = req.assetBundle;
            onLoaded?.Invoke(req.assetBundle);
        }
    }
}
