# Unity RPG Framework

模块化 Unity 2D RPG 框架，采用事件驱动架构、命令模式输入、统一战斗管线与数据-表现分离设计。

## 项目结构

```
Assets/
├── Scripts/
│   ├── Framework/      # 基础设施（无业务依赖）
│   ├── Core/           # 核心基类与跨系统结构体
│   ├── Gameplay/       # 运行时玩法系统
│   │   ├── Player/     # 玩家控制、状态机、属性
│   │   ├── Enemy/      # 敌人 AI、表现、攻击
│   │   ├── Combat/     # 统一伤害管线
│   │   └── Inventory/  # 背包（运行时）
│   ├── UI/             # 纯视图层（Controller/Presenter/View）
│   ├── Managers/       # 全局单例管理器
│   ├── Data/           # ScriptableObject 数据定义
│   ├── Items/          # 物品系统
│   ├── Skills/         # 技能系统
│   ├── Quests/         # 任务系统
│   ├── Achievements/   # 成就系统
│   └── Legacy/         # 旧版兼容层（只读）
└── Editor/             # Unity Editor 工具
```

详细说明参见 [`Assets/Docs/Architecture.md`](Assets/Docs/Architecture.md)。

## 核心设计

### 命令模式输入

```
PlayerInputController  →  IPlayerCommand  →  PlayerController  →  PlayerStateMachine
                                                                →  PlayerMovement
```

输入动作被封装为 `IPlayerCommand` 对象入队，状态机和移动组件从 `PlayerCommandContext` 读取快照，与 Unity Input 系统完全解耦。

### 统一战斗管线

所有伤害来源构造 `DamageInfo` 后交由 `CombatResolver` 分发，目标实体通过 `IDamageReceiver.ReceiveDamage()` 处理：

```csharp
var info = new DamageInfo(amount, sourcePos, go, CombatDamageType.Physical, CombatHitKind.Attack);
CombatResolver.TryApplyDamage(targetCollider, info);
```

### 属性聚合

`PlayerStatsRuntime` 聚合所有 `IPlayerStatModifierSource`（装备、Buff、等级），统一写入各组件，外部不得直接修改属性。

### UI 数据-表现分离

Domain → Presenter（转换为 ViewData）→ Controller（渲染 View）→ View（只接收 ViewData）

## 快速开始

### 初始化

```csharp
// 在启动场景 GameManager 中
DataManager.Instance.InitializeAllDatabases();
Framework.Events.EventManager.Instance.Initialize();
```

### 添加伤害来源

```csharp
using Gameplay.Combat;
using Framework.Interfaces;

var info = new DamageInfo(
    amount: 30f,
    sourcePosition: transform.position,
    sourceObject: gameObject,
    damageType: CombatDamageType.Magic,
    hitKind: CombatHitKind.Skill
);
CombatResolver.TryApplyDamage(enemy.GetComponent<Collider2D>(), info);
```

### 添加 Buff

```csharp
var buff = GetComponent<PlayerBuffController>();
buff.AddBuff(new BuffEntry {
    duration = 10f,
    statDelta = new Core.Stats.PlayerStatBlock { AttackDamage = 20 }
});
```

### 订阅玩家血量变化

```csharp
private void OnEnable()  => playerHealth.OnHealthChanged += OnHealthChanged;
private void OnDisable() => playerHealth.OnHealthChanged -= OnHealthChanged;

private void OnHealthChanged(float current, float max) { /* 更新 UI */ }
```

## 文档

| 文档 | 内容 |
|------|------|
| [`Assets/Docs/Architecture.md`](Assets/Docs/Architecture.md) | 分层架构、各模块职责、设计约束 |
| [`Assets/Docs/CombatSystem.md`](Assets/Docs/CombatSystem.md) | 战斗管线、DamageInfo、DamageableBase、Buff |
| [`Assets/Docs/CodingStandards.md`](Assets/Docs/CodingStandards.md) | 命名空间、基类、事件、输入、伤害、UI 规范 |

## 技术要求

- Unity 2021.3+
- .NET Standard 2.1
