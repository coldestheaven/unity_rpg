#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Editor.CodeGen
{
    /// <summary>
    /// 命令系统同步检查器。
    ///
    /// 对比 <c>PresCommandId.cs</c>（枚举）与 <c>PresentationCommand.cs</c>（工厂方法），
    /// 找出所有没有对应静态工厂方法的枚举值，并在 Editor 窗口中报告。
    ///
    /// 注意：命令的 payload 设计因需求各异（float 槽/int 槽的用途不同），
    /// 本工具只做 <b>检查报告</b>，不自动生成工厂方法，留由开发者手动补充。
    /// </summary>
    public static class CommandSyncCodeGen
    {
        public const string CommandIdRelPath  = "Assets/Scripts/Framework/Presentation/PresCommandId.cs";
        public const string CommandRelPath    = "Assets/Scripts/Framework/Presentation/PresentationCommand.cs";

        // ── 分析结果 ──────────────────────────────────────────────────────────

        public struct SyncReport
        {
            /// <summary>PresCommandId 枚举中所有有效值。</summary>
            public List<string> AllCommandIds;
            /// <summary>PresentationCommand.cs 中已存在的静态工厂方法名称。</summary>
            public List<string> ExistingFactories;
            /// <summary>缺少对应工厂方法的枚举值列表。</summary>
            public List<string> MissingFactories;
            public string ErrorMessage;

            public bool HasError   => !string.IsNullOrEmpty(ErrorMessage);
            public bool AllSynced  => !HasError && (MissingFactories == null || MissingFactories.Count == 0);
        }

        public static SyncReport Analyse()
        {
            var report = new SyncReport
            {
                AllCommandIds    = new List<string>(),
                ExistingFactories = new List<string>(),
                MissingFactories = new List<string>()
            };

            // ── 解析 PresCommandId.cs ─────────────────────────────────────────
            string idPath = Path.GetFullPath(CommandIdRelPath);
            if (!File.Exists(idPath))
            {
                report.ErrorMessage = $"找不到文件: {CommandIdRelPath}";
                return report;
            }

            try
            {
                report.AllCommandIds = EventSyncCodeGen.ParseEnumValues(
                    File.ReadAllText(idPath), "PresCommandId");
            }
            catch (Exception e)
            {
                report.ErrorMessage = $"解析 PresCommandId.cs 时出错: {e.Message}";
                return report;
            }

            // ── 解析 PresentationCommand.cs ────────────────────────────────────
            string cmdPath = Path.GetFullPath(CommandRelPath);
            if (!File.Exists(cmdPath))
            {
                report.ErrorMessage = $"找不到文件: {CommandRelPath}";
                return report;
            }

            try
            {
                report.ExistingFactories = ParseFactoryMethodNames(File.ReadAllText(cmdPath));
            }
            catch (Exception e)
            {
                report.ErrorMessage = $"解析 PresentationCommand.cs 时出错: {e.Message}";
                return report;
            }

            // ── 差集计算 ───────────────────────────────────────────────────────
            var existingSet = new HashSet<string>(report.ExistingFactories);
            foreach (string id in report.AllCommandIds)
            {
                if (!existingSet.Contains(id))
                    report.MissingFactories.Add(id);
            }

            return report;
        }

        /// <summary>
        /// 生成缺少工厂方法的存根代码片段（复制到剪贴板），方便开发者补充。
        /// </summary>
        public static string BuildStubSnippet(IReadOnlyList<string> missingIds)
        {
            if (missingIds == null || missingIds.Count == 0)
                return string.Empty;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// ── 待补充工厂方法（请填写 payload 注释和参数）────────────────────────");
            foreach (string id in missingIds)
            {
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>TODO: 填写 payload 说明。</summary>");
                sb.AppendLine($"        public static PresentationCommand {id}(/* 参数 */)");
                sb.AppendLine($"            => new PresentationCommand(PresCommandId.{id} /*, payload */);");
            }
            return sb.ToString();
        }

        // ── 内部解析 ─────────────────────────────────────────────────────────

        internal static List<string> ParseFactoryMethodNames(string source)
        {
            var result = new List<string>();
            // 匹配 `public static PresentationCommand MethodName(`
            foreach (Match m in Regex.Matches(
                source,
                @"public\s+static\s+PresentationCommand\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("))
            {
                result.Add(m.Groups[1].Value);
            }
            return result;
        }
    }
}
#endif
