# 架构总览

## 分层结构

```
Assets/
├── Scripts/
│   ├── Framework/          # 基础设施层：可复用、无业务依赖
│   ├── Core/               # 核心业务基类与兼容层
│   ├── Gameplay/           # 运行时玩法系统
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Combat/
│   │   └── Inventory/
│   ├── UI/                 # 纯视图层
│   ├── Managers/           # 全局单例管理器
│   ├── Data/               # ScriptableObject 数据定义
│   ├── Items/              # 物品系统（待迁移至 Gameplay）
│   ├── Skills/             # 技能系统（待迁移至 Gameplay）
│   ├── Quests/             # 任务系统（待迁移至 Gameplay）
│   ├── Achievements/       # 成就系统（待迁移至 Gameplay）
│   └── Legacy/             # 旧版代码兼容层，只读不扩展
└── Editor/                 # Unity Editor 工具扩展
```

## Framework 层

纯基础设施，不引用任何游戏业务代码。

| 文件 | 内容 |
|------|------|
| `Base/BaseClasses.cs` | `MonoBehaviourBase`、`SingletonMonoBehaviour<T>` |
| `Base/MoveableBase.cs` | 可移动实体抽象基类，实现 `IMovable` |
| `Base/StateMachineBase.cs` | 泛型状态机基类，包装 `Framework.Core.StateMachine` |
| `Core/Events/EventManager.cs` | 全局字符串事件总线 |
| `Core/Events/EventDelegates.cs` | 事件委托类型定义 |
| `Core/Patterns/Singleton.cs` | 线程安全 MonoBehaviour `Singleton<T>` |
| `Core/Patterns/ObjectPool.cs` | 通用对象池 |
| `Core/StateMachine/StateMachine.cs` | 通用状态机（Initialize/TransitionTo/Update） |
| `Core/Utils/Extensions.cs` | 扩展方法 |
| `Interfaces/IGameInterfaces.cs` | `IDamageable`、`IDamageReceiver`、`IKillable`、`IMovable`、`IInteractable` |

## Core 层

跨系统共享的游戏业务结构体与遗留兼容桥。`Core.Base.*` 已清空，纯抽象基类已上移至 `Framework.Base`。

| 文件 | 内容 |
|------|------|
| `Stats/PlayerStatBlock.cs` | `PlayerStatBlock` struct、`IPlayerStatModifierSource` 接口 |
| `PlayerProgress.cs` | 玩家进度数据（等级、经验、金币），`PlayerProgressManager` |
| `SaveSystem.cs` | JSON 存档读写，`RPG.Core.SaveSystem` |
| `GameState.cs` | 游戏状态枚举与 `RPG.Core.GameStateManager`（兼容桥） |
| `GameManager.cs` | `RPG.Core.GameManager`（兼容桥） |
| `CharacterStats.cs` | 旧 int 型属性包（遗留，不新增依赖） |
| `Singleton.cs` | `RPG.Core.Singleton<T>` 遗留单例基类，新代码用 `Framework.Base.SingletonMonoBehaviour<T>` |

## Gameplay 层

### Player 子系统

逻辑与表现完全分离，输入通过命令队列传递。

```
PlayerInputController          ← 读取 Unity Input，生产 IPlayerCommand
      ↓ EnqueueCommand()
PlayerController               ← 消费命令，维护 PlayerCommandContext；暴露输入状态门面
      ↓ 读取 context
PlayerStateMachine             ← 驱动状态转换（Idle/Move/Jump/Attack/Interact/Skill/Dead）
PlayerMovement                 ← 读取 controller.MoveInput / JumpPressed 执行物理移动
PlayerCombat                   ← 执行近战攻击，通过 CombatResolver 应用伤害
PlayerPresenter                ← 读取 controller 状态，驱动 Animator 和 SpriteRenderer
PlayerHealth (DamageableBase)  ← 生命值、无敌帧、击退，死亡触发 GameState 切换
PlayerStatsRuntime             ← 聚合 IPlayerStatModifierSource，将最终属性写入各组件
PlayerBuffController           ← 管理限时 buff，实现 IPlayerStatModifierSource
```

关键文件：

| 文件 | 职责 |
|------|------|
| `Controllers/PlayerCommands.cs` | `IPlayerCommand`、`PlayerCommandContext`、各具体命令 |
| `Controllers/PlayerController.cs` | 命令消费者；输入状态门面 |
| `Controllers/PlayerInputController.cs` | 命令生产者 |
| `Controllers/PlayerStateMachine.cs` | 状态机，读取 controller 状态驱动转换 |
| `Controllers/PlayerActionContracts.cs` | `IPlayerSkillCaster`、`IPlayerInteractor` 接口 |
| `Components/PlayerMovement.cs` | 物理移动 |
| `Components/PlayerCombat.cs` | 攻击执行 |
| `Components/PlayerHealth.cs` | 生命值管理 |
| `Components/PlayerPresenter.cs` | 动画与朝向表现 |
| `Components/PlayerStatsRuntime.cs` | 属性聚合 |
| `Components/PlayerBuffController.cs` | Buff 系统 |

