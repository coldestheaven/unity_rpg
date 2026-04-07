using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Assets;
using Framework.Core.Patterns;
using UnityEngine;

namespace Framework.Graphics
{
    /// <summary>
    /// 集中式 Shader 管理器（单例 MonoBehaviour）。
    ///
    /// ■ 核心职责：
    ///   1. 管理 <see cref="ShaderVariantCollection"/> 的注册与预热，
    ///      消除首次渲染时的 GPU 着色器编译卡帧（Shader Compilation Stall）。
    ///   2. 缓存 <see cref="Shader"/> 查找结果（避免重复 <c>Shader.Find</c> 字符串开销）。
    ///   3. 缓存共享 <see cref="Material"/>（减少逐帧 <c>new Material()</c> 分配）。
    ///
    /// ■ 使用方式（在 Loading 场景）：
    /// <code>
    ///   // 方式 A：协程进度反馈（推荐）
    ///   yield return ShaderManager.Instance.WarmupAsync(
    ///       progress => loadingBar.value = progress);
    ///
    ///   // 方式 B：立即同步（单帧内完成，适合集合较少时）
    ///   ShaderManager.Instance.WarmupImmediate();
    /// </code>
    ///
    /// ■ 注意：
    ///   • <see cref="ShaderVariantCollection.WarmUp"/> 会触发 GPU 着色器编译，
    ///     须在主线程执行，不得移入后台线程。
    ///   • 已预热的 Collection 再次调用 WarmUp 是幂等操作，无额外开销。
    /// </summary>
    public sealed class ShaderManager : Singleton<ShaderManager>
    {
        // ── Inspector 字段 ────────────────────────────────────────────────────

        [Header("配置")]
        [Tooltip("Shader Warmup Config 资产（留空则自动从 Resources 加载）。")]
        [SerializeField] private ShaderWarmupConfig _config;

        // ── 运行时状态 ────────────────────────────────────────────────────────

        private readonly List<ShaderVariantCollection> _collections
            = new List<ShaderVariantCollection>(8);

        private readonly Dictionary<string, Shader>   _shaderCache
            = new Dictionary<string, Shader>(32);

        private readonly Dictionary<string, Material> _materialCache
            = new Dictionary<string, Material>(16);

        private int  _warmedUpCount;
        private bool _warmupStarted;
        private bool _warmupComplete;

        /// <summary>已预热完成的 Collection 数 / 已注册总数。</summary>
        public float Progress => _collections.Count == 0
            ? 1f
            : (float)_warmedUpCount / _collections.Count;

        /// <summary>所有 Collection 均已完成预热时为 true。</summary>
        public bool IsWarmedUp => _warmupComplete;

        // ── Unity 生命周期 ────────────────────────────────────────────────────

        private void Start()
        {
            EnsureConfig();

            if (_config == null) return;

            // 注册 Config 中的 Collections
            foreach (var col in _config.collections)
                Register(col);

            if (_config.warmupOnStart)
            {
                if (_config.mode == ShaderWarmupConfig.WarmupMode.Immediate)
                    WarmupImmediate();
                else
                    StartCoroutine(WarmupAsync(null, _config.collectionsPerFrame));
            }
        }

        // ── 注册 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 运行时追加注册一个 <see cref="ShaderVariantCollection"/>。
        /// 可在 <see cref="Start"/> 之后、<see cref="WarmupAsync"/> 之前调用。
        /// </summary>
        public void Register(ShaderVariantCollection collection)
        {
            if (collection == null || _collections.Contains(collection)) return;
            _collections.Add(collection);
        }

        // ── 立即预热 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 当帧内同步预热所有已注册的 <see cref="ShaderVariantCollection"/>。
        /// Collection 较多时可能造成短暂卡顿；适合集合简单或无 Loading 界面的场景。
        /// </summary>
        public void WarmupImmediate()
        {
            if (_warmupComplete) return;

            _warmupStarted = true;
            int total = _collections.Count;

            for (int i = 0; i < total; i++)
            {
                WarmupOne(_collections[i], i, total);
                _warmedUpCount++;
            }

            _warmupComplete = true;
            Debug.Log($"[ShaderManager] 立即预热完成，共 {total} 个 Collection。");
        }

