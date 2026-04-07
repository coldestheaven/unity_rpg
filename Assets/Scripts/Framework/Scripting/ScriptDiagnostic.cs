namespace Framework.Scripting
{
    /// <summary>Roslyn 诊断严重性，对齐 <c>Microsoft.CodeAnalysis.DiagnosticSeverity</c>。</summary>
    public enum ScriptDiagnosticSeverity
    {
        Hidden  = 0,
        Info    = 1,
        Warning = 2,
        Error   = 3
    }

    /// <summary>
    /// 编译诊断消息（错误 / 警告 / 提示）的平台无关封装。
    ///
    /// 由 <see cref="RoslynCompiler"/> 生成，收录在 <see cref="ScriptCompileResult.Diagnostics"/> 中。
    /// 不直接暴露 <c>Microsoft.CodeAnalysis.Diagnostic</c>，以便在 Roslyn 未安装时代码仍可编译。
    /// </summary>
    public readonly struct ScriptDiagnostic
    {
        /// <summary>诊断严重性。</summary>
        public readonly ScriptDiagnosticSeverity Severity;

        /// <summary>Roslyn 诊断码（如 "CS0103"）。</summary>
        public readonly string Id;

        /// <summary>可读消息。</summary>
        public readonly string Message;

        /// <summary>源文件路径（动态编译时为空）。</summary>
        public readonly string FilePath;

        /// <summary>行号（1-based；无法确定时为 -1）。</summary>
        public readonly int Line;

        /// <summary>列号（1-based；无法确定时为-1）。</summary>
        public readonly int Column;

        public ScriptDiagnostic(
            ScriptDiagnosticSeverity severity,
            string id, string message,
            string filePath = "",
            int line = -1, int column = -1)
        {
            Severity = severity;
            Id       = id ?? string.Empty;
            Message  = message ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            Line     = line;
            Column   = column;
        }

        public bool IsError   => Severity == ScriptDiagnosticSeverity.Error;
        public bool IsWarning => Severity == ScriptDiagnosticSeverity.Warning;

        public override string ToString()
        {
            string loc = Line >= 0 ? $"({Line},{Column})" : string.Empty;
            return $"{Severity} {Id}{loc}: {Message}";
        }
    }
}
