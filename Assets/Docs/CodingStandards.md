# 代码规范

## 命名空间

| 目录 | 命名空间 |
|------|---------|
| `Scripts/Framework/Base/` | `Framework.Base`（含 `MonoBehaviourBase`、`SingletonMonoBehaviour<T>`、`MoveableBase`、`StateMachineBase<T>`） |
| `Scripts/Framework/Core/Events/` | `Framework.Events`（`EventManager`） |
| `Scripts/Framework/Core/Patterns/` | `Framework.Core.Patterns`（`Singleton<T>`、`ObjectPool`） |
| `Scripts/Framework/Core/StateMachine/` | `Framework.Core.StateMachine` |
| `Scripts/Framework/Interfaces/` | `Framework.Interfaces` |
| `Scripts/Core/Stats/` | `Core.Stats`（`PlayerStatBlock`、`IPlayerStatModifierSource`） |
| `Scripts/Core/` (根) | `RPG.Core`（遗留兼容桥，不新增类） |
| `Scripts/Gameplay/Player/` | `Gameplay.Player` |
| `Scripts/Gameplay/Enemy/` | `Gameplay.Enemy` |
| `Scripts/Gameplay/Combat/` | `Gameplay.Combat` |
| `Scripts/Gameplay/Inventory/` | `Gameplay.Inventory` |
| `Scripts/UI/` | `UI`、`UI.Controllers`、`UI.Presenters`、`UI.Views` |
| `Scripts/Managers/` | `Managers` |
| `Scripts/Items/` | `RPG.Items`（待迁移） |
| `Scripts/Skills/` | `RPG.Skills`（待迁移） |

**禁止**：使用 `RPG.Player`、`RPG.Enemy`、`RPG.UI` 等旧命名空间编写新代码。

## 基类使用规则

```csharp
// ✅ 所有 MonoBehaviour 继承自 MonoBehaviourBase
public class MyComponent : Framework.Base.MonoBehaviourBase { }

// ✅ 全局单例继承 SingletonMonoBehaviour<T>
public class MyManager : Framework.Base.SingletonMonoBehaviour<MyManager> { }

// ✅ 可移动实体继承 MoveableBase（Framework.Base）
public class MyMover : Framework.Base.MoveableBase { }

// ✅ 枚举驱动状态机继承 StateMachineBase<TEnum>（Framework.Base）
public class MyFSM : Framework.Base.StateMachineBase<MyStateEnum> { }

// ✅ 攻击组件继承 AttackComponent（Gameplay.Combat）
public class MyAttack : Gameplay.Combat.AttackComponent { }

// ❌ 不要直接继承 MonoBehaviour（新代码）
public class MyComponent : MonoBehaviour { }

// ❌ 不要使用 RPG.Core.Singleton<T>（遗留），改用 SingletonMonoBehaviour<T>
public class MyManager : RPG.Core.Singleton<MyManager> { }  // 旧写法
```

## 事件系统

```csharp
// ✅ 订阅：在 OnEnable 中，取消：在 OnDisable 中
private void OnEnable()  => health.OnHealthChanged += HandleHealthChanged;
private void OnDisable() => health.OnHealthChanged -= HandleHealthChanged;

// ✅ 全局事件通过 Framework.Events.EventManager
Framework.Events.EventManager.Instance.TriggerEvent(GameEvents.PLAYER_DIED);

// ❌ 不要在 Start/Awake 中订阅而不在 OnDestroy 中取消
```

## 输入处理

```csharp
// ✅ 新增输入动作：实现 IPlayerCommand，在 PlayerInputController 中生产
public sealed class MyInputCommand : IPlayerCommand
{
    public void Execute(PlayerCommandContext ctx) { ctx.MyFlag = true; }
}

// ❌ 禁止在组件中直接读 Input.GetKey / Input.GetAxis
// ❌ 禁止绕过 PlayerController 直接修改玩家状态
```

## 伤害应用

```csharp
// ✅ 所有伤害必须经过 CombatResolver
var info = new DamageInfo(amount, sourcePos, gameObject, CombatDamageType.Physical, CombatHitKind.Attack);
CombatResolver.TryApplyDamage(target, info);

// ❌ 禁止直接调用 TakeDamage（遗留代码除外）
target.TakeDamage(20, transform.position);
```

## 属性修改

```csharp
// ✅ 添加永久属性修改：实现 IPlayerStatModifierSource，注册到 PlayerStatsRuntime
public class MyModifier : MonoBehaviour, IPlayerStatModifierSource
{
    public event Action ModifiersChanged;
    public void ApplyModifiers(ref PlayerStatBlock stats) { stats.AttackDamage += bonus; }
}

// ❌ 禁止直接调用 PlayerController.SetAttackDamage() 等方法（除 PlayerStatsRuntime 外）
```

## UI 层规则

```csharp
// ✅ View 只接受 ViewData struct
public class ItemSlot : MonoBehaviour
{
    public void Setup(InventorySlotViewData data) { /* 只渲染，不访问 Domain */ }
}

// ✅ Presenter 转换 Domain → ViewData，触发事件
public class InventoryPresenter
{
    public event Action<InventorySlotViewData[]> SlotsChanged;
}

// ❌ View 不应直接引用 InventorySystem、ItemData 等 Domain 类型
```

## 组件引用

```csharp
// ✅ 使用 [SerializeField]，在 Awake 中 GetComponent，不要依赖 Inspector 拖拽引用
[SerializeField] private PlayerMovement movement;
protected override void Awake()
{
    base.Awake();
    if (movement == null) movement = GetComponent<PlayerMovement>();
}
```

## Legacy 代码约束

- `Assets/Scripts/Legacy/` 目录：只读，不得修改，不得被新代码引用
- `Items/`、`Skills/`、`Quests/`、`Achievements/` 目录：功能可用，但新功能应在对应 `Gameplay.*` 子系统中开发
- 旧命名空间 `RPG.*`：存量代码保留，新文件禁止使用

## 文件命名

- 类名与文件名必须一致（Unity 要求）
- 一文件一公共类（接口和枚举可例外）
- 命名语义清晰：`PlayerHealthPresenter.cs` 优于 `HealthUI.cs`

## Git 提交规范

```
feat: 添加新功能
fix: 修复 Bug
refactor: 重构（不影响外部行为）
chore: 工具/配置/文档变更
perf: 性能优化
```
