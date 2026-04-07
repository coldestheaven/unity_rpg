using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Scripting
{
    /// <summary>
    /// 在主线程上执行由 <see cref="RoslynCompiler"/> 编译的 <see cref="IGameScript"/> 实现。
    ///
    /// <para>
    /// <see cref="Shared"/> 是全局共享实例（内置 <see cref="ScriptCache"/> 与 <see cref="RoslynCompiler"/>）。
    /// 若需自定义缓存策略或额外程序集引用，可直接 <c>new ScriptRunner()</c>。
    /// </para>
    ///
    /// 典型用法（Editor / 调试场景）：
    /// <code>
    /// string src = File.ReadAllText(scriptPath);
    /// await ScriptRunner.Shared.CompileAndRunAsync(src, new GameScriptContext());
    /// </code>
    ///
    /// 典型用法（运行时热补丁）：
    /// <code>
    /// var result = await ScriptRunner.Shared.CompileAsync(src);
    /// if (result.IsSuccess)
    ///     ScriptRunner.Shared.Run(result, context);
    /// </code>
    /// </summary>
    public sealed class ScriptRunner
    {
        // ── 单例 ──────────────────────────────────────────────────────────────

        private static ScriptRunner _shared;
        /// <summary>全局共享实例（首次访问时创建）。</summary>
        public static ScriptRunner Shared => _shared ??= new ScriptRunner();

        // ── 状态 ──────────────────────────────────────────────────────────────

        /// <summary>编译器实例。</summary>
        public RoslynCompiler Compiler { get; }

        /// <summary>编译结果缓存。</summary>
        public ScriptCache Cache { get; }

        /// <summary>记录每次执行的时间（毫秒）。仅保留最近 64 次。</summary>
        public IReadOnlyList<double> ExecutionTimesMs => _execTimes;

        private readonly List<double> _execTimes = new List<double>(64);
        private int _runCount;
        private int _errorCount;

        // ── 构造 ──────────────────────────────────────────────────────────────

        public ScriptRunner(RoslynCompiler compiler = null, ScriptCache cache = null)
        {
            Compiler = compiler ?? new RoslynCompiler();
            Cache    = cache    ?? new ScriptCache(32);
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// 同步编译源码（含缓存查找）。
        /// </summary>
        public ScriptCompileResult Compile(string source)
        {
            string hash = ScriptCache.Hash(source);
            if (Cache.TryGet(hash, out var cached)) return cached;

            var result = Compiler.Compile(source);
            Cache.Set(hash, result);
            return result;
        }

        /// <summary>
        /// 异步编译源码（含缓存查找，Emit 在线程池执行）。
        /// </summary>
        public async Task<ScriptCompileResult> CompileAsync(string source)
        {
            string hash = ScriptCache.Hash(source);
            if (Cache.TryGet(hash, out var cached)) return cached;

            var result = await Compiler.CompileAsync(source);
            Cache.Set(hash, result);
            return result;
        }

        /// <summary>
        /// 编译并立即在主线程执行。若编译失败，记录所有诊断并返回 <c>false</c>。
        /// </summary>
        public bool CompileAndRun(string source, GameScriptContext context)
        {
            var result = Compile(source);
            if (!result.IsSuccess)
            {
                Debug.LogError($"[ScriptRunner] 编译失败:\n{result.FormatDiagnostics()}");
                return false;
            }
            return Run(result, context);
        }

        /// <summary>
        /// 异步编译，完成后回调至 <see cref="Action{bool}"/> onDone（在调用线程执行，通常需
        /// 通过 <see cref="Framework.Threading.MainThreadDispatcher"/> 回调到主线程）。
        /// </summary>
        public async void CompileAndRunAsync(string source, GameScriptContext context,
            Action<bool> onDone = null)
        {
            var result = await CompileAsync(source);
            bool ok = result.IsSuccess && Run(result, context);
            if (!result.IsSuccess)
                Debug.LogError($"[ScriptRunner] 编译失败:\n{result.FormatDiagnostics()}");
            onDone?.Invoke(ok);
        }

        /// <summary>
        /// 执行已编译程序集中所有 <see cref="IGameScript"/> 实现。
        /// <para>
        /// • 在主线程调用（Unity API 可用）。
        /// • 每个实现类独立实例化（<c>Activator.CreateInstance</c>），不缓存实例。
        /// • 单个脚本抛出异常不会阻止其余脚本执行。
        /// </para>
        /// </summary>
        /// <returns><c>true</c> 表示至少一个脚本执行成功且无异常。</returns>
        public bool Run(ScriptCompileResult compileResult, GameScriptContext context)
        {
            if (compileResult == null) throw new ArgumentNullException(nameof(compileResult));
            if (!compileResult.IsSuccess)
            {
                Debug.LogWarning("[ScriptRunner] 尝试执行编译失败的结果，已跳过。");
                return false;
            }
            if (context == null) context = new GameScriptContext();

            var scriptTypes = FindScriptTypes(compileResult.Assembly);
            if (scriptTypes.Count == 0)
            {
                Debug.LogWarning("[ScriptRunner] 程序集中未发现实现 IGameScript 的类型。");
                return false;
            }

            bool anySuccess = false;
            long startTick  = System.Diagnostics.Stopwatch.GetTimestamp();

            foreach (var type in scriptTypes)
            {
                try
                {
                    var script = (IGameScript)Activator.CreateInstance(type);
                    script.Execute(context);
                    anySuccess = true;
                    _runCount++;
                }
                catch (Exception e)
                {
                    _errorCount++;
                    Debug.LogError($"[ScriptRunner] 脚本 '{type.Name}' 执行异常: {e}");
                }
            }

            double elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - startTick)
                               * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            RecordExecTime(elapsedMs);

            return anySuccess;
        }

        /// <summary>返回运行统计摘要。</summary>
        public string GetStats()
            => $"ScriptRunner: 累计运行={_runCount} 错误={_errorCount} | {Cache.GetStats()}";

        // ── 私有 ──────────────────────────────────────────────────────────────

        private static readonly Type GameScriptInterface = typeof(IGameScript);

        private static List<Type> FindScriptTypes(Assembly assembly)
        {
            var result = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && !type.IsInterface &&
                    GameScriptInterface.IsAssignableFrom(type) &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                {
                    result.Add(type);
                }
            }
            return result;
        }

        private void RecordExecTime(double ms)
        {
            if (_execTimes.Count >= 64)
                _execTimes.RemoveAt(0);
            _execTimes.Add(ms);
        }
    }
}
