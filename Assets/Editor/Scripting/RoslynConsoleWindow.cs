#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.Scripting;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripting
{
    /// <summary>
    /// RPG C# 脚本控制台 — 在运行时动态编译并执行 C# 代码片段（Roslyn REPL）。
    ///
    /// 打开方式：菜单 RPG / Scripting / C# Console  或  Shift+F6
    ///
    /// 功能：
    ///   • 编辑 C# 源码，一键编译 + 执行
    ///   • 诊断面板显示错误 / 警告（含行号）
    ///   • 历史记录（最近 20 条，可快速重载）
    ///   • 模板选择（空白 / EventBus 示例 / 伤害测试 / 日志打印）
    ///   • 缓存 / 统计信息面板
    ///   • 快捷键：Ctrl+Enter = 编译并运行
    /// </summary>
    public sealed class RoslynConsoleWindow : EditorWindow
    {
        // ── 菜单 ──────────────────────────────────────────────────────────────

        [MenuItem("RPG/Scripting/C# Console _#F6", priority = 10)]
        public static void Open()
        {
            var win = GetWindow<RoslynConsoleWindow>("C# Console");
            win.minSize = new Vector2(640, 480);
            win.Show();
        }

        // ── 状态 ──────────────────────────────────────────────────────────────

        private string _source        = DefaultTemplate;
        private string _outputLog     = string.Empty;
        private bool   _isCompiling   = false;
        private bool   _showHistory   = false;
        private bool   _showStats     = false;
        private int    _templateIndex = 0;

        private Vector2 _sourceScroll;
        private Vector2 _outputScroll;
        private Vector2 _historyScroll;

        private ScriptRunner _runner;
        private GameScriptContext _context;

        // 历史记录（最近 20 条）
        private readonly List<string> _history = new List<string>(20);

        // ── 样式（延迟初始化）────────────────────────────────────────────────

        private GUIStyle _codeStyle;
        private GUIStyle _outputStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _successStyle;
        private bool     _stylesReady;

        // ── 模板 ──────────────────────────────────────────────────────────────

        private static readonly string[] TemplateNames =
        {
            "空白脚本",
            "打印 Hello",
            "发布 PlayerDied 事件",
            "查询场景根对象",
            "加经验 + 金币",
        };

        private static readonly string[] Templates =
        {
            // 空白
            @"using Framework.Scripting;

public class MyScript : IGameScript
{
    public void Execute(GameScriptContext ctx)
    {
        // 在此编写逻辑...
        ctx.Log(""Hello from Roslyn!"");
    }
}",
            // 打印
            @"using Framework.Scripting;
using UnityEngine;

public class HelloScript : IGameScript
{
    public void Execute(GameScriptContext ctx)
    {
        ctx.Log($""当前场景: {ctx.CurrentScene} | 游戏时间: {ctx.GameTime:F1}s"");
    }
}",
            // 发布事件
            @"using Framework.Scripting;
using Framework.Events;

public class FireEventScript : IGameScript
{
    public void Execute(GameScriptContext ctx)
    {
        ctx.Publish(new PlayerDiedEvent());
        ctx.Log(""已发布 PlayerDied 事件"");
    }
}",
            // 场景查询
            @"using Framework.Scripting;
using UnityEngine;

public class SceneQueryScript : IGameScript
{
    public void Execute(GameScriptContext ctx)
    {
        var root = ctx.Find(""GameManager"");
        if (root != null)
            ctx.Log($""找到 GameManager: {root.name}"");
        else
            ctx.LogWarning(""场景中未找到 GameManager 对象"");
    }
}",
            // 加经验
            @"using Framework.Scripting;

public class GainXPScript : IGameScript
{
    public void Execute(GameScriptContext ctx)
    {
        ctx.AddExperience(500);
        ctx.AddGold(100);
        ctx.Log(""已通过逻辑线程追加 500 XP + 100 金币"");
    }
}",
        };

        private static string DefaultTemplate => Templates[0];

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _runner  = new ScriptRunner();
            _context = new GameScriptContext();
            _stylesReady = false;
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();
            HandleKeyboardShortcuts();

            DrawToolbar();
            EditorGUILayout.Space(2);

            float totalH    = position.height - 32; // 留 toolbar
            float editorH   = Mathf.Max(totalH * 0.55f, 120f);
            float outputH   = Mathf.Max(totalH - editorH - 8, 60f);

            DrawSourceEditor(editorH);
            EditorGUILayout.Space(4);
            DrawOutput(outputH);

            if (_showHistory) DrawHistory();
            if (_showStats)   DrawStats();
        }

        // ── 工具栏 ────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 模板下拉
                EditorGUI.BeginChangeCheck();
                _templateIndex = EditorGUILayout.Popup(_templateIndex, TemplateNames,
                    EditorStyles.toolbarPopup, GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck())
                    _source = Templates[_templateIndex];

                GUILayout.Space(6);

                // 清空
                if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(44)))
                    _source = string.Empty;

                GUILayout.FlexibleSpace();

                // 历史 / 统计 开关
                _showHistory = GUILayout.Toggle(_showHistory, "历史",
                    EditorStyles.toolbarButton, GUILayout.Width(48));
                _showStats = GUILayout.Toggle(_showStats, "统计",
                    EditorStyles.toolbarButton, GUILayout.Width(48));

                GUILayout.Space(6);

                // 编译
                GUI.enabled = !_isCompiling;
                if (GUILayout.Button("▶ 编译 + 运行 (Ctrl+↵)", EditorStyles.toolbarButton,
                    GUILayout.Width(168)))
                    CompileAndRun();
                GUI.enabled = true;

                // 仅编译（不运行）
                if (GUILayout.Button("⚙ 仅编译", EditorStyles.toolbarButton, GUILayout.Width(68)))
                    CompileOnly();
            }
        }

        // ── 代码编辑器 ────────────────────────────────────────────────────────

        private void DrawSourceEditor(float height)
        {
            EditorGUILayout.LabelField("C# 源码", EditorStyles.boldLabel);
            _sourceScroll = EditorGUILayout.BeginScrollView(_sourceScroll,
                GUILayout.Height(height));
            _source = EditorGUILayout.TextArea(_source, _codeStyle,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ── 输出面板 ──────────────────────────────────────────────────────────

        private void DrawOutput(float height)
        {
            EditorGUILayout.LabelField("输出", EditorStyles.boldLabel);
            _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll,
                GUILayout.Height(height));
            bool hasError = _outputLog.StartsWith("❌");
            var style = hasError ? _errorStyle : _outputStyle;
            EditorGUILayout.TextArea(_outputLog, style,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ── 历史记录面板 ──────────────────────────────────────────────────────

        private void DrawHistory()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"历史记录（{_history.Count} 条）", EditorStyles.boldLabel);
            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll,
                GUILayout.Height(120));
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                string preview = _history[i].Length > 60
                    ? _history[i].Substring(0, 60).Replace('\n', ' ') + "…"
                    : _history[i].Replace('\n', ' ');
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i + 1}. {preview}",
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("载入", GUILayout.Width(46)))
                        _source = _history[i];
                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        _history.RemoveAt(i);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 统计面板 ──────────────────────────────────────────────────────────

        private void DrawStats()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("运行统计", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_runner.GetStats(), MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清除缓存")) _runner.Cache.Clear();
                if (GUILayout.Button("刷新引用缓存")) RoslynCompiler.InvalidateReferenceCache();
            }
        }

        // ── 键盘快捷键 ────────────────────────────────────────────────────────

        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown &&
                e.keyCode == KeyCode.Return &&
                e.control && !_isCompiling)
            {
                CompileAndRun();
                e.Use();
            }
        }

        // ── 编译 / 运行 ───────────────────────────────────────────────────────

        private void CompileOnly()
        {
            if (string.IsNullOrWhiteSpace(_source))
            {
                _outputLog = "❌ 源码为空。";
                return;
            }

            _isCompiling = true;
            Repaint();

            try
            {
                var result = _runner.Compile(_source);
                _outputLog = result.FormatDiagnostics();
            }
            catch (Exception e)
            {
                _outputLog = $"❌ 编译器内部异常:\n{e}";
            }
            finally
            {
                _isCompiling = false;
                Repaint();
            }
        }

        private void CompileAndRun()
        {
            if (string.IsNullOrWhiteSpace(_source))
            {
                _outputLog = "❌ 源码为空。";
                return;
            }

            if (!Application.isPlaying)
            {
                _outputLog = "⚠ 需要在运行模式下执行脚本（Enter Play Mode）。";
                return;
            }

            _isCompiling = true;
            Repaint();

            var sb = new StringBuilder();
            var start = DateTime.Now;

            _runner.CompileAndRunAsync(_source, _context, success =>
            {
                double ms = (DateTime.Now - start).TotalMilliseconds;
                if (success)
                {
                    sb.AppendLine($"✅ 执行成功（{ms:F1} ms）");
                    var cached = _runner.Compile(_source);
                    if (cached.Diagnostics.Count > 0)
                        sb.AppendLine(cached.FormatDiagnostics());
                    AddHistory(_source);
                }
                else
                {
                    sb.AppendLine($"❌ 执行失败（{ms:F1} ms）");
                    var cached = _runner.Compile(_source);
                    sb.AppendLine(cached.FormatDiagnostics());
                }
                _outputLog   = sb.ToString();
                _isCompiling = false;
                Repaint();
            });
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private void AddHistory(string source)
        {
            // 重复去掉
            _history.Remove(source);
            _history.Add(source);
            if (_history.Count > 20)
                _history.RemoveAt(0);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                font      = Resources.Load<Font>("Fonts/CourierNew") ??
                            EditorStyles.textArea.font,
                fontSize  = 13,
                wordWrap  = false,
            };

            _outputStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 12,
                wordWrap = true,
            };

            _errorStyle = new GUIStyle(_outputStyle);
            _errorStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            _successStyle = new GUIStyle(_outputStyle);
            _successStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
        }
    }
}
#endif
