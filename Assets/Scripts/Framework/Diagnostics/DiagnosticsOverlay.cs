using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Framework.Diagnostics
{
    /// <summary>
    /// 运行时右上角诊断覆盖层。
    ///
    /// 显示内容：
    ///   • FPS / 主线程耗时
    ///   • 每帧 GC 分配（滑动均值，30 帧）
    ///   • GC 堆使用 / 保留量
    ///   • DrawCall 数 / 三角面数
    ///   • PresentationCommandQueue 挂起命令数
    ///
    /// 颜色编码：
    ///   绿色  — GC &lt; 2 KB/帧（正常）
    ///   橙色  — 2–10 KB/帧（警告）
    ///   红色  — &gt; 10 KB/帧（异常）
    ///
    /// 快捷键（可配置）默认 F8 切换显示。
    ///
    /// Release 构建中（<c>DEVELOPMENT_BUILD</c> / <c>UNITY_EDITOR</c> 均未定义时），
    /// 组件在 <c>Awake</c> 内销毁自身，不产生任何运行时开销。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RPG/Diagnostics/Diagnostics Overlay")]
    public sealed class DiagnosticsOverlay : MonoBehaviour
    {
        [Header("显示设置")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F8;
        [SerializeField] private int     _fontSize   = 13;
        [SerializeField] private bool    _startVisible = true;

        [Header("颜色")]
        [SerializeField] private Color _normalColor  = new Color(0.2f, 1f,   0.35f, 0.9f);
        [SerializeField] private Color _warnColor    = new Color(1f,   0.8f, 0.1f,  0.9f);
        [SerializeField] private Color _errorColor   = new Color(1f,   0.3f, 0.3f,  0.9f);

        [Header("GC 预算（字节/帧）")]
        [Tooltip("超过此值变橙色（默认 2 KB）")]
        [SerializeField] private long _warnThreshold  = 2 * 1024;
        [Tooltip("超过此值变红色（默认 10 KB）")]
        [SerializeField] private long _errorThreshold = 10 * 1024;

#if DEVELOPMENT_BUILD || UNITY_EDITOR

        // ── ProfilerRecorder（持久，不每帧 new） ─────────────────────────────
        private ProfilerRecorder _gcAllocRec;        // GC.Alloc 每帧字节数
        private ProfilerRecorder _gcUsedRec;         // GC 堆已用
        private ProfilerRecorder _gcReservedRec;     // GC 堆保留
        private ProfilerRecorder _drawCallsRec;      // DrawCall 数
        private ProfilerRecorder _triRec;            // 三角面数
        private ProfilerRecorder _mainThreadTimeRec; // 主线程时间（ns）

        private const int SampleCount = 30;

        // ── 状态 ─────────────────────────────────────────────────────────────
        private bool   _visible;
        private string _text   = string.Empty;
        private Color  _color;
        private float  _nextUpdate;
        private const float UpdateInterval = 0.1f; // 文字刷新间隔（秒）

        private readonly StringBuilder _sb = new StringBuilder(512);

        // ── GUI 样式（延迟初始化，避免 OnEnable 时 GUISkin 未就绪） ──────────
        private GUIStyle   _labelStyle;
        private GUIStyle   _bgStyle;
        private Texture2D  _bgTex;
        private bool       _stylesReady;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake() => _visible = _startVisible;

        private void OnEnable()
        {
            _gcAllocRec        = ProfilerRecorder.StartNew(ProfilerCategory.Memory,
                                     "GC.Alloc",             SampleCount);
            _gcUsedRec         = ProfilerRecorder.StartNew(ProfilerCategory.Memory,
                                     "GC Used Memory",       1);
            _gcReservedRec     = ProfilerRecorder.StartNew(ProfilerCategory.Memory,
                                     "GC Reserved Memory",   1);
            _drawCallsRec      = ProfilerRecorder.StartNew(ProfilerCategory.Render,
                                     "Draw Calls Count",     1);
            _triRec            = ProfilerRecorder.StartNew(ProfilerCategory.Render,
                                     "Triangles Count",      1);
            _mainThreadTimeRec = ProfilerRecorder.StartNew(ProfilerCategory.Internal,
                                     "Main Thread",          SampleCount);
        }

        private void OnDisable()
        {
            _gcAllocRec.Dispose();
            _gcUsedRec.Dispose();
            _gcReservedRec.Dispose();
            _drawCallsRec.Dispose();
            _triRec.Dispose();
            _mainThreadTimeRec.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _visible = !_visible;

            if (!_visible) return;
            if (Time.unscaledTime < _nextUpdate) return;
            _nextUpdate = Time.unscaledTime + UpdateInterval;

            RefreshText();
        }

        private void RefreshText()
        {
            long   gcAlloc    = SlidingAverage(_gcAllocRec);
            long   gcUsed     = LastValue(_gcUsedRec);
            long   gcReserved = LastValue(_gcReservedRec);
            long   drawCalls  = LastValue(_drawCallsRec);
            long   tris       = LastValue(_triRec);
            double mainMs     = SlidingAverageNs(_mainThreadTimeRec) / 1_000_000.0;
            float  fps        = 1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f);

            _sb.Clear();
            _sb.Append($"FPS {fps:F0}  |  主线程 {mainMs:F1} ms\n");
            _sb.Append($"GC/帧  {FmtBytes(gcAlloc)}\n");
            _sb.Append($"GC 堆  {FmtBytes(gcUsed)} / {FmtBytes(gcReserved)}\n");
            _sb.Append($"Draw {drawCalls}  Tri {tris / 1000}K\n");
            _sb.Append($"PCQ  {Framework.Presentation.PresentationCommandQueue.Count} cmds");

            _text  = _sb.ToString();
            _color = gcAlloc > _errorThreshold ? _errorColor
                   : gcAlloc > _warnThreshold  ? _warnColor
                   : _normalColor;
        }

        private void OnGUI()
        {
            if (!_visible || string.IsNullOrEmpty(_text)) return;

            EnsureStyles();
            _labelStyle.fontSize             = _fontSize;
            _labelStyle.normal.textColor     = _color;

            const float pad   = 6f;
            const float width = 240f;
            float x = Screen.width - width - pad;
            float y = pad;

            var content = new GUIContent(_text);
            float height = _labelStyle.CalcHeight(content, width);

            var bg = new Rect(x - pad, y - pad, width + pad * 2, height + pad * 2);
            GUI.Box(bg, GUIContent.none, _bgStyle);
            GUI.Label(new Rect(x, y, width, height), _text, _labelStyle);
        }

        // ── ProfilerRecorder 辅助 ─────────────────────────────────────────────

        private static long SlidingAverage(ProfilerRecorder rec)
        {
            if (!rec.Valid || rec.Count == 0) return 0;
            long sum = 0;
            int  n   = rec.Count;
            for (int i = 0; i < n; i++) sum += rec.GetSample(i).Value;
            return sum / n;
        }

        private static double SlidingAverageNs(ProfilerRecorder rec)
        {
            if (!rec.Valid || rec.Count == 0) return 0;
            long sum = 0;
            int  n   = rec.Count;
            for (int i = 0; i < n; i++) sum += rec.GetSample(i).Value;
            return (double)sum / n;
        }

        private static long LastValue(ProfilerRecorder rec)
            => rec.Valid ? rec.LastValue : 0;

        // ── GUI 样式 ─────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            _bgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            _bgTex.Apply();

            _bgStyle = new GUIStyle(GUI.skin.box);
            _bgStyle.normal.background = _bgTex;
        }

        // ── 格式化 ────────────────────────────────────────────────────────────

        private static string FmtBytes(long b)
        {
            if (b <= 0)          return "0 B";
            if (b < 1024)        return $"{b} B";
            if (b < 1024 * 1024) return $"{b / 1024.0:F1} KB";
            return $"{b / (1024.0 * 1024):F1} MB";
        }

#else
        // ── Release 版本：即刻销毁，零开销 ──────────────────────────────────
        private void Awake() { Destroy(this); }
#endif
    }
}
