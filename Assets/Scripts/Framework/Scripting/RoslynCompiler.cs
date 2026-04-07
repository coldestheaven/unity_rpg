// ────────────────────────────────────────────────────────────────────────────
//  RoslynCompiler.cs
//
//  前置条件：
//    1. 将以下 DLL 放入  Assets/Plugins/Roslyn/  并确保 Inspector 里
//       Platform 只勾选 Editor + Standalone（不要勾 WebGL / IL2CPP）：
//         • Microsoft.CodeAnalysis.dll
//         • Microsoft.CodeAnalysis.CSharp.dll
//         • System.Collections.Immutable.dll
//         • System.Reflection.Metadata.dll
//         • System.Runtime.CompilerServices.Unsafe.dll
//       可通过 RPG/Scripting/Install Roslyn DLL 自动下载安装。
//
//    2. 在 Edit > Project Settings > Player > Scripting Define Symbols 中
//       添加  ROSLYN_ENABLED  ，使本文件的实现代码参与编译。
//
//  若 ROSLYN_ENABLED 未定义，所有 Compile* 方法返回 ScriptCompileResult.NotAvailable()。
// ────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

#if ROSLYN_ENABLED
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Framework.Scripting
{
    /// <summary>
    /// 将 C# 源码编译为内存中的 <see cref="Assembly"/>，封装 Roslyn CSharpCompilation API。
    ///
    /// <para>
    /// 线程安全：<see cref="Compile"/> 可在任意线程调用（Unity API 调用仅在构造时）。
    /// <see cref="CompileAsync"/> 将耗时的 Emit 转移到线程池。
    /// </para>
    ///
    /// 典型用法：
    /// <code>
    /// var compiler = new RoslynCompiler();
    /// var result   = compiler.Compile(sourceCode);
    /// if (result.IsSuccess)
    ///     ScriptRunner.Shared.Run(result, scriptContext);
    /// else
    ///     Debug.LogError(result.FormatDiagnostics());
    /// </code>
    /// </summary>
    public sealed class RoslynCompiler
    {
#if ROSLYN_ENABLED

        // ── Roslyn 配置 ───────────────────────────────────────────────────────

        private static readonly CSharpParseOptions ParseOptions =
            CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.CSharp9)
                .WithDocumentationMode(DocumentationMode.None);

        private static readonly CSharpCompilationOptions CompileOptions =
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel:       OptimizationLevel.Release,
                allowUnsafe:             false,
                nullableContextOptions:  NullableContextOptions.Disable,
                // 不允许脚本绕过 CA 抑制特性
                specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic>
                {
                    ["CS1701"] = ReportDiagnostic.Suppress, // 程序集版本不匹配
                    ["CS1702"] = ReportDiagnostic.Suppress,
                });

        // ── 引用 ──────────────────────────────────────────────────────────────

        // 延迟构建，因 AppDomain.GetAssemblies 需在主线程首次调用后才能获取所有项目程序集
        private static IReadOnlyList<MetadataReference> _cachedReferences;
        private static readonly object _refLock = new object();

        private static IReadOnlyList<MetadataReference> GetOrBuildReferences(
            IEnumerable<Assembly> extra)
        {
            // 如果有额外程序集，每次重建（不常见路径）
            if (extra != null)
                return BuildReferences(extra);

            lock (_refLock)
            {
                if (_cachedReferences == null)
                    _cachedReferences = BuildReferences(null);
                return _cachedReferences;
            }
        }

        private static List<MetadataReference> BuildReferences(IEnumerable<Assembly> extra)
        {
            var refs    = new List<MetadataReference>(128);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(Assembly asm)
            {
                if (asm == null || asm.IsDynamic) return;
                string loc = asm.Location;
                if (string.IsNullOrEmpty(loc) || !visited.Add(loc)) return;
                if (!File.Exists(loc)) return;
                try
                {
                    refs.Add(MetadataReference.CreateFromFile(loc));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[RoslynCompiler] 无法加载引用 '{loc}': {e.Message}");
                }
            }

            // 所有已加载的 AppDomain 程序集（包含 UnityEngine、项目脚本等）
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                TryAdd(asm);

            // 额外指定的程序集
            if (extra != null)
                foreach (var asm in extra)
                    TryAdd(asm);

            return refs;
        }

        // ── 状态 ──────────────────────────────────────────────────────────────

        private readonly IEnumerable<Assembly> _extraAssemblies;

        /// <summary>调用 Compile 的次数。</summary>
        public int CompileCount { get; private set; }

