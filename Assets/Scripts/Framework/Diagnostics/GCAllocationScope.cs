using System;
using UnityEngine;

namespace Framework.Diagnostics
{
    // ────────────────────────────────────────────────────────────────────────────
    //  GCAllocationScope — 范围内 GC 分配检测
    //
    //  仅在  DEVELOPMENT_BUILD  或  UNITY_EDITOR  下有效；
    //  Release 版本（无以上宏）下，所有操作均为空操作，零开销。
    //
    //  用法示例：
    //
    //  1. 断言零分配（测试 / 热路径验证）：
    //     using (GCAllocationScope.AssertZero("EventBus.Publish"))
    //         EventBus.Publish(new PlayerDiedEvent());
    //
    //  2. 带预算警告（运行时）：
    //     using (GCAllocationScope.Warn("MyHotPath", warnAboveBytes: 512))
    //         DoSomething();
    //
    //  3. 静态一次性测量（返回分配字节数）：
    //     long bytes = GCAllocationScope.Measure(() => DoSomething());
    //     Debug.Log($"分配了 {bytes} B");
    //
    //  注意：
    //    • 基于 GC.GetTotalMemory(false)，若作用域内发生 GC 回收，值可能为负（被钳制到 0）。
    //    • 适合单线程验证；多线程并发分配会影响测量精度。
    //    • 如需精确测量，使用 ProfilerRecorder("GC.Alloc")（见 GCMonitorWindow）。
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 基于 <c>GC.GetTotalMemory</c> 的轻量范围内 GC 分配检测。
    /// 仅在 Development Build / Editor 下有效，Release 版本为空操作。
    /// </summary>
    public readonly struct GCAllocationScope : IDisposable
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private readonly long   _memBefore;
        private readonly string _label;
        private readonly long   _threshold;
        private readonly bool   _assertZero;
#endif

        // ── 工厂方法 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 断言作用域内零 GC 分配。有分配则 <see cref="Debug.LogError"/>。
        /// <para>适用于单元测试、热路径验证。</para>
        /// </summary>
        public static GCAllocationScope AssertZero(string label)
            => new GCAllocationScope(label, threshold: 0, assertZero: true);

        /// <summary>
        /// 当分配超过 <paramref name="warnAboveBytes"/> 时 <see cref="Debug.LogWarning"/>。
        /// <para><paramref name="warnAboveBytes"/> = 0 表示任何分配都警告。</para>
        /// </summary>
        public static GCAllocationScope Warn(string label, long warnAboveBytes = 0)
            => new GCAllocationScope(label, threshold: warnAboveBytes, assertZero: false);

        /// <summary>
        /// 静态一次性测量：执行 <paramref name="action"/> 并返回 GC 分配字节数。
        /// <para>返回值为负表示作用域内发生了 GC 回收（视为 0）。</para>
        /// </summary>
        public static long Measure(Action action)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (action == null) throw new ArgumentNullException(nameof(action));
            // 强制 GC 让基线更准确（仅在测量工具场景下可接受）
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetTotalMemory(false);
            action();
            long allocated = GC.GetTotalMemory(false) - before;
            return allocated < 0 ? 0 : allocated;
#else
            action?.Invoke();
            return 0;
#endif
        }

        /// <summary>
        /// 测量 <paramref name="func"/> 的分配字节数（有返回值版本）。
        /// </summary>
        public static long Measure<T>(Func<T> func, out T result)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (func == null) throw new ArgumentNullException(nameof(func));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetTotalMemory(false);
            result = func();
            long allocated = GC.GetTotalMemory(false) - before;
            return allocated < 0 ? 0 : allocated;
#else
            result = func != null ? func() : default;
            return 0;
#endif
        }

        // ── 构造 / IDisposable ────────────────────────────────────────────────

        private GCAllocationScope(string label, long threshold, bool assertZero)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _label      = label     ?? "?";
            _threshold  = threshold;
            _assertZero = assertZero;
            _memBefore  = GC.GetTotalMemory(false);
#endif
        }

        /// <summary>
        /// 测量并报告。由 <c>using</c> 语句自动调用。
        /// </summary>
        public void Dispose()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            long allocated = GC.GetTotalMemory(false) - _memBefore;
            if (allocated < 0) allocated = 0; // GC 回收导致负值

            if (_assertZero)
            {
                if (allocated > 0)
                    Debug.LogError(
                        $"[GCAssert] ❌ '{_label}' 产生了 {FormatBytes(allocated)} GC 分配，" +
                        $"期望零分配！\n" +
                        $"常见原因：装箱（enum/struct→object）、委托闭包捕获、" +
                        $"临时 List/Array/string 创建、LINQ、params 数组。");
            }
            else if (allocated > _threshold)
            {
                Debug.LogWarning(
                    $"[GCCheck] ⚠ '{_label}' GC 分配 {FormatBytes(allocated)}" +
                    (_threshold > 0 ? $"（预算 {FormatBytes(_threshold)}）" : string.Empty));
            }
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }
#endif
    }
}
