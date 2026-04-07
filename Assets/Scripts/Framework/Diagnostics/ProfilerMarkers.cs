// ProfilerMarkers.cs — 集中注册所有热路径的 ProfilerMarker
//
// 使用方式（零开销，安全保留于 Release 版本）：
//
//   using (ProfilerMarkers.EventBus_Publish.Auto())   // 自动 Begin / End
//       EventBus.Publish(evt);
//
//   ProfilerMarkers.Combat_ApplyDamage.Begin();
//   ApplyDamage();
//   ProfilerMarkers.Combat_ApplyDamage.End();
//
// ProfilerMarker 未连接 Profiler 时开销 ≈ 0（两次条件判断）。
// 连接后，采样数据会出现在 Unity Profiler 的时间轴视图中。
using Unity.Profiling;

namespace Framework.Diagnostics
{
    /// <summary>
    /// 所有游戏热路径的 <see cref="ProfilerMarker"/> 集中注册。
    /// 使用 <c>using (ProfilerMarkers.Xxx.Auto())</c> 标注代码段，
    /// 或调用 <c>.Begin() / .End()</c> 手动标注跨方法的范围。
    /// </summary>
    public static class ProfilerMarkers
    {
        // ── EventBus ──────────────────────────────────────────────────────────
        public static readonly ProfilerMarker EventBus_Publish =
            new ProfilerMarker(ProfilerCategory.Scripts, "EventBus.Publish");

        public static readonly ProfilerMarker EventBus_Subscribe =
            new ProfilerMarker(ProfilerCategory.Scripts, "EventBus.Subscribe");

        // ── Presentation Command Queue ────────────────────────────────────────
        /// <summary>PresentationCommandQueue.Enqueue（逻辑线程调用）。</summary>
        public static readonly ProfilerMarker PCQ_Enqueue =
            new ProfilerMarker(ProfilerCategory.Scripts, "PCQ.Enqueue");

        /// <summary>PresentationDispatcher 每帧 TryDequeue + 分发全部命令。</summary>
        public static readonly ProfilerMarker PCQ_Flush =
            new ProfilerMarker(ProfilerCategory.Scripts, "PCQ.Flush");

        // ── Logic Thread ──────────────────────────────────────────────────────
        /// <summary>逻辑线程每个 Work Item 的执行。</summary>
        public static readonly ProfilerMarker LogicThread_WorkItem =
            new ProfilerMarker(ProfilerCategory.Scripts, "LogicThread.WorkItem");

        // ── Simulation ────────────────────────────────────────────────────────
        public static readonly ProfilerMarker Sim_HealthTick =
            new ProfilerMarker(ProfilerCategory.Scripts, "Sim.HealthTick");

        public static readonly ProfilerMarker Sim_DoTTick =
            new ProfilerMarker(ProfilerCategory.Scripts, "Sim.DoTTick");

        public static readonly ProfilerMarker Sim_ProgressTick =
            new ProfilerMarker(ProfilerCategory.Scripts, "Sim.ProgressTick");

        public static readonly ProfilerMarker Sim_CombatSnapshot =
            new ProfilerMarker(ProfilerCategory.Scripts, "Sim.CombatSnapshot");

        // ── Combat ────────────────────────────────────────────────────────────
        public static readonly ProfilerMarker Combat_ApplyDamage =
            new ProfilerMarker(ProfilerCategory.Scripts, "Combat.ApplyDamage");

        public static readonly ProfilerMarker Combat_PhysicsQuery =
            new ProfilerMarker(ProfilerCategory.Scripts, "Combat.PhysicsQuery");

        public static readonly ProfilerMarker Combat_DamageHandler =
            new ProfilerMarker(ProfilerCategory.Scripts, "Combat.DamageHandler");

        // ── Skills ────────────────────────────────────────────────────────────
        public static readonly ProfilerMarker Skills_TryUse =
            new ProfilerMarker(ProfilerCategory.Scripts, "Skills.TryUse");

        public static readonly ProfilerMarker Skills_FindTargets =
            new ProfilerMarker(ProfilerCategory.Scripts, "Skills.FindTargets");

        public static readonly ProfilerMarker Skills_EffectApply =
            new ProfilerMarker(ProfilerCategory.Scripts, "Skills.EffectApply");

        // ── Enemy AI ──────────────────────────────────────────────────────────
        public static readonly ProfilerMarker Enemy_DetectPlayer =
            new ProfilerMarker(ProfilerCategory.Scripts, "Enemy.DetectPlayer");

        public static readonly ProfilerMarker Enemy_AttackHitScan =
            new ProfilerMarker(ProfilerCategory.Scripts, "Enemy.AttackHitScan");

        // ── Asset Loading ─────────────────────────────────────────────────────
        public static readonly ProfilerMarker Asset_Load =
            new ProfilerMarker(ProfilerCategory.Loading, "AssetService.Load");

        public static readonly ProfilerMarker Asset_LoadAsync =
            new ProfilerMarker(ProfilerCategory.Loading, "AssetService.LoadAsync");

        // ── Object Pools ──────────────────────────────────────────────────────
        public static readonly ProfilerMarker Pool_GOGet =
            new ProfilerMarker(ProfilerCategory.Scripts, "Pool.GameObject.Get");

        public static readonly ProfilerMarker Pool_GORelease =
            new ProfilerMarker(ProfilerCategory.Scripts, "Pool.GameObject.Release");

        public static readonly ProfilerMarker Pool_ListGet =
            new ProfilerMarker(ProfilerCategory.Scripts, "Pool.List.Get");

        public static readonly ProfilerMarker Pool_ListRelease =
            new ProfilerMarker(ProfilerCategory.Scripts, "Pool.List.Release");

        // ── Save / Load ───────────────────────────────────────────────────────
        public static readonly ProfilerMarker Save_Write =
            new ProfilerMarker(ProfilerCategory.Scripts, "SaveSystem.Write");

        public static readonly ProfilerMarker Save_Read =
            new ProfilerMarker(ProfilerCategory.Scripts, "SaveSystem.Read");

        // ── UI ────────────────────────────────────────────────────────────────
        public static readonly ProfilerMarker UI_HudUpdate =
            new ProfilerMarker(ProfilerCategory.Scripts, "UI.HudUpdate");

        public static readonly ProfilerMarker UI_VMUpdate =
            new ProfilerMarker(ProfilerCategory.Scripts, "UI.ViewModelUpdate");
    }
}
