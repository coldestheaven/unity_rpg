using UnityEngine;
using Gameplay.Combat;

namespace RPG.Skills.Graph.Nodes
{
    /// <summary>
    /// 全屏伤害节点 — 对场景中所有存活的 <see cref="Gameplay.Combat.DamageableBase"/> 造成伤害，
    /// 可选排除施法者自身及其友方（通过 Tag 过滤）。
    ///
    /// 典型用途：核爆、天降陨石、全图诅咒、剧情一刀清场。
    ///
    /// ⚠ 警告: 此节点不受距离限制，伤害所有符合条件的实体，谨慎用于非战斗场景。
    /// </summary>
    [SkillNodeType("全屏伤害", "战斗/形状", "#CC2020",
        tooltip: "伤害场景中所有存活的敌人（或指定 Tag 的对象）。不受距离限制。")]
    public sealed class FullScreenDamageNode : SkillNode
    {
        [NodeField("基础伤害", 0f, 100000f)]
        public float baseDamage = 50f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Magic;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        /// <summary>
        /// 目标 Tag（留空则使用 Enemy 物理层；设置后按 Tag 查找，可突破层级限制）。
        /// 多个 Tag 用英文逗号分隔，如 "Enemy,Boss"。
        /// </summary>
        [NodeField("目标 Tag (空=Enemy层)")]
        public string targetTags = "";

        /// <summary>排除施法者所在的 GameObject。</summary>
        [NodeField("排除施法者")]
        public bool excludeCaster = true;

        /// <summary>
        /// 从边缘距离施法者 <see cref="excludeRadius"/> 以内的目标不受伤害（安全区）。
        /// 0 = 不设安全区。
        /// </summary>
        [NodeField("安全半径 (0=无)", 0f, 30f)]
        public float excludeRadius = 0f;

        // ── Ports ──────────────────────────────────────────────────────────────

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("done",    "完成"),
            new NodePortDefinition("onKill",  "击杀时"),
            new NodePortDefinition("allDead", "全部消灭")   // fires only if every hit target died
        };

        // ── Execute ────────────────────────────────────────────────────────────

        public override void Execute(SkillNodeContext ctx)
        {
            var info        = DamageShapeHelper.BuildInfo(ctx, baseDamage, damageMultiplier, damageType);
            Vector2 casterPos = ctx.CasterPosition;

            // Collect targets
            DamageableBase[] allEntities;

#if UNITY_2023_1_OR_NEWER
            allEntities = Object.FindObjectsByType<DamageableBase>(FindObjectsSortMode.None);
#else
            allEntities = Object.FindObjectsOfType<DamageableBase>();
#endif

            bool killedAny  = false;
            int  hitCount   = 0;
            int  killedCount = 0;

            foreach (var entity in allEntities)
            {
                if (entity == null || entity.IsDead) continue;

                // Exclude caster
                if (excludeCaster && ctx.CasterObject != null &&
                    entity.gameObject == ctx.CasterObject)
                    continue;

                // Tag filter (if specified)
                if (!string.IsNullOrEmpty(targetTags) &&
                    !MatchesAnyTag(entity.gameObject, targetTags))
                    continue;

                // Safe-radius exclusion
                if (excludeRadius > 0f &&
                    Vector2.Distance(casterPos, entity.transform.position) < excludeRadius)
                    continue;

                bool wasAlive = !entity.IsDead;
                CombatResolver.TryApplyDamage(entity.gameObject, info);
                hitCount++;

                if (wasAlive && entity.IsDead)
                {
                    killedAny = true;
                    killedCount++;
                }
            }

            bool allKilled = hitCount > 0 && killedCount == hitCount;

            if (allKilled)   ctx.ExecuteOutputPort("allDead");
            if (killedAny)   ctx.ExecuteOutputPort("onKill");
            ctx.ExecuteOutputPort("done");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static bool MatchesAnyTag(GameObject go, string tagsCsv)
        {
            string[] tags = tagsCsv.Split(',');
            foreach (var t in tags)
            {
                string trimmed = t.Trim();
                if (!string.IsNullOrEmpty(trimmed) && go.CompareTag(trimmed))
                    return true;
            }
            return false;
        }
    }
}