#endif // ROSLYN_ENABLED

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <param name="extraAssemblies">
        /// 需要额外纳入引用的程序集（不在 AppDomain 中的情况，通常无需指定）。
        /// </param>
        public RoslynCompiler(IEnumerable<Assembly> extraAssemblies = null)
        {
#if ROSLYN_ENABLED
            _extraAssemblies = extraAssemblies;
#endif
        }

        /// <summary>
        /// 同步编译 C# 源码。建议仅在非性能敏感路径（Editor 工具、启动期）使用；
        /// 运行时热编译请用 <see cref="CompileAsync"/>。
        /// </summary>
        /// <param name="source">完整的 C# 源码字符串。</param>
        /// <param name="assemblyName">生成程序集的名称（留空则自动生成唯一名）。</param>
        public ScriptCompileResult Compile(string source, string assemblyName = null)
        {
#if ROSLYN_ENABLED
            if (string.IsNullOrWhiteSpace(source))
                return ScriptCompileResult.Failure(
                    new[] { new ScriptDiagnostic(ScriptDiagnosticSeverity.Error,
                        "SYS0002", "源码不能为空。") });

            CompileCount++;
            string name = string.IsNullOrEmpty(assemblyName)
                ? $"RPGScript_{CompileCount:000}"
                : assemblyName;

            try
            {
                return DoCompile(source, name);
            }
            catch (Exception e)
            {
                return ScriptCompileResult.Failure(
                    new[] { new ScriptDiagnostic(ScriptDiagnosticSeverity.Error,
                        "SYS0099", $"编译器内部异常: {e.Message}") });
            }
#else
            return ScriptCompileResult.NotAvailable();
#endif
        }

        /// <summary>
        /// 异步编译 C# 源码（Emit 操作在线程池执行，不阻塞主线程）。
        /// </summary>
        public Task<ScriptCompileResult> CompileAsync(string source, string assemblyName = null)
        {
#if ROSLYN_ENABLED
            if (string.IsNullOrWhiteSpace(source))
                return Task.FromResult(ScriptCompileResult.Failure(
                    new[] { new ScriptDiagnostic(ScriptDiagnosticSeverity.Error,
                        "SYS0002", "源码不能为空。") }));

            CompileCount++;
            string name = string.IsNullOrEmpty(assemblyName)
                ? $"RPGScript_{CompileCount:000}"
                : assemblyName;

            return Task.Run(() =>
            {
                try
                {
                    return DoCompile(source, name);
                }
                catch (Exception e)
                {
                    return ScriptCompileResult.Failure(
                        new[] { new ScriptDiagnostic(ScriptDiagnosticSeverity.Error,
                            "SYS0099", $"编译器内部异常: {e.Message}") });
                }
            });
#else
            return Task.FromResult(ScriptCompileResult.NotAvailable());
#endif
        }

        /// <summary>
        /// 失效已缓存的全局程序集引用列表，下次编译时重新收集。
        /// 当动态加载了新程序集后调用此方法。
        /// </summary>
        public static void InvalidateReferenceCache()
        {
#if ROSLYN_ENABLED
            lock (_refLock)
                _cachedReferences = null;
#endif
        }

        // ── 私有实现 ──────────────────────────────────────────────────────────

#if ROSLYN_ENABLED
        private ScriptCompileResult DoCompile(string source, string name)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions);
            var refs       = GetOrBuildReferences(_extraAssemblies);

            var compilation = CSharpCompilation.Create(
                name,
                syntaxTrees: new[] { syntaxTree },
                references:  refs,
                options:     CompileOptions);

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            // 收集诊断（警告 + 错误）
            var diagnostics = new List<ScriptDiagnostic>(emitResult.Diagnostics.Length);
            foreach (var d in emitResult.Diagnostics)
            {
                if (d.Severity < DiagnosticSeverity.Warning) continue;

                var span    = d.Location.GetLineSpan();
                diagnostics.Add(new ScriptDiagnostic(
                    severity: (ScriptDiagnosticSeverity)(int)d.Severity,
                    id:       d.Id,
                    message:  d.GetMessage(),
                    filePath: span.Path,
                    line:     span.StartLinePosition.Line + 1,
                    column:   span.StartLinePosition.Character + 1));
            }

            if (!emitResult.Success)
                return ScriptCompileResult.Failure(diagnostics);

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            return ScriptCompileResult.Success(assembly, diagnostics);
        }
#endif
    }
}
