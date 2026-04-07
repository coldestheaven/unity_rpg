#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor.CodeGen
{
    /// <summary>
    /// 领域脚手架生成器。
    ///
    /// 根据领域名称（如 "Buff"、"NPC"）自动生成以下文件，
    /// 对齐项目现有的 DAO / Repository 分层架构：
    ///
    /// <list type="bullet">
    ///   <item><c>{Domain}Data.cs</c>   — ScriptableObject 数据模型</item>
    ///   <item><c>{Domain}Database.cs</c> — RepositoryBase 子类（编辑器数据库）</item>
    ///   <item><c>{Domain}SaveDTO.cs</c>  — 可序列化的存档 DTO</item>
    ///   <item><c>{Domain}Service.cs</c>  — 可选：Runtime 服务类（MonoBehaviour 单例）</item>
    /// </list>
    ///
    /// 生成结果符合项目中 <c>RepositoryBase&lt;T&gt;</c> 的约定：
    ///   1. 声明 <c>[Serializable]</c> Entry 内嵌类；
    ///   2. 实现 <c>PopulateDictionary</c>；
    ///   3. DTO 只包含运行时存档所需的字段存根。
    /// </summary>
    public static class DomainScaffoldCodeGen
    {
        // ── 配置 ──────────────────────────────────────────────────────────────

        public sealed class DomainConfig
        {
            /// <summary>领域名称，首字母大写（如 "Buff"、"NpcDialog"）。</summary>
            public string DomainName;

            /// <summary>游戏命名空间前缀（如 "RPG" → RPG.Buffs）。</summary>
            public string GameNamespace = "RPG";

            /// <summary>输出目录（Assets/ 开头的相对路径）。</summary>
            public string OutputDirectory = "Assets/Scripts";

            /// <summary>是否生成可选的 Service（MonoBehaviour 单例）。</summary>
            public bool GenerateService = false;

            // ── 计算属性 ─────────────────────────────────────────────────────

            /// <summary>领域复数命名（简单加 s）。</summary>
            public string DomainPlural => DomainName + "s";

            /// <summary>完整游戏命名空间（如 RPG.Buffs）。</summary>
            public string GameNs => $"{GameNamespace}.{DomainPlural}";

            /// <summary>数据命名空间（RPG.Data）。</summary>
            public string DataNs => $"{GameNamespace}.Data";

            /// <summary>数据模型完整类型名（如 RPG.Buffs.BuffData）。</summary>
            public string DataFullType => $"{GameNs}.{DomainName}Data";

            /// <summary>计算后的实际输出目录（绝对路径）。</summary>
            public string AbsOutputDir => Path.GetFullPath(OutputDirectory);

            /// <summary>各子目录。</summary>
            public string ScriptsDir  => Path.Combine(AbsOutputDir, DomainPlural);
            public string DataDir     => Path.Combine(AbsOutputDir, "Data", "DTO");

            public bool Validate(out string error)
            {
                if (string.IsNullOrWhiteSpace(DomainName))
                { error = "DomainName 不能为空。"; return false; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(DomainName, @"^[A-Za-z][A-Za-z0-9]*$"))
                { error = "DomainName 只允许字母和数字，且必须以字母开头。"; return false; }
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                { error = "OutputDirectory 不能为空。"; return false; }
                error = null;
                return true;
            }
        }

        // ── 生成入口 ──────────────────────────────────────────────────────────

        /// <summary>生成所有脚手架文件并刷新 AssetDatabase。</summary>
        public static GenerateResult Generate(DomainConfig config)
        {
            if (!config.Validate(out string err))
                return GenerateResult.Fail(err);

            var created = new List<string>();

            try
            {
                Directory.CreateDirectory(config.ScriptsDir);
                Directory.CreateDirectory(config.DataDir);

                WriteFile(config.ScriptsDir, $"{config.DomainName}Data.cs",
                    BuildDataModel(config), created);

                WriteFile(config.ScriptsDir, $"{config.DomainName}Database.cs",
                    BuildDatabase(config), created);

                WriteFile(config.DataDir, $"{config.DomainName}SaveDTO.cs",
                    BuildSaveDTO(config), created);

                if (config.GenerateService)
                    WriteFile(config.ScriptsDir, $"{config.DomainName}Service.cs",
                        BuildService(config), created);

                AssetDatabase.Refresh();
                return GenerateResult.Ok(created);
            }
            catch (Exception e)
            {
                return GenerateResult.Fail($"生成时发生异常: {e.Message}");
            }
        }

        // ── 结果 ──────────────────────────────────────────────────────────────

        public struct GenerateResult
        {
            public bool Success;
            public string ErrorMessage;
            public List<string> CreatedFiles;

            public static GenerateResult Ok(List<string> files)
                => new GenerateResult { Success = true, CreatedFiles = files };

            public static GenerateResult Fail(string msg)
                => new GenerateResult { Success = false, ErrorMessage = msg };
        }

        // ── 模板 ──────────────────────────────────────────────────────────────

        private static string BuildDataModel(DomainConfig c) => $@"using UnityEngine;

namespace {c.GameNs}
{{
    /// <summary>
    /// {c.DomainName} 数据模型（ScriptableObject，由 {c.DomainName}Database 管理）。
    ///
    /// 添加游戏所需字段；<c>id</c> 用于 Repository 查找。
    /// </summary>
    [CreateAssetMenu(
        fileName = ""{c.DomainName}_new"",
        menuName  = ""RPG/Data/{c.DomainPlural}/{c.DomainName}"")]
    public sealed class {c.DomainName}Data : ScriptableObject
    {{
        [Header(""Identity"")]
        [SerializeField] public string id;
        [SerializeField] public string displayName;

        [Header(""Description"")]
        [TextArea, SerializeField] public string description;

        // TODO: 添加更多字段
    }}
}}
";

        private static string BuildDatabase(DomainConfig c) => $@"using System;
using System.Collections.Generic;
using RPG.Data;
using UnityEngine;

namespace {c.GameNs}
{{
    /// <summary>
    /// {c.DomainName} 数据库，扩展 <see cref=""RepositoryBase{{T}}""/>。
    ///
    /// 用法：通过 <c>GameDataService.Instance.{c.DomainPlural}</c>（需注册）访问。
    /// </summary>
    [CreateAssetMenu(
        fileName = ""{c.DomainName}Database"",
        menuName  = ""RPG/Databases/{c.DomainName} Database"")]
    public sealed class {c.DomainName}Database : RepositoryBase<{c.DomainName}Data>
    {{
        // ── 序列化条目 ────────────────────────────────────────────────────────

        [Serializable]
        public class Entry
        {{
            public string          id;
            public {c.DomainName}Data data;
        }}

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        // ── RepositoryBase 合约 ───────────────────────────────────────────────

        protected override void PopulateDictionary(Dictionary<string, {c.DomainName}Data> dict)
        {{
            foreach (var e in _entries)
            {{
                if (e?.data == null || string.IsNullOrEmpty(e.id))
                {{
                    Debug.LogWarning($""[{c.DomainName}Database] 跳过无效条目（id 或 data 为空）。"");
                    continue;
                }}
                dict[e.id] = e.data;
            }}
        }}
    }}
}}
";

        private static string BuildSaveDTO(DomainConfig c) => $@"using System;

namespace {c.DataNs}
{{
    /// <summary>
    /// {c.DomainName} 存档数据传输对象（DTO）。
    ///
    /// 只包含需要持久化的运行时状态；不引用 UnityEngine 类型。
    /// 由 SaveSystem / JsonSaveDAO 序列化；对应的 SaveKey 在 SaveKeys 中注册。
    /// </summary>
    [Serializable]
    public sealed class {c.DomainName}SaveDTO
    {{
        /// <summary>关联的 {c.DomainName} id（对应 {c.DomainName}Data.id）。</summary>
        public string id;

        // TODO: 添加需要持久化的字段，例如：
        // public int   level;
        // public float progress;
        // public bool  isUnlocked;
    }}
}}
";

        private static string BuildService(DomainConfig c) => $@"using Framework.Core.Patterns;
using UnityEngine;

namespace {c.GameNs}
{{
    /// <summary>
    /// {c.DomainName} 运行时服务（单例 MonoBehaviour）。
    ///
    /// 通过 <see cref=""Singleton{{T}}""/> 模式访问：
    ///   <code>{c.DomainName}Service.Instance.DoSomething();</code>
    /// </summary>
    public sealed class {c.DomainName}Service : Singleton<{c.DomainName}Service>
    {{
        // TODO: 注入数据库引用、实现服务逻辑

        protected override void Awake()
        {{
            base.Awake();
            // 初始化
        }}
    }}
}}
";

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private static void WriteFile(string dir, string fileName, string content,
                                       List<string> created)
        {
            string fullPath = Path.Combine(dir, fileName);
            if (File.Exists(fullPath))
            {
                Debug.LogWarning($"[DomainScaffoldCodeGen] 文件已存在，跳过: {fullPath}");
                return;
            }

            File.WriteAllText(fullPath, content, Encoding.UTF8);
            // 转为 Assets/ 相对路径，方便在 Unity 中打开
            string relPath = "Assets" + fullPath.Replace('\\', '/')
                                .Replace(Application.dataPath.Replace('\\', '/'), "");
            created.Add(relPath);
        }
    }
}
#endif
