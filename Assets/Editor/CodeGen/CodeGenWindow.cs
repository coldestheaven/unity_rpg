#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor.CodeGen
{
    /// <summary>
    /// RPG 代码生成器主窗口。
    ///
    /// 菜单入口：RPG / Code Generator
    ///
    /// 功能标签页：
    ///   1. 事件同步 — 检查并补全 GameEventId ↔ GameEventTypes 结构体
    ///   2. 命令工厂 — 检查 PresCommandId ↔ PresentationCommand 工厂方法
    ///   3. 领域脚手架 — 输入领域名称，一键生成 Data / Database / DTO 文件套件
    /// </summary>
    public sealed class CodeGenWindow : EditorWindow
    {
        // ── 窗口入口 ──────────────────────────────────────────────────────────

        [MenuItem("RPG/Code Generator _F5", priority = 0)]
        public static void Open()
        {
            var win = GetWindow<CodeGenWindow>("RPG Code Generator");
            win.minSize = new Vector2(520, 460);
            win.Show();
        }

        // ── 标签页枚举 ────────────────────────────────────────────────────────

        private enum Tab { EventSync, CommandSync, DomainScaffold }

        // ── 状态 ──────────────────────────────────────────────────────────────

        private Tab _currentTab = Tab.EventSync;

        // ── 事件同步 tab ──────────────────────────────────────────────────────
        private EventSyncCodeGen.SyncReport   _eventReport;
        private bool                           _eventReportDirty = true;
        private Vector2                        _eventScroll;

        // ── 命令工厂 tab ──────────────────────────────────────────────────────
        private CommandSyncCodeGen.SyncReport  _cmdReport;
        private bool                           _cmdReportDirty = true;
        private Vector2                        _cmdScroll;
        private string                         _cmdStubText = string.Empty;

        // ── 领域脚手架 tab ────────────────────────────────────────────────────
        private DomainScaffoldCodeGen.DomainConfig _domainConfig = new DomainScaffoldCodeGen.DomainConfig();
        private string  _domainError   = string.Empty;
        private string  _domainSuccess = string.Empty;
        private Vector2 _domainScroll;

        // ── 样式（延迟初始化）────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _successBox;
        private GUIStyle _errorBox;
        private GUIStyle _warnBox;
        private GUIStyle _codeStyle;
        private bool     _stylesInitialized;

        // ── Unity 回调 ────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            // ── 标签栏 ────────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            Tab selected = (Tab)GUILayout.Toolbar((int)_currentTab,
                new[] { "① 事件同步", "② 命令工厂", "③ 领域脚手架" });

            if (selected != _currentTab)
            {
                _currentTab = selected;
                // 切换 tab 时重置分析结果，下次进入时重新分析
                _eventReportDirty = true;
                _cmdReportDirty   = true;
                _domainError      = _domainSuccess = string.Empty;
            }

            EditorGUILayout.Space(2);
            Separator();

            switch (_currentTab)
            {
                case Tab.EventSync:      DrawEventSyncTab();      break;
                case Tab.CommandSync:    DrawCommandSyncTab();    break;
                case Tab.DomainScaffold: DrawDomainScaffoldTab(); break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab 1 — 事件同步
        // ══════════════════════════════════════════════════════════════════════

        private void DrawEventSyncTab()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("事件系统同步", _headerStyle);
            EditorGUILayout.HelpBox(
                "扫描 GameEventId.cs（枚举）和 GameEventTypes.cs（结构体），\n" +
                "列出所有缺少对应 `IGameEvent` 结构体的枚举值，并可一键生成。",
                MessageType.None);

            EditorGUILayout.Space(6);

            // ── 扫描按钮 ──────────────────────────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🔍 扫描", GUILayout.Width(90)))
                {
                    _eventReport      = EventSyncCodeGen.Analyse();
                    _eventReportDirty = false;
                }

                if (!_eventReportDirty && !_eventReport.HasError && !_eventReport.AllSynced)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button($"✨ 生成 {_eventReport.MissingStructIds.Count} 个缺失结构体",
                        GUILayout.Width(220)))
                    {
                        EventSyncCodeGen.GenerateMissing(_eventReport.MissingStructIds);
                        _eventReportDirty = true;
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.Space(4);

            // ── 结果显示 ──────────────────────────────────────────────────────
            if (_eventReportDirty)
            {
                EditorGUILayout.HelpBox("点击「扫描」以分析事件文件。", MessageType.Info);
                return;
            }

            if (_eventReport.HasError)
            {
                EditorGUILayout.HelpBox(_eventReport.ErrorMessage, MessageType.Error);
                return;
            }

            if (_eventReport.AllSynced)
            {
                EditorGUILayout.HelpBox(
                    $"✅ 全部同步！共 {_eventReport.AllEventIds.Count} 个事件，无缺失结构体。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"⚠ 发现 {_eventReport.MissingStructIds.Count} 个缺失结构体：",
                    MessageType.Warning);
            }

            // 统计信息
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                Stat("枚举值总数",  _eventReport.AllEventIds.Count.ToString());
                Stat("已有结构体", _eventReport.ExistingStructNames.Count.ToString());
                Stat("缺失结构体", _eventReport.MissingStructIds.Count.ToString(),
                    _eventReport.MissingStructIds.Count > 0 ? Color.yellow : Color.white);
            }

            EditorGUILayout.Space(4);

            // 缺失列表
            if (_eventReport.MissingStructIds.Count > 0)
            {
                EditorGUILayout.LabelField("缺失列表", EditorStyles.boldLabel);
                _eventScroll = EditorGUILayout.BeginScrollView(_eventScroll,
                    GUILayout.Height(Mathf.Min(200, _eventReport.MissingStructIds.Count * 22 + 8)));
                foreach (var id in _eventReport.MissingStructIds)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(8);
                        EditorGUILayout.LabelField($"• {id}Event", _codeStyle);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab 2 — 命令工厂
        // ══════════════════════════════════════════════════════════════════════

        private void DrawCommandSyncTab()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("命令工厂检查", _headerStyle);
            EditorGUILayout.HelpBox(
                "对比 PresCommandId.cs（枚举）与 PresentationCommand.cs（静态工厂方法），\n" +
                "找出缺少工厂方法的命令 ID，并生成代码存根供复制使用。",
                MessageType.None);

            EditorGUILayout.Space(6);

            if (GUILayout.Button("🔍 扫描", GUILayout.Width(90)))
            {
                _cmdReport      = CommandSyncCodeGen.Analyse();
                _cmdReportDirty = false;
                _cmdStubText    = _cmdReport.MissingFactories.Count > 0
                    ? CommandSyncCodeGen.BuildStubSnippet(_cmdReport.MissingFactories)
                    : string.Empty;
            }

            EditorGUILayout.Space(4);

            if (_cmdReportDirty)
            {
                EditorGUILayout.HelpBox("点击「扫描」以分析命令文件。", MessageType.Info);
                return;
            }

            if (_cmdReport.HasError)
            {
                EditorGUILayout.HelpBox(_cmdReport.ErrorMessage, MessageType.Error);
                return;
            }

            if (_cmdReport.AllSynced)
            {
                EditorGUILayout.HelpBox(
                    $"✅ 全部同步！共 {_cmdReport.AllCommandIds.Count} 个命令，均有工厂方法。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"⚠ {_cmdReport.MissingFactories.Count} 个命令缺少工厂方法（见下方存根）。",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                Stat("命令总数",    _cmdReport.AllCommandIds.Count.ToString());
                Stat("已有工厂",   _cmdReport.ExistingFactories.Count.ToString());
                Stat("缺少工厂",   _cmdReport.MissingFactories.Count.ToString(), Color.yellow);
            }

            EditorGUILayout.Space(4);

            // ── 代码存根 ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("代码存根（复制到 PresentationCommand.cs）：",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("复制到剪贴板", GUILayout.Width(140)))
                    GUIUtility.systemCopyBuffer = _cmdStubText;
            }

            _cmdScroll = EditorGUILayout.BeginScrollView(_cmdScroll, GUILayout.Height(180));
            EditorGUILayout.TextArea(_cmdStubText, _codeStyle,
                GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab 3 — 领域脚手架
        // ══════════════════════════════════════════════════════════════════════

        private void DrawDomainScaffoldTab()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("领域脚手架生成", _headerStyle);
            EditorGUILayout.HelpBox(
                "输入领域名称，一键生成完整的分层代码套件：\n" +
                "  • {Domain}Data.cs       — ScriptableObject 数据模型\n" +
                "  • {Domain}Database.cs   — RepositoryBase 子类（数据库）\n" +
                "  • {Domain}SaveDTO.cs    — 存档数据传输对象（DTO）\n" +
                "  • {Domain}Service.cs    — 运行时服务（可选）",
                MessageType.None);

            EditorGUILayout.Space(8);

            _domainScroll = EditorGUILayout.BeginScrollView(_domainScroll);

            // ── 参数输入 ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("基本配置", EditorStyles.boldLabel);

            _domainConfig.DomainName = EditorGUILayout.TextField(
                new GUIContent("领域名称 *",
                    "首字母大写，如 Buff / NpcDialog / Shop（只含字母数字）"),
                _domainConfig.DomainName ?? string.Empty);

            _domainConfig.GameNamespace = EditorGUILayout.TextField(
                new GUIContent("命名空间前缀",
                    "如 RPG → 生成 RPG.Buffs.BuffData"),
                _domainConfig.GameNamespace ?? "RPG");

            _domainConfig.OutputDirectory = EditorGUILayout.TextField(
                new GUIContent("输出目录",
                    "Assets/ 开头的相对路径，如 Assets/Scripts"),
                _domainConfig.OutputDirectory ?? "Assets/Scripts");

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("可选文件", EditorStyles.boldLabel);
            _domainConfig.GenerateService = EditorGUILayout.Toggle(
                new GUIContent("生成 Service.cs",
                    "生成一个继承 Singleton<T> 的 MonoBehaviour 服务类"),
                _domainConfig.GenerateService);

            EditorGUILayout.Space(8);

            // ── 预览 ──────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(_domainConfig.DomainName))
            {
                EditorGUILayout.LabelField("生成预览", EditorStyles.boldLabel);
                string ns     = _domainConfig.GameNamespace;
                string plural = _domainConfig.DomainName + "s";
                string dom    = _domainConfig.DomainName;
                string outDir = _domainConfig.OutputDirectory;

                EditorGUILayout.LabelField(
                    $"  {outDir}/{plural}/{dom}Data.cs", _codeStyle);
                EditorGUILayout.LabelField(
                    $"  {outDir}/{plural}/{dom}Database.cs", _codeStyle);
                EditorGUILayout.LabelField(
                    $"  {outDir}/Data/DTO/{dom}SaveDTO.cs", _codeStyle);
                if (_domainConfig.GenerateService)
                    EditorGUILayout.LabelField(
                        $"  {outDir}/{plural}/{dom}Service.cs", _codeStyle);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    $"命名空间: {ns}.{plural} / {ns}.Data", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);

            // ── 生成按钮 ──────────────────────────────────────────────────────
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("✨ 生成脚手架文件", GUILayout.Height(32)))
            {
                _domainError   = string.Empty;
                _domainSuccess = string.Empty;

                var result = DomainScaffoldCodeGen.Generate(_domainConfig);
                if (result.Success)
                {
                    _domainSuccess = $"✅ 成功生成 {result.CreatedFiles.Count} 个文件：\n" +
                                     string.Join("\n", result.CreatedFiles);
                    Debug.Log("[CodeGenWindow] " + _domainSuccess);
                }
                else
                {
                    _domainError = "❌ " + result.ErrorMessage;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();

            // ── 结果提示 ──────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            if (!string.IsNullOrEmpty(_domainSuccess))
                EditorGUILayout.HelpBox(_domainSuccess, MessageType.Info);
            if (!string.IsNullOrEmpty(_domainError))
                EditorGUILayout.HelpBox(_domainError, MessageType.Error);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 共用工具
        // ══════════════════════════════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 14
            };

            _successBox = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.2f, 0.7f, 0.2f) }
            };

            _errorBox = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.9f, 0.2f, 0.2f) }
            };

            _warnBox = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 0.1f) }
            };

            _codeStyle = new GUIStyle(EditorStyles.label)
            {
                font      = Resources.Load<Font>("Fonts/RobotoMono-Regular") ??
                             EditorStyles.label.font,
                fontSize  = 11,
                wordWrap  = true,
                normal    = { textColor = new Color(0.7f, 0.9f, 1f) }
            };
        }

        private static void Separator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f));
            EditorGUILayout.Space(2);
        }

        private static void Stat(string label, string value, Color? valueColor = null)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox,
                GUILayout.Width(120)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                var old = GUI.color;
                if (valueColor.HasValue) GUI.color = valueColor.Value;
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
                GUI.color = old;
            }
        }
    }
}
#endif
