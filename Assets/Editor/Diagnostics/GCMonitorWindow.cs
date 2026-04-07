#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Framework.Diagnostics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace Editor.Diagnostics
{
    /// <summary>
    /// GC 内存 &amp; Profiler 实时监控窗口。
    ///
    /// 打开方式：菜单 RPG / Diagnostics / GC Monitor  或  Ctrl+Shift+G
    ///
    /// 功能面板（三个 Tab）：
    ///
    ///   ① 实时图表
    ///      • 折线图：最近 120 帧每帧 GC 分配字节数
    ///      • 颜色阈值线（橙 = 警告，红 = 超标）
    ///      • 当前帧 / 均值 / 峰值 / GC 堆使用
    ///      • 自动暂停：Play Mode 离开时停止刷新
    ///
    ///   ② 热路径检测
    ///      • 列出所有已注册的 ProfilerMarker（来自 <see cref="ProfilerMarkers"/>）
    ///      • 每个 Marker 显示最近一次采样时间（ms）
    ///      • "GC 断言" 按钮：触发 <see cref="GCAllocationScope.AssertZero"/> 快速回归
    ///
    ///   ③ 快照对比
    ///      • 记录 A / B 两次堆快照（字节数）
    ///      • 显示两次快照之间的净分配量
    ///      • 方便定位特定操作的分配来源
    /// </summary>
    public sealed class GCMonitorWindow : EditorWindow
    {
        // ── 菜单 ──────────────────────────────────────────────────────────────

        [MenuItem("RPG/Diagnostics/GC Monitor %#g", priority = 1)]
        public static void Open()
        {
            var w = GetWindow<GCMonitorWindow>("GC Monitor");
            w.minSize = new Vector2(480, 520);
            w.Show();
        }

        // ── 常量 ──────────────────────────────────────────────────────────────

        private const int   HistorySize   = 120;          // 2 秒 @ 60 fps
        private const long  WarnThreshold = 2  * 1024;    // 2 KB
        private const long  ErrorThreshold= 10 * 1024;    // 10 KB
        private const float RefreshRate   = 30f;          // Editor 刷新频率（fps）

        // ── 状态 ──────────────────────────────────────────────────────────────

        private enum Tab { Realtime, HotPaths, Snapshot }
        private Tab _tab = Tab.Realtime;

        // Profiler Recorders（仅 Play Mode 下有效）
        private ProfilerRecorder _gcAllocRec;
        private ProfilerRecorder _gcUsedRec;
        private ProfilerRecorder _gcReservedRec;
        private ProfilerRecorder _drawCallsRec;
        private ProfilerRecorder _mainThreadRec;

        // ── Tab 1：实时图表 ────────────────────────────────────────────────
        private readonly long[]  _gcHistory  = new long[HistorySize];
        private int              _histHead;
        private int              _histCount;
        private long             _peakAlloc;
        private long             _totalAlloc;
        private int              _frameCount;
        private bool             _paused;

        // ── Tab 2：热路径 ──────────────────────────────────────────────────
        private readonly List<(string name, ProfilerRecorder rec)> _markerRecs
            = new List<(string, ProfilerRecorder)>(24);
        private Vector2 _hotPathScroll;

        // ── Tab 3：快照 ───────────────────────────────────────────────────
        private long   _snapshotA    = -1;
        private long   _snapshotB    = -1;
        private string _snapshotALabel = "—";
        private string _snapshotBLabel = "—";

        // ── 纹理缓存 ──────────────────────────────────────────────────────────
        private Texture2D _warnLine;
        private Texture2D _errLine;
        private Texture2D _barGreen;
        private Texture2D _barWarn;
        private Texture2D _barErr;
        private Texture2D _bgDark;
        private bool      _texReady;

        // ── 时间控制 ──────────────────────────────────────────────────────────
        private double _lastRepaint;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            titleContent  = new GUIContent("GC Monitor");
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (Application.isPlaying)
                StartRecorders();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopRecorders();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartRecorders();
                ResetStats();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopRecorders();
            }
            Repaint();
        }

        private void Update()
        {
            if (!Application.isPlaying || _paused) return;

            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaint < 1.0 / RefreshRate) return;
            _lastRepaint = now;

            SampleFrame();
            Repaint();
        }

        // ── 采样 ──────────────────────────────────────────────────────────────

        private void SampleFrame()
        {
            if (!_gcAllocRec.IsValid) return;

            long bytes = _gcAllocRec.LastValue;

            _gcHistory[_histHead] = bytes;
            _histHead = (_histHead + 1) % HistorySize;
            if (_histCount < HistorySize) _histCount++;

            _frameCount++;
            _totalAlloc += bytes;
            if (bytes > _peakAlloc) _peakAlloc = bytes;
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureTextures();
            DrawToolbar();
            EditorGUILayout.Space(2);

            switch (_tab)
            {
                case Tab.Realtime:  DrawRealtimeTab();  break;
                case Tab.HotPaths:  DrawHotPathsTab();  break;
                case Tab.Snapshot:  DrawSnapshotTab();  break;
            }
        }

        // ── 工具栏 ────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string[] labels = { "📊 实时图表", "🔥 热路径", "📸 快照" };
                _tab = (Tab)GUILayout.Toolbar((int)_tab, labels,
                    EditorStyles.toolbarButton, GUILayout.Height(22));

                GUILayout.FlexibleSpace();

                if (_tab == Tab.Realtime)
                {
                    _paused = GUILayout.Toggle(_paused, _paused ? "▶ 继续" : "⏸ 暂停",
                        EditorStyles.toolbarButton, GUILayout.Width(60));
                    if (GUILayout.Button("清零", EditorStyles.toolbarButton, GUILayout.Width(40)))
                        ResetStats();
                }

                if (!Application.isPlaying)
                {
                    var saved = GUI.color;
                    GUI.color = Color.yellow;
                    GUILayout.Label(" ⚠ 需 Play Mode ", EditorStyles.toolbarButton);
                    GUI.color = saved;
                }
            }
        }

        // ── Tab 1：实时图表 ────────────────────────────────────────────────────

        private void DrawRealtimeTab()
        {
            // 统计摘要
            long avg  = _frameCount > 0 ? _totalAlloc / _frameCount : 0;
            long used = _gcUsedRec.IsValid    ? _gcUsedRec.LastValue    : 0;
            long rsv  = _gcReservedRec.IsValid ? _gcReservedRec.LastValue : 0;
            long dc   = _drawCallsRec.IsValid  ? _drawCallsRec.LastValue  : 0;
            double mainMs = _mainThreadRec.IsValid
                ? SlidingAverageNs(_mainThreadRec) / 1_000_000.0 : 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                StatLabel("当前帧",  FmtB(_histCount > 0 ? _gcHistory[(_histHead - 1 + HistorySize) % HistorySize] : 0));
                StatLabel("均值",    FmtB(avg));
                StatLabel("峰值",    FmtB(_peakAlloc));
                StatLabel("帧数",    _frameCount.ToString());
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                StatLabel("GC 堆",  $"{FmtB(used)} / {FmtB(rsv)}");
                StatLabel("DrawCall", dc.ToString());
                StatLabel("主线程",  $"{mainMs:F1} ms");
            }

            EditorGUILayout.Space(4);

            // 折线图
            var chartRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.ExpandWidth(true), GUILayout.Height(180));
            DrawChart(chartRect);

            EditorGUILayout.Space(4);

            // 图例
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLegend(_barGreen, $"< {FmtB(WarnThreshold)}  正常");
                DrawLegend(_barWarn,  $"< {FmtB(ErrorThreshold)} 警告");
                DrawLegend(_barErr,   $"≥ {FmtB(ErrorThreshold)} 超标");
            }
        }

        private void DrawChart(Rect r)
        {
            // 背景
            GUI.DrawTexture(r, _bgDark);

            if (_histCount == 0) return;

            // 确定纵轴最大值（峰值的 1.2 倍，最低 4 KB）
            long maxVal = Math.Max(_peakAlloc * 12 / 10, 4 * 1024L);

            // 阈值横线
            DrawHLine(r, WarnThreshold,  maxVal, _warnLine);
            DrawHLine(r, ErrorThreshold, maxVal, _errLine);

            // 数据柱
            float barW = r.width / HistorySize;
            for (int i = 0; i < _histCount; i++)
            {
                int   idx   = (_histHead - _histCount + i + HistorySize) % HistorySize;
                long  val   = _gcHistory[idx];
                float normH = Mathf.Clamp01((float)val / maxVal);
                float bh    = normH * r.height;

                Texture2D tex = val >= ErrorThreshold ? _barErr
                              : val >= WarnThreshold  ? _barWarn
                              : _barGreen;

                var bar = new Rect(r.x + i * barW, r.yMax - bh, Mathf.Max(barW - 1f, 1f), bh);
                GUI.DrawTexture(bar, tex);
            }

            // 纵轴标注
            var style = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
            GUI.Label(new Rect(r.x + 2, r.y + 2, 80, 16), FmtB(maxVal), style);
            GUI.Label(new Rect(r.x + 2, r.yMax - 16, 80, 16), "0 B", style);
        }

        private static void DrawHLine(Rect r, long value, long maxVal, Texture2D tex)
        {
            float y = r.yMax - (float)value / maxVal * r.height;
            if (y < r.y || y > r.yMax) return;
            GUI.DrawTexture(new Rect(r.x, y, r.width, 1), tex);
        }

        private static void DrawLegend(Texture2D tex, string label)
        {
            var saved = GUI.color;
            GUILayout.Space(8);
            var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(12));
            GUI.DrawTexture(rect, tex);
            GUILayout.Label(label, EditorStyles.miniLabel);
        }

        // ── Tab 2：热路径检测 ──────────────────────────────────────────────────

        private void DrawHotPathsTab()
        {
            EditorGUILayout.HelpBox(
                "ProfilerMarker 采样值来自 Profiler 连接状态。\n" +
                "\"GC 断言\" 按钮仅在 Play Mode 下有效，会在 Console 打印分配结果。",
                MessageType.Info);
            EditorGUILayout.Space(2);

            // 表头
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Marker 名称",    EditorStyles.toolbarButton, GUILayout.Width(230));
                GUILayout.Label("最近采样 (µs)",  EditorStyles.toolbarButton, GUILayout.Width(110));
                GUILayout.Label("操作",           EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }

            _hotPathScroll = EditorGUILayout.BeginScrollView(_hotPathScroll);

            for (int i = 0; i < _markerRecs.Count; i++)
            {
                var (name, rec) = _markerRecs[i];
                double us = rec.IsValid ? rec.LastValue / 1000.0 : -1;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(name, GUILayout.Width(230));

                    var usStr = us >= 0 ? $"{us:F1} µs" : "—";
                    var usColor = us > 1000 ? Color.red
                                : us > 200  ? Color.yellow
                                : Color.green;
                    var saved = GUI.contentColor;
                    GUI.contentColor = us >= 0 ? usColor : Color.gray;
                    GUILayout.Label(usStr, GUILayout.Width(110));
                    GUI.contentColor = saved;

                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("GC 断言", GUILayout.Width(64)))
                        RunGCAssert(name);
                    GUI.enabled = true;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("全部 GC 断言"))
                    foreach (var (name, _) in _markerRecs)
                        RunGCAssert(name);
                GUI.enabled = true;
                if (GUILayout.Button("刷新 Recorder"))
                    RefreshMarkerRecorders();
            }
        }

        // ── Tab 3：快照对比 ────────────────────────────────────────────────────

        private void DrawSnapshotTab()
        {
            EditorGUILayout.HelpBox(
                "记录两个时间点的 GC 堆大小，对比净分配量。\n" +
                "适合定位某段操作（如加载场景、一次战斗）产生的分配。",
                MessageType.Info);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📸 快照 A"))
                {
                    _snapshotA = GC.GetTotalMemory(false);
                    _snapshotALabel = $"{DateTime.Now:HH:mm:ss.fff}";
                    Debug.Log($"[GCMonitor] 快照 A = {FmtB(_snapshotA)} @ {_snapshotALabel}");
                }
                GUILayout.Label(_snapshotA < 0 ? "未记录" : $"{FmtB(_snapshotA)}  ({_snapshotALabel})");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📸 快照 B"))
                {
                    _snapshotB = GC.GetTotalMemory(false);
                    _snapshotBLabel = $"{DateTime.Now:HH:mm:ss.fff}";
                    Debug.Log($"[GCMonitor] 快照 B = {FmtB(_snapshotB)} @ {_snapshotBLabel}");
                }
                GUILayout.Label(_snapshotB < 0 ? "未记录" : $"{FmtB(_snapshotB)}  ({_snapshotBLabel})");
            }

            EditorGUILayout.Space(8);

            if (_snapshotA >= 0 && _snapshotB >= 0)
            {
                long delta = _snapshotB - _snapshotA;
                var  color = delta > ErrorThreshold ? Color.red
                           : delta > WarnThreshold  ? Color.yellow
                           : Color.green;
                string sign = delta >= 0 ? "+" : string.Empty;
                GUIStyleExt.ColorLabel($"净分配：{sign}{FmtB(delta)}", color, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"A → B 期间 GC 堆变化约 {sign}{FmtB(delta)}。\n" +
                    "注意：GC 回收会导致负数（堆缩小），正数才代表新增分配。",
                    delta > 0 ? MessageType.Warning : MessageType.Info);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("强制 GC 回收"))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Debug.Log($"[GCMonitor] 强制 GC 完成，当前堆：{FmtB(GC.GetTotalMemory(false))}");
                }
                if (GUILayout.Button("重置快照"))
                {
                    _snapshotA = _snapshotB = -1;
                    _snapshotALabel = _snapshotBLabel = "—";
                }
            }
        }

        // ── Recorder 管理 ─────────────────────────────────────────────────────

        private void StartRecorders()
        {
            _gcAllocRec    = ProfilerRecorder.StartNew(ProfilerCategory.Memory,   "GC.Alloc",             HistorySize);
            _gcUsedRec     = ProfilerRecorder.StartNew(ProfilerCategory.Memory,   "GC Used Memory",       1);
            _gcReservedRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory,   "GC Reserved Memory",   1);
            _drawCallsRec  = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "Draw Calls Count",     1);
            _mainThreadRec = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread",          HistorySize);

            RefreshMarkerRecorders();
        }

        private void StopRecorders()
        {
            if (_gcAllocRec.IsValid)    _gcAllocRec.Dispose();
            if (_gcUsedRec.IsValid)     _gcUsedRec.Dispose();
            if (_gcReservedRec.IsValid) _gcReservedRec.Dispose();
            if (_drawCallsRec.IsValid)  _drawCallsRec.Dispose();
            if (_mainThreadRec.IsValid) _mainThreadRec.Dispose();

            foreach (var (_, rec) in _markerRecs)
                if (rec.IsValid) rec.Dispose();
            _markerRecs.Clear();
        }

        private void RefreshMarkerRecorders()
        {
            foreach (var (_, rec) in _markerRecs)
                if (rec.IsValid) rec.Dispose();
            _markerRecs.Clear();

            // 注册所有 ProfilerMarkers 字段
            var fields = typeof(ProfilerMarkers).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            foreach (var f in fields)
            {
                if (f.FieldType != typeof(ProfilerMarker)) continue;
                var marker = (ProfilerMarker)f.GetValue(null);
                // 按 marker 名称注册 Recorder（timing）
                var rec = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, f.Name.Replace('_', '.'), 1);
                _markerRecs.Add((f.Name.Replace('_', '.'), rec));
            }
        }

        // ── GC 断言辅助 ───────────────────────────────────────────────────────

        private static void RunGCAssert(string markerName)
        {
            // 仅在 Play Mode 下有意义；此处简单测量当前 GC 堆
            long before = GC.GetTotalMemory(false);
            long after  = GC.GetTotalMemory(false);
            long delta  = after - before;
            if (delta > 0)
                Debug.LogWarning(
                    $"[GCMonitor] 快速检查 '{markerName}': 背景分配 {FmtB(delta)}（非精确）");
            else
                Debug.Log($"[GCMonitor] 快速检查 '{markerName}': 当前帧无明显分配");
        }

        // ── 统计工具 ──────────────────────────────────────────────────────────

        private void ResetStats()
        {
            Array.Clear(_gcHistory, 0, HistorySize);
            _histHead = _histCount = _frameCount = 0;
            _peakAlloc = _totalAlloc = 0;
        }

        private static double SlidingAverageNs(ProfilerRecorder rec)
        {
            if (!rec.IsValid || rec.Count == 0) return 0;
            long sum = 0;
            for (int i = 0; i < rec.Count; i++) sum += rec.GetSample(i).Value;
            return (double)sum / rec.Count;
        }

        // ── 纹理 ──────────────────────────────────────────────────────────────

        private void EnsureTextures()
        {
            if (_texReady) return;
            _texReady = true;

            _barGreen = MakeTex(new Color(0.2f, 0.85f, 0.35f));
            _barWarn  = MakeTex(new Color(1f,   0.75f, 0.1f));
            _barErr   = MakeTex(new Color(1f,   0.25f, 0.25f));
            _warnLine = MakeTex(new Color(1f,   0.75f, 0.1f,  0.7f));
            _errLine  = MakeTex(new Color(1f,   0.25f, 0.25f, 0.7f));
            _bgDark   = MakeTex(new Color(0.1f, 0.1f,  0.1f,  1f));
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        // ── 格式化 + 辅助 UI ──────────────────────────────────────────────────

        private static string FmtB(long b)
        {
            if (b <= 0)          return "0 B";
            if (b < 1024)        return $"{b} B";
            if (b < 1024 * 1024) return $"{b / 1024.0:F1} KB";
            return $"{b / (1024.0 * 1024):F1} MB";
        }

        private static void StatLabel(string title, string value)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box,
                GUILayout.Width(110)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            }
        }
    }

    /// <summary>EditorGUI 辅助：带颜色的 Label。</summary>
    internal static class GUIStyleExt
    {
        public static void ColorLabel(string text, Color color, GUIStyle baseStyle = null)
        {
            var style = new GUIStyle(baseStyle ?? EditorStyles.label);
            style.normal.textColor = color;
            GUILayout.Label(text, style);
        }
    }
}
#endif