        // ── 渐进预热 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 渐进式（逐帧）预热所有已注册的 Collection，适合在 Loading 界面调用。
        /// </summary>
        /// <param name="onProgress">
        ///   每帧回调，参数为 [0, 1] 进度值，用于驱动进度条 UI。
        ///   传 <c>null</c> 时不回调。
        /// </param>
        /// <param name="collectionsPerFrame">
        ///   每帧处理的 Collection 数量（默认 1，最平滑）。
        ///   增大此值可加快预热，但单帧 GPU 负担也随之增加。
        /// </param>
        public IEnumerator WarmupAsync(Action<float> onProgress = null,
                                       int collectionsPerFrame  = 1)
        {
            if (_warmupComplete) { onProgress?.Invoke(1f); yield break; }

            _warmupStarted = true;
            int total      = _collections.Count;
            collectionsPerFrame = Mathf.Max(1, collectionsPerFrame);

            if (total == 0)
            {
                Debug.LogWarning("[ShaderManager] 没有已注册的 ShaderVariantCollection，跳过预热。");
                _warmupComplete = true;
                onProgress?.Invoke(1f);
                yield break;
            }

            Debug.Log($"[ShaderManager] 开始渐进预热，共 {total} 个 Collection" +
                      $"（每帧 {collectionsPerFrame} 个）...");

            int idx = 0;
            while (idx < total)
            {
                // 每帧处理 collectionsPerFrame 个
                int end = Mathf.Min(idx + collectionsPerFrame, total);
                for (; idx < end; idx++)
                {
                    WarmupOne(_collections[idx], idx, total);
                    _warmedUpCount++;
                }

                onProgress?.Invoke(Progress);
                yield return null;  // 等待下一帧，给 Loading UI 更新的机会
            }

            _warmupComplete = true;
            onProgress?.Invoke(1f);
            Debug.Log($"[ShaderManager] 渐进预热完成，共 {total} 个 Collection。");
        }

        // ── Shader / Material 缓存 ────────────────────────────────────────────

        /// <summary>
        /// 按名称查找 Shader，结果缓存于内存。
        /// 避免高频调用 <c>Shader.Find</c>（每次涉及字符串哈希查找）。
        /// </summary>
        public Shader GetShader(string shaderName)
        {
            if (_shaderCache.TryGetValue(shaderName, out var cached)) return cached;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                Debug.LogError($"[ShaderManager] Shader 未找到: \"{shaderName}\"。" +
                               "请确认 Always Included Shaders 或 ShaderVariantCollection 已包含该 Shader。");
            else
                _shaderCache[shaderName] = shader;

            return shader;
        }

        /// <summary>
        /// 获取以指定 Shader 构建的共享 <see cref="Material"/>（缓存单实例）。
        /// 适合不需要逐对象独立材质球的场景（如 UI、粒子特效公共材质）。
        /// </summary>
        /// <param name="shaderName">Shader 名称（如 <c>"Sprites/Default"</c>）。</param>
        public Material GetSharedMaterial(string shaderName)
        {
            if (_materialCache.TryGetValue(shaderName, out var cached)) return cached;

            var shader = GetShader(shaderName);
            if (shader == null) return null;

            var mat = new Material(shader) { name = $"[Shared] {shaderName}" };
            _materialCache[shaderName] = mat;
            return mat;
        }

        // ── 调试 ─────────────────────────────────────────────────────────────

        /// <summary>打印所有已注册 Collection 的预热状态。</summary>
        public string GetStats()
        {
            using (Framework.Core.Pools.StringBuilderPool.Rent(out var sb))
            {
                sb.AppendLine("[ShaderManager] Warmup 状态：");
                sb.Append("  总计: ").Append(_collections.Count)
                  .Append("  已预热: ").Append(_warmedUpCount)
                  .Append("  完成: ").AppendLine(_warmupComplete.ToString());

                for (int i = 0; i < _collections.Count; i++)
                {
                    var col = _collections[i];
                    if (col == null) continue;
                    sb.Append("  [").Append(i).Append("] ").Append(col.name)
                      .Append(" — shaderCount=").Append(col.shaderCount)
                      .Append(" warmedUp=").AppendLine(col.isWarmedUp.ToString());
                }
                return sb.ToString();
            }
        }

        // ── 生命周期清理 ──────────────────────────────────────────────────────

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 清理动态创建的共享材质球，防止资产泄漏
            foreach (var mat in _materialCache.Values)
                if (mat != null) Destroy(mat);
            _materialCache.Clear();
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────

        private static void WarmupOne(ShaderVariantCollection collection, int idx, int total)
        {
            if (collection == null) return;
            if (collection.isWarmedUp)
            {
                Debug.Log($"[ShaderManager] [{idx + 1}/{total}] '{collection.name}' 已预热，跳过。");
                return;
            }

            float t0 = Time.realtimeSinceStartup;
            collection.WarmUp();
            float ms = (Time.realtimeSinceStartup - t0) * 1000f;
            Debug.Log($"[ShaderManager] [{idx + 1}/{total}] '{collection.name}' 预热完成" +
                      $"（{collection.shaderCount} shaders，耗时 {ms:F1} ms）。");
        }

        private void EnsureConfig()
        {
            if (_config != null) return;
            _config = AssetService.Load<ShaderWarmupConfig>(AssetPaths.Graphics.ShaderWarmupConfig);
            if (_config == null)
                Debug.LogWarning("[ShaderManager] 未找到 ShaderWarmupConfig 资产，" +
                                 "请在 Inspector 中手动赋值或放至 Resources/Graphics/ShaderWarmupConfig.asset。");
        }
    }
}
