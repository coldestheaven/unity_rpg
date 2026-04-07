#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Editor.CodeGen
{
    /// <summary>
    /// 事件系统同步生成器。
    ///
    /// 解析 <c>GameEventId.cs</c>（枚举定义）和 <c>GameEventTypes.cs</c>（事件结构体），
    /// 找出所有没有对应 <c>readonly struct {Id}Event : IGameEvent</c> 的枚举值，
    /// 并可一键补全到 <c>GameEventTypes.cs</c> 文件末尾。
    ///
    /// 命名约定：
    ///   GameEventId.PlayerDied  →  struct PlayerDiedEvent : IGameEvent
    ///   GameEventId.HealthChanged → struct HealthChangedEvent : IGameEvent
    /// </summary>
    public static class EventSyncCodeGen
    {
        // ── 文件路径（相对于项目根目录）──────────────────────────────────────
        public const string EventIdRelPath    = "Assets/Scripts/Framework/Core/Events/GameEventId.cs";
        public const string EventTypesRelPath = "Assets/Scripts/Framework/Core/Events/GameEventTypes.cs";

        // ── 分析结果 ──────────────────────────────────────────────────────────

        public struct SyncReport
        {
            /// <summary>GameEventId 枚举中所有有效值（已排除 _Count）。</summary>
            public List<string> AllEventIds;
            /// <summary>GameEventTypes.cs 中已存在的 struct 名称。</summary>
            public List<string> ExistingStructNames;
            /// <summary>缺少对应 struct 的枚举值名称列表。</summary>
            public List<string> MissingStructIds;
            /// <summary>文件读取或解析时的错误信息。</summary>
            public string ErrorMessage;

            public bool HasError   => !string.IsNullOrEmpty(ErrorMessage);
            public bool AllSynced  => !HasError && (MissingStructIds == null || MissingStructIds.Count == 0);
        }

        /// <summary>
        /// 分析两个文件，返回同步状态报告。不修改任何文件。
        /// </summary>
        public static SyncReport Analyse()
        {
            var report = new SyncReport
            {
                AllEventIds       = new List<string>(),
                ExistingStructNames = new List<string>(),
                MissingStructIds  = new List<string>()
            };

            // ── 读取 GameEventId.cs ───────────────────────────────────────────
            string idPath = Path.GetFullPath(EventIdRelPath);
            if (!File.Exists(idPath))
            {
                report.ErrorMessage = $"找不到文件: {EventIdRelPath}";
                return report;
            }

            try
            {
                report.AllEventIds = ParseEnumValues(File.ReadAllText(idPath), "GameEventId");
            }
            catch (Exception e)
            {
                report.ErrorMessage = $"解析 GameEventId.cs 时出错: {e.Message}";
                return report;
            }

            // ── 读取 GameEventTypes.cs ────────────────────────────────────────
            string typesPath = Path.GetFullPath(EventTypesRelPath);
            if (!File.Exists(typesPath))
            {
                report.ErrorMessage = $"找不到文件: {EventTypesRelPath}";
                return report;
            }

            try
            {
                report.ExistingStructNames = ParseReadonlyStructNames(File.ReadAllText(typesPath));
            }
            catch (Exception e)
            {
                report.ErrorMessage = $"解析 GameEventTypes.cs 时出错: {e.Message}";
                return report;
            }

            // ── 差集计算 ───────────────────────────────────────────────────────
            var existingSet = new HashSet<string>(report.ExistingStructNames);
            foreach (string id in report.AllEventIds)
            {
                if (!existingSet.Contains(id + "Event"))
                    report.MissingStructIds.Add(id);
            }

            return report;
        }

        /// <summary>
        /// 将所有 <paramref name="missingIds"/> 对应的空事件结构体追加到 <c>GameEventTypes.cs</c>。
        /// <para>
        /// 生成的结构体仅包含 <c>EventId</c> 属性，开发者可按需手动添加 payload 字段。
        /// </para>
        /// </summary>
        public static void GenerateMissing(IReadOnlyList<string> missingIds)
        {
            if (missingIds == null || missingIds.Count == 0)
            {
                Debug.Log("[EventSyncCodeGen] 没有需要生成的事件结构体。");
                return;
            }

            string path = Path.GetFullPath(EventTypesRelPath);
            if (!File.Exists(path))
            {
                Debug.LogError($"[EventSyncCodeGen] 文件不存在: {path}");
                return;
            }

            string original = File.ReadAllText(path);

            // 找到命名空间的最后一个 `}` — 在它之前插入新内容
            int lastBrace = original.LastIndexOf('}');
            if (lastBrace < 0)
            {
                Debug.LogError("[EventSyncCodeGen] 无法在 GameEventTypes.cs 中找到闭合的 `}`。");
                return;
            }

            var sb = new StringBuilder();
            sb.Append(original, 0, lastBrace);
            sb.AppendLine();
            sb.AppendLine(
                "    // ── 自动生成 by EventSyncCodeGen ─────────────────────────────────────────");
            sb.AppendLine(
                "    // 以下结构体由代码生成器填充，payload 字段请手动补充。");
            sb.AppendLine();

            foreach (string id in missingIds)
            {
                sb.AppendLine($"    /// <summary>对应 <see cref=\"GameEventId.{id}\"/>（自动生成，请补充 payload）。</summary>");
                sb.AppendLine($"    public readonly struct {id}Event : IGameEvent");
                sb.AppendLine("    {");
                sb.AppendLine($"        public GameEventId EventId => GameEventId.{id};");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"[EventSyncCodeGen] 已在 GameEventTypes.cs 中生成 {missingIds.Count} 个事件结构体：" +
                      string.Join(", ", missingIds));
        }

        // ── 内部解析工具 ──────────────────────────────────────────────────────

        /// <summary>解析 C# 枚举体，返回所有有效成员名（不含 <c>_Count</c> 等哨兵值）。</summary>
        internal static List<string> ParseEnumValues(string source, string enumName)
        {
            var result = new List<string>();

            // 匹配枚举体
            var bodyMatch = Regex.Match(
                source,
                $@"enum\s+{Regex.Escape(enumName)}\s*(?::\s*\w+\s*)?\{{([^}}]+)\}}",
                RegexOptions.Singleline);

            if (!bodyMatch.Success) return result;

            string body = bodyMatch.Groups[1].Value;

            // 逐行提取成员名（兼容行内注释、等号赋值）
            foreach (Match m in Regex.Matches(
                body,
                @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?:=\s*[^,\n]+)?\s*,?",
                RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                // 排除哨兵值
                if (name.StartsWith("_", StringComparison.Ordinal)) continue;
                result.Add(name);
            }

            return result;
        }

        /// <summary>从 C# 源码中提取所有 <c>readonly struct</c> 的类型名。</summary>
        internal static List<string> ParseReadonlyStructNames(string source)
        {
            var result = new List<string>();
            foreach (Match m in Regex.Matches(
                source,
                @"\breadonly\s+struct\s+([A-Za-z_][A-Za-z0-9_]*)"))
            {
                result.Add(m.Groups[1].Value);
            }
            return result;
        }
    }
}
#endif
