using System;
using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework.Assets
{
    /// <summary>
    /// 编辑器下 AssetBundle 的模拟加载器。
    ///
    /// ■ 工作原理：
    ///   利用 <see cref="AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName"/> 查询
    ///   已在 Inspector 中设置了 AssetBundle 标签的资产，再通过
    ///   <see cref="AssetDatabase.LoadAssetAtPath{T}"/> 直接加载，
    ///   <b>无需实际构建 AssetBundle</b>，大幅缩短迭代周期。
    ///
    /// ■ 前提条件：
    ///   资产的 AssetBundle 标签（Inspector → Asset Labels）必须与
    ///   运行时 bundle 名一致，否则查询结果为空。
    ///
    /// ■ path 格式：同 <see cref="AssetBundleAssetLoader"/>，即 <c>"bundleName/assetName"</c>。
    ///
    /// ■ 推荐启用方式（GameManager.Awake）：
    /// <code>
    ///   #if UNITY_EDITOR
    ///       AssetService.UseEditorSimulation();   // 编辑器：模拟加载
    ///   #else
    ///       AssetService.UseAssetBundles();        // 真机：真实 AB 加载
    ///   #endif
    /// </code>
    ///
    /// ■ 此类在非编辑器环境（Player 构建）下仅保留桩实现，不会引用 UnityEditor 命名空间。
    /// </summary>
    public sealed class EditorSimulatedAssetLoader : IAssetLoader
    {
#if UNITY_EDITOR

        private readonly bool _verbose;

        /// <param name="verbose">
        ///   <c>true</c> 时每次加载都打印详细日志，便于核对路径映射是否正确。
        /// </param>
        public EditorSimulatedAssetLoader(bool verbose = false)
        {
            _verbose = verbose;
        }

        // ── IAssetLoader ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// 通过 <see cref="AssetDatabase"/> 同步加载，编辑器内无帧开销。
        /// </remarks>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (!ParsePath(path, out var bundleName, out var assetName))
                return null;

            var asset = FindAsset<T>(bundleName, assetName);

            if (_verbose)
            {
                if (asset != null)
                    Debug.Log($"[EditorSim] Load<{typeof(T).Name}> '{path}' → {AssetDatabase.GetAssetPath(asset)}");
                else
                    Debug.LogWarning($"[EditorSim] Load<{typeof(T).Name}> '{path}' → 未找到");
            }

            return asset;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 编辑器模拟下等价于同步加载（立即回调）。
        /// 设置了 <paramref name="simulatedDelay"/> 时会等待指定秒数再回调，
        /// 用于测试异步加载的 UI 表现。
        /// </remarks>
        public IEnumerator LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
        {
            // 模拟一帧延迟，让调用方的协程状态机至少推进一次
            yield return null;
            onLoaded?.Invoke(Load<T>(path));
        }

        /// <inheritdoc/>
        /// <remarks>编辑器模拟下无需释放，为空操作。</remarks>
        public void Release(UnityEngine.Object asset) { }

        /// <inheritdoc/>
        /// <remarks>编辑器模拟下无需释放，为空操作。</remarks>
        public void ReleaseAll() { }

        /// <inheritdoc/>
        /// <remarks>编辑器模拟下无需预加载，立即触发回调。</remarks>
        public IEnumerator PreloadAsync(string address, Action onComplete = null)
        {
            yield return null;
            onComplete?.Invoke();
        }

        // ── 查询工具 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 列出指定 bundle 中所有已标记资产的路径（调试用）。
        /// </summary>
        public static string[] GetBundleAssetPaths(string bundleName)
            => AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

        // ── 内部实现 ──────────────────────────────────────────────────────────

        private static T FindAsset<T>(string bundleName, string assetName)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("[EditorSim] bundleName 为空。");
                return null;
            }

            // 优先用 bundleName + assetName 联合查询（精确）
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(
                bundleName, assetName);

            foreach (var p in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(p);
                if (asset != null) return asset;
            }

            // 找不到时尝试只用 assetName 在整个 bundle 中搜索（文件名不带扩展名模糊匹配）
            if (paths.Length == 0)
            {
                string[] allInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                string lowerAsset = assetName.ToLowerInvariant();

                foreach (var p in allInBundle)
                {
                    string fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                    if (fileNameNoExt == lowerAsset)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<T>(p);
                        if (asset != null) return asset;
                    }
                }

                Debug.LogError($"[EditorSim] 未找到资产 <{typeof(T).Name}> " +
                               $"bundle='{bundleName}', asset='{assetName}'。\n" +
                               $"请确认资产已在 Inspector 中设置 AssetBundle 标签为 '{bundleName}'。");
            }

            return null;
        }

        private static bool ParsePath(string path, out string bundleName, out string assetName)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[EditorSim] path 为空。");
                bundleName = assetName = string.Empty;
                return false;
            }

            int sep = path.IndexOf('/');
            if (sep <= 0)
            {
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

#else
        // ── 非编辑器桩实现（Player 构建不引用 UnityEditor）──────────────────

        public EditorSimulatedAssetLoader(bool verbose = false)
        {
            Debug.LogError("[EditorSimulatedAssetLoader] 此类仅用于编辑器，" +
                           "请勿在 Player 构建中使用。");
        }

        public T Load<T>(string path) where T : UnityEngine.Object => null;

        public IEnumerator LoadAsync<T>(string path, Action<T> onLoaded)
            where T : UnityEngine.Object
        { onLoaded?.Invoke(null); yield break; }

        public void Release(UnityEngine.Object asset) { }
        public void ReleaseAll() { }

        public IEnumerator PreloadAsync(string address, Action onComplete = null)
        { onComplete?.Invoke(); yield break; }
#endif
    }
}
