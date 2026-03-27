# 战斗系统文档

## 统一伤害管线

所有伤害来源都通过同一个入口 `CombatResolver.TryApplyDamage`，不允许直接调用 `TakeDamage`。

```
攻击来源                  构造 DamageInfo              执行
──────────────────────────────────────────────────────────
PlayerCombat.PerformAttack()   → new DamageInfo(...)  → CombatResolver.TryApplyDamage()
EnemyAttack.PerformAttack()    → new DamageInfo(...)  → CombatResolver.TryApplyDamage()
Hitbox.OnTriggerEnter2D()      → new DamageInfo(...)  → CombatResolver.TryApplyDamage()
SkillController.ApplySkill()   → new DamageInfo(...)  → CombatResolver.TryApplyDamage()
SkillEffect.DealDamage()       → BuildDamageInfo()    → CombatResolver.TryApplyDamage()
```

## DamageInfo 结构

```csharp
// Gameplay.Combat 命名空间
public readonly struct DamageInfo
{
    public float Amount { get; }
    public Vector2 SourcePosition { get; }
    public GameObject SourceObject { get; }
    public CombatDamageType DamageType { get; }
    public CombatHitKind HitKind { get; }
    public bool IsPeriodic { get; }
}

public enum CombatDamageType { Physical, Magic, Fire, Ice, Lightning, Poison, True }
public enum CombatHitKind   { Attack, Skill, Hitbox, DamageOverTime }
```

## CombatResolver

```csharp
// 优先使用 IDamageReceiver（携带完整上下文），回退 IDamageable（兼容旧代码）
public static class CombatResolver
{
    public static bool TryApplyDamage(Collider2D col, DamageInfo info);
    public static bool TryApplyDamage(GameObject go,  DamageInfo info);
    public static bool TryApplyDamage(IDamageable target, DamageInfo info);
}
```

## DamageableBase 生命周期

`DamageableBase` 是 `PlayerHealth` 和 `Health`（通用实体）的公共基类。

```
ReceiveDamage(DamageInfo)
  └─ CanReceiveDamage()    → 如果返回 false 跳过（死亡、无敌帧等）
  └─ ResolveDamage()       → 计算实际扣血量（可被子类 override 处理防御、抗性）
  └─ currentHealth -= resolved
  └─ NotifyHealthChanged() → 触发 OnHealthChanged 事件
  └─ OnDamageTaken()       → 子类特定处理（击退、无敌帧等）
  └─ if dead → OnDeathInternal() → 触发 OnDied 事件
```

### PlayerHealth 覆写

| 虚方法 | PlayerHealth 行为 |
|--------|------------------|
| `CanReceiveDamage()` | 检查无敌帧 `isInvincible` |
| `ResolveDamage()` | 基类（防御扣减） |
| `OnDamageTaken()` | 触发击退 Knockback + 启动无敌帧协程 |
| `NotifyHealthChanged()` | 触发 `OnHealthChanged` 事件（供 HUDController 订阅） |
| `OnDeathInternal()` | 触发 `OnDied`，通知 `GameStateManager` 切换 GameOver |
| `Revive()` | 满血复活（绝对值，不走 base 逻辑） |

### Health 覆写（通用实体）

| 虚方法 | Health 行为 |
|--------|-------------|
| `ResolveDamage()` | 向下取整为 `int` |
| `NotifyHealthChanged()` | 触发 `OnHealthChanged` 事件 |
| `OnDeathInternal()` | 播放死亡特效，可选延迟销毁 GameObject |

## 属性与防御计算

防御减伤在 `DamageableBase.ResolveDamage` 中完成（默认线性减法，子类可 override）：

```csharp
protected virtual float ResolveDamage(DamageInfo info)
    => Mathf.Max(0f, info.Amount - Defense);
```

`Defense` 来自 `DamageableBase.Defense` 属性，由 `PlayerStatsRuntime` 写入（调用 `SetDefense()`）。

## 添加新伤害来源

1. 构造 `DamageInfo`，填写 `Amount`、`SourcePosition`、`DamageType`、`HitKind`
2. 调用 `CombatResolver.TryApplyDamage(target, info)`
3. 不需要关心目标是 `PlayerHealth` 还是 `Health`，Resolver 自动分发

```csharp
// 示例：陷阱伤害
var info = new DamageInfo(
    amount: 20f,
    sourcePosition: transform.position,
    sourceObject: gameObject,
    damageType: CombatDamageType.Physical,
    hitKind: CombatHitKind.Hitbox
);
CombatResolver.TryApplyDamage(playerCollider, info);
```

## 技能伤害接入

技能伤害通过 `SkillEffect.BuildDamageInfo()` 构造，使用 `CombatDamageTypeMapper` 将 `RPG.Skills.DamageType` 映射到 `CombatDamageType`：

```csharp
protected DamageInfo BuildDamageInfo()
{
    return new DamageInfo(
        amount: skillData.GetDamage(skillLevel),
        sourcePosition: transform.position,
        sourceObject: gameObject,
        damageType: CombatDamageTypeMapper.FromSkillDamageType(skillData.damageType),
        hitKind: CombatHitKind.Skill
    );
}
```

## Buff 系统

`PlayerBuffController` 管理限时 Buff，每个 Buff 携带一个 `PlayerStatBlock` delta：

```csharp
// 添加 Buff（由 ConsumableData.Use() 或技能效果调用）
PlayerBuffController.AddBuff(new BuffEntry {
    duration = 10f,
    statDelta = new PlayerStatBlock { AttackDamage = 15, MoveSpeed = 1f }
});
```

Buff 到期后自动移除，触发 `ModifiersChanged` 事件，`PlayerStatsRuntime` 重新聚合属性。
