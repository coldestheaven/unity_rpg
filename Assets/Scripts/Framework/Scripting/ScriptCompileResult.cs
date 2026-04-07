using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Framework.Scripting
{
    /// <summary>
    /// <see cref="RoslynCompiler.Compile"/> / <see cref="RoslynCompiler.CompileAsync"/> 的返回值。
    ///
    /// • <see cref="IsSuccess"/> 为 <c>true</c> 时 <see cref="Assembly"/> 可用。
    /// • <see cref="Diagnostics"/> 始终包含所有警告和错误（即使编译成功也可能含警告）。
    /// • 对象是不可变的，线程安全读取。
    /// </summary>
    public sealed class ScriptCompileResult
    {
        // ── 工厂方法 ──────────────────────────────────────────────────────────

        /// <summary>创建成功结果。</summary>
        public static ScriptCompileResult Success(Assembly assembly,
            IReadOnlyList<ScriptDiagnostic> diagnostics = null)
            => new ScriptCompileResult(true, assembly, diagnostics);

        /// <summary>创建失败结果（assembly 为 null）。</summary>
        public static ScriptCompileResult Failure(IReadOnlyList<ScriptDiagnostic> diagnostics)
            => new ScriptCompileResult(false, null, diagnostics);

        /// <summary>创建无 Roslyn 可用时的存根失败结果。</summary>
        public static ScriptCompileResult NotAvailable()
            => new ScriptCompileResult(false, null,
                new[] { new ScriptDiagnostic(ScriptDiagnosticSeverity.Error,
                    "SYS0001",
                    "Roslyn 编译器未安装。请运行 RPG/Scripting/Install Roslyn 安装所需 DLL，" +
                    "然后在 Player Settings 中添加脚本宏定义 ROSLYN_ENABLED。") });

        // ── 属性 ──────────────────────────────────────────────────────────────

        /// <summary>是否编译成功（可安全执行）。</summary>
        public bool IsSuccess { get; }

        /// <summary>编译产生的程序集；仅 <see cref="IsSuccess"/> == true 时非 null。</summary>
        public Assembly Assembly { get; }

        /// <summary>所有诊断消息（警告 + 错误）。</summary>
        public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }

        /// <summary>仅错误列表（快捷筛选）。</summary>
        public IEnumerable<ScriptDiagnostic> Errors
        {
            get
            {
                foreach (var d in Diagnostics)
                    if (d.IsError) yield return d;
            }
        }

        /// <summary>格式化所有诊断为多行字符串，便于在 UI / Console 显示。</summary>
        public string FormatDiagnostics()
        {
            if (Diagnostics == null || Diagnostics.Count == 0)
                return IsSuccess ? "编译成功，无诊断消息。" : "编译失败，无诊断消息。";

            var sb = new StringBuilder();
            sb.AppendLine(IsSuccess ? "✅ 编译成功" : "❌ 编译失败");
            foreach (var d in Diagnostics)
                sb.AppendLine($"  {d}");
            return sb.ToString().TrimEnd();
        }

        // ── 私有构造 ─────────────────────────────────────────────────────────

        private ScriptCompileResult(bool success, Assembly assembly,
            IReadOnlyList<ScriptDiagnostic> diagnostics)
        {
            IsSuccess   = success;
            Assembly    = assembly;
            Diagnostics = diagnostics ?? System.Array.Empty<ScriptDiagnostic>();
        }
    }
}