### Enemy 子系统

| 文件 | 职责 |
|------|------|
| `AI/EnemyAI.cs` | AI 决策（巡逻/追踪/攻击），暴露状态属性 |
| `Components/EnemyPresenter.cs` | 读取 EnemyAI 状态，驱动动画和朝向 |
| `Components/EnemyAttack.cs` | 攻击执行，通过 CombatResolver 应用伤害 |
| `Components/EnemyReward.cs` | 死亡奖励（经验、掉落） |
| `Controllers/EnemyController.cs` | 敌人根 GameObject 控制器 |

### Combat 子系统

统一伤害管线，所有伤害来源最终经过 `CombatResolver`。

| 文件 | 职责 |
|------|------|
| `DamageInfo.cs` | `DamageInfo` struct、`CombatDamageType` 枚举、`CombatHitKind` 枚举 |
| `DamageableBase.cs` | 可受伤实体抽象基类，实现 `IDamageable` + `IDamageReceiver`（原 `Core.Base`） |
| `AttackComponent.cs` | 冷却门控攻击抽象组件，`OnAttackStarted/Finished` 事件（原 `Core.Components`） |
| `CombatResolver.cs` | 静态伤害入口：优先调用 `IDamageReceiver`，回退 `IDamageable` |
| `Health.cs` | 通用实体生命值组件（继承 `DamageableBase`） |
| `Hitbox.cs` | 碰撞体伤害触发，构造 `DamageInfo` 交由 `CombatResolver` |
| `CombatDamageTypeMapper.cs` | `RPG.Skills.DamageType` → `CombatDamageType` 映射工具 |

## UI 层

严格数据-表现分离：Controller/Presenter 不直接访问 Domain 数据，只消费 ViewData。

```
Domain (InventorySystem)
      ↓
InventoryPresenter              ← 转换为 InventorySlotViewData / InventoryDetailsViewData
      ↓ events
InventoryUIController           ← 纯渲染，接收 ViewData 驱动 ItemSlot
ItemSlot                        ← 纯 View，通过 Setup(InventorySlotViewData) 刷新
```

| 文件 | 职责 |
|------|------|
| `Base/UIElementBase.cs` | UI 元素基类 |
| `Base/UIPanelBase.cs` | UI 面板基类（淡入淡出） |
| `Controllers/UIManager.cs` | 全局面板管理 |
| `Controllers/HUDController.cs` | HUD（血量、等级），事件驱动更新 |
| `Controllers/InventoryUIController.cs` | 背包 UI 控制器，消费 Presenter 事件 |
| `Presenters/InventoryPresenter.cs` | 背包数据→视图数据转换层 |
| `Views/HealthBar.cs` | 血条视图 |
| `Views/ItemSlot.cs` | 物品槽视图 |

## Managers 层

全局单例，跨场景持久化。

| 文件 | 职责 |
|------|------|
| `GameManager.cs` | 游戏生命周期、场景切换 |
| `GameStateManager.cs` | 游戏状态机（Menu/Playing/Paused/GameOver） |
| `DataManager.cs` | ScriptableObject 数据库统一入口 |
| `SaveManager.cs` | 存档槽管理 |
| `AudioManager.cs` | 音效/BGM 管理 |

## 属性系统

```
PlayerProgressManager.OnProgressChanged
EquipmentSystem (IPlayerStatModifierSource)
PlayerBuffController (IPlayerStatModifierSource)
        ↓ 所有 source 变更时
PlayerStatsRuntime.Recalculate()
        ↓ 写入最终值
PlayerHealth.SetMaxHealth()
PlayerCombat.SetAttackDamage()
PlayerMovement.SetMoveSpeed()
```

## 设计约束

1. **Framework 层禁止引用任何 Gameplay/Core 命名空间**
2. **UI 层只访问 Presenter/ViewData，不直接读 Domain Model**
3. **所有伤害路径必须经过 `CombatResolver.TryApplyDamage`**
4. **输入只能通过 `IPlayerCommand` → `PlayerController` 传递，禁止直接读 Input**
5. **属性修改只能通过 `IPlayerStatModifierSource` → `PlayerStatsRuntime` 流动**
