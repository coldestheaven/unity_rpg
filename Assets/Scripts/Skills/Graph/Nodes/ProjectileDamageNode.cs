using UnityEngine;
using Gameplay.Combat;
using RPG.Skills.Projectiles;

namespace RPG.Skills.Graph.Nodes
{
    // ── 发射方向模式 ───────────────────────────────────────────────────────────

    public enum ProjectileDirectionMode
    {
        Facing,         // 施法者当前朝向（SpriteRenderer.flipX / transform.right）
        NearestEnemy,   // 自动瞄准最近的敌人
        MousePosition,  // 朝屏幕鼠标位置（需要主摄像机）
    }

    /// <summary>
    /// 投掷物伤害节点 — 在施法者位置生成一枚或多枚飞行投掷物。
    ///
    /// 功能亮点:
    ///   • 单发 / 多发扇形散弹（count + spreadAngle）
    ///   • 直线 / 追踪（homingStrength > 0）
    ///   • 穿透（piercing = true）
    ///   • 三种初始方向模式（朝向 / 最近敌人 / 鼠标）
    ///   • 支持发射偏移（发射点相对施法者的偏移）
    ///
    /// 节点执行后立即沿 "launched" 端口继续，投掷物在后台异步飞行。
    ///
    /// 预制体要求：Collider2D (IsTrigger=true) + Rigidbody2D (Kinematic)。
    /// 如果预制体上已有 <see cref="SkillProjectile"/>，节点将覆盖其参数；
    /// 否则节点会自动 AddComponent 添加它。
    /// </summary>
    [SkillNodeType("投掷物伤害", "战斗", "#E86020",
        tooltip: "生成飞行投掷物，命中敌人时造成伤害。支持多发/散弹/追踪/穿透。")]
    [NodeInput("execute",  "执行")]
    [NodeOutput("launched","已发射")]
    public sealed class ProjectileDamageNode : SkillNode
    {
        // ── 基础伤害 ───────────────────────────────────────────────────────────

        [NodeField("投掷物预制体")]
        public GameObject prefab;

        [NodeField("基础伤害", 0f, 10000f)]
        public float baseDamage = 20f;

        [NodeField("伤害类型")]
        public RPG.Skills.DamageType damageType = RPG.Skills.DamageType.Physical;

        [NodeField("伤害倍率")]
        public float damageMultiplier = 1f;

        // ── 飞行参数 ───────────────────────────────────────────────────────────

        [NodeField("飞行速度", 1f, 100f)]
        public float speed = 10f;

        [NodeField("最大射程", 0.5f, 100f)]
        public float maxRange = 15f;

        // ── 多发 / 散弹 ────────────────────────────────────────────────────────

        [NodeField("发射数量", 1f, 20f)]
        public int count = 1;

        /// <summary>多发时的总展开角度（度）。count=3、spread=60° → 各相差 30°。</summary>
        [NodeField("散射张角 (°)", 0f, 180f)]
        public float spreadAngle = 30f;

        // ── 方向 & 追踪 ────────────────────────────────────────────────────────

        [NodeField("方向模式")]
        public ProjectileDirectionMode directionMode = ProjectileDirectionMode.Facing;

        /// <summary>追踪强度 0 = 直线；1 = 强追踪；适合导弹/精灵弹。</summary>
        [NodeField("追踪强度", 0f, 5f)]
        public float homingStrength = 0f;

        // ── 穿透 ──────────────────────────────────────────────────────────────

        [NodeField("穿透")]
        public bool piercing = false;

        [NodeField("最大穿透数", 1f, 20f)]
        public int maxPierces = 3;

        // ── 发射点偏移 ─────────────────────────────────────────────────────────

        [NodeField("发射偏移 X")]
        public float spawnOffsetX = 0f;

        [NodeField("发射偏移 Y")]
        public float spawnOffsetY = 0.5f;

        // ── Ports ──────────────────────────────────────────────────────────────

        public override NodePortDefinition[] GetInputPorts() => new[]
        {
            new NodePortDefinition("execute", "执行")
        };

        public override NodePortDefinition[] GetOutputPorts() => new[]
        {
            new NodePortDefinition("launched", "已发射")
        };

        // ── Execute ────────────────────────────────────────────────────────────

        public override void Execute(SkillNodeContext ctx)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ProjectileDamageNode] 未设置投掷物预制体，跳过发射。");
                ctx.ExecuteOutputPort("launched");
                return;
            }

            var info    = DamageShapeHelper.BuildInfo(ctx, baseDamage, damageMultiplier, damageType);
            var hitMask = DamageShapeHelper.EnemyMask;

            Vector2 baseDir = GetBaseDirection(ctx);
            Vector3 origin  = ctx.CasterPosition +
                              new Vector3(spawnOffsetX, spawnOffsetY, 0f);

            // ── 多发散射 ────────────────────────────────────────────────────
            float startAngle = count > 1 ? -spreadAngle * 0.5f : 0f;
            float stepAngle  = count > 1 ? spreadAngle / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float   angle = (count > 1 ? startAngle + stepAngle * i : 0f) * Mathf.Deg2Rad;
                Vector2 dir   = RotateVector(baseDir, angle);

                SpawnProjectile(origin, dir, info, hitMask, ctx);
            }

            ctx.ExecuteOutputPort("launched");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SpawnProjectile(
            Vector3 origin, Vector2 direction,
            DamageInfo info, LayerMask hitMask,
            SkillNodeContext ctx)
        {
            var go = Object.Instantiate(prefab, origin, Quaternion.identity);

            // Get or add SkillProjectile component
            var proj = go.GetComponent<SkillProjectile>();
            if (proj == null) proj = go.AddComponent<SkillProjectile>();

            proj.Payload       = info;
            proj.Direction     = direction.normalized;
            proj.Speed         = speed;
            proj.MaxRange      = maxRange;
            proj.Piercing      = piercing;
            proj.MaxPierces    = maxPierces;
            proj.HomingStrength = homingStrength;
            proj.HitMask       = hitMask;

            // Store hit count in context variable for potential downstream use
            int hitsBefore = ctx.GetVar<int>("projectile_hits", 0);
            proj.OnHit = _ => ctx.SetVar("projectile_hits", hitsBefore + 1);
        }

        private Vector2 GetBaseDirection(SkillNodeContext ctx)
        {
            switch (directionMode)
            {
                case ProjectileDirectionMode.Facing:
                    return DamageShapeHelper.GetFacing(ctx.Caster);

                case ProjectileDirectionMode.NearestEnemy:
                    return GetDirectionToNearest(ctx.CasterPosition);

                case ProjectileDirectionMode.MousePosition:
                    return GetDirectionToMouse(ctx.CasterPosition);

                default:
                    return Vector2.right;
            }
        }

        private static Vector2 GetDirectionToNearest(Vector3 from)
        {
            var hits = Physics2D.OverlapCircleAll(from, 50f, DamageShapeHelper.EnemyMask);
            Vector2 nearest = Vector2.right;
            float minDist = float.MaxValue;
            foreach (var h in hits)
            {
                float d = Vector2.Distance(from, h.transform.position);
                if (d < minDist) { minDist = d; nearest = ((Vector2)h.transform.position - (Vector2)from).normalized; }
            }
            return nearest;
        }

        private static Vector2 GetDirectionToMouse(Vector3 from)
        {
            if (Camera.main == null) return Vector2.right;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = from.z;
            Vector2 dir = ((Vector2)mouseWorld - (Vector2)from);
            return dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
        }

        private static Vector2 RotateVector(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
