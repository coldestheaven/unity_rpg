# Unity RPG 项目重构文档

## 📁 新目录结构

重构后的项目采用分层架构，每个层级职责清晰：

```
Assets/Scripts/
├── 01_Framework/              # 框架层 - 基础架构
│   ├── Core/
│   │   ├── Events/          # 事件系统
│   │   ├── StateMachine/    # 状态机
│   │   ├── Patterns/        # 设计模式（单例、对象池）
│   │   └── Utils/           # 工具类
│   ├── Interfaces/          # 接口定义
│   └── Base/                # 基类
│
├── 02_Data/                 # 数据层 - ScriptableObject
│   ├── Items/               # 物品数据
│   ├── Skills/              # 技能数据
│   ├── Quests/              # 任务数据
│   └── Databases/           # 数据库
│
├── 03_Gameplay/             # 游戏逻辑层
│   ├── Player/              # 玩家系统
│   │   ├── Controllers/     # 控制器
│   │   └── Components/      # 组件
│   ├── Enemy/               # 敌人系统
│   │   ├── Controllers/
│   │   └── AI/
│   ├── Combat/              # 战斗系统
│   └── Inventory/           # 背包系统
│
├── 04_UI/                   # UI层
│   ├── Base/                # UI基类
│   ├── Controllers/         # UI控制器
│   └── Views/               # UI视图组件
│
├── 05_Managers/             # 管理器层
│   ├── GameManager.cs
│   ├── GameStateManager.cs
│   ├── SaveManager.cs
│   ├── AudioManager.cs
│   └── DataManager.cs
│
└── 06_Editor/               # Editor工具
    ├── Editors/             # 自定义编辑器
    ├── Windows/             # 编辑器窗口
    └── Tools/               # 工具脚本
```

## 🏗️ 架构特点

### 1. 命名空间规范

- **Framework**: 基础框架代码
  - `Framework.Core.Patterns` - 设计模式
  - `Framework.Core.StateMachine` - 状态机
  - `Framework.Events` - 事件系统
  - `Framework.Interfaces` - 接口
  - `Framework.Utils` - 工具类
  - `Framework.Base` - 基类

- **Managers**: 游戏管理器
  - `Managers.GameManager`
  - `Managers.GameStateManager`
  - `Managers.SaveManager`
  - 等等...

- **Gameplay**: 游戏逻辑
  - `Gameplay.Player` - 玩家系统
  - `Gameplay.Enemy` - 敌人系统
  - `Gameplay.Combat` - 战斗系统
  - `Gameplay.Inventory` - 背包系统

- **UI**: UI系统
  - `UI.Base` - UI基类
  - `UI.Controllers` - UI控制器
  - `UI.Views` - UI视图

- **Editor**: 编辑器工具
  - `Editor` - 编辑器
  - `Editor.Windows` - 编辑器窗口
  - `Editor.Tools` - 工具

### 2. 设计原则

#### 单一职责原则 (SRP)
每个类只负责一个功能：
- `PlayerMovement` 只负责移动
- `PlayerCombat` 只负责战斗
- `PlayerHealth` 只负责健康

#### 开闭原则 (OCP)
对扩展开放，对修改关闭：
- 使用事件系统解耦
- 使用接口定义契约

#### 依赖倒置原则 (DIP)
依赖抽象而非具体实现：
- 使用 `IDamageable` 接口
- 使用 `IState` 接口

### 3. 事件驱动架构

使用 `EventManager` 进行系统间通信：

```csharp
// 触发事件
EventManager.Instance.TriggerEvent(GameEvents.PLAYER_DIED);

// 监听事件
EventManager.Instance.AddListener(GameEvents.PLAYER_DIED, HandlePlayerDeath);
```

### 4. 组件化设计

玩家系统采用组件化架构：

```
PlayerController (控制器)
├── PlayerMovement (移动组件)
├── PlayerCombat (战斗组件)
├── PlayerHealth (健康组件)
└── PlayerInputController (输入组件)
```

## 🔄 迁移指南

### 旧代码 -> 新代码映射

| 旧位置 | 新位置 | 命名空间变化 |
|--------|--------|-------------|
| `Core/Singleton.cs` | `01_Framework/Core/Patterns/Singleton.cs` | `Framework.Core.Patterns` |
| `Core/EventManager.cs` | `01_Framework/Core/Events/EventManager.cs` | `Framework.Events` |
| `Player/PlayerController.cs` | `03_Gameplay/Player/Controllers/PlayerController.cs` | `Gameplay.Player` |
| `Player/PlayerHealth.cs` | `03_Gameplay/Player/Components/PlayerHealth.cs` | `Gameplay.Player` |
| `UI/UIManager.cs` | `04_UI/Controllers/UIManager.cs` | `UI.Controllers` |
| `GameManager.cs` | `05_Managers/GameManager.cs` | `Managers` |

### 修改命名空间

如果需要保留旧代码，只需更新命名空间：

```csharp
// 旧命名空间
using RPG.Core;
using RPG.Player;

// 新命名空间
using Framework.Core.Patterns;
using Gameplay.Player;
```

### 更新事件名称

```csharp
// 旧事件
EventManager.Instance.TriggerEvent("PlayerDied");

// 新事件
EventManager.Instance.TriggerEvent(Framework.Events.GameEvents.PLAYER_DIED);
```

## 📝 代码示例

### 创建新的玩家组件

```csharp
using UnityEngine;
using Gameplay.Player;

public class MyPlayerComponent : Framework.Base.MonoBehaviourBase
{
    protected override void Awake()
    {
        base.Awake();
        // 初始化代码
    }

    protected override void Update()
    {
        base.Update();
        // 更新代码
    }
}
```

### 使用事件系统

```csharp
public class MyListener : Framework.Base.MonoBehaviourBase
{
    private void Start()
    {
        Framework.Events.EventManager.Instance.AddListener(
            Framework.Events.GameEvents.PLAYER_DIED,
            HandlePlayerDied
        );
    }

    private void HandlePlayerDied(object data)
    {
        Debug.Log("Player died!");
    }

    private void OnDestroy()
    {
        Framework.Events.EventManager.Instance.RemoveListener(
            Framework.Events.GameEvents.PLAYER_DIED,
            HandlePlayerDied
        );
    }
}
```

### 创建自定义UI面板

```csharp
using UnityEngine;
using UI;

public class MyPanel : UIPanelBase
{
    protected override void OnShowComplete()
    {
        base.OnShowComplete();
        Debug.Log("Panel shown!");
    }

    protected override void OnHideComplete()
    {
        base.OnHideComplete();
        Debug.Log("Panel hidden!");
    }
}
```

## 🎯 最佳实践

### 1. 组件引用

使用 `[SerializeField]` 并在 `Awake()` 中初始化：

```csharp
[SerializeField] private PlayerMovement movement;

protected override void Awake()
{
    base.Awake();
    movement = GetComponent<PlayerMovement>();
}
```

### 2. 事件订阅

在 `OnEnable()` 中订阅，在 `OnDisable()` 中取消订阅：

```csharp
private void OnEnable()
{
    health.OnHealthChanged += HandleHealthChanged;
}

private void OnDisable()
{
    health.OnHealthChanged -= HandleHealthChanged;
}
```

### 3. 接口实现

优先使用接口而非具体类：

```csharp
public void DealDamage(IDamageable target)
{
    target.TakeDamage(10, transform.position);
}
```

### 4. 单例访问

使用泛型单例基类：

```csharp
// 访问管理器
Managers.GameManager.Instance.PauseGame();
UI.Controllers.UIManager.Instance.ShowPanel("Inventory");
```

## 🔧 Editor工具

### 生成默认数据

使用菜单：`RPG/Generate Default Data` 自动创建数据库

### 打开游戏数据库

使用菜单：`RPG/Game Database` 打开数据库管理窗口

### 清除玩家数据

使用菜单：`RPG/Clear All Data` 清除所有玩家数据

## ⚠️ 注意事项

1. **不要删除旧的代码文件**，直到新代码完全测试通过
2. **逐步迁移**，一次迁移一个系统
3. **测试每个迁移的组件**
4. **更新所有的 `using` 语句**
5. **更新事件常量引用**

## 📚 下一步

1. ✅ 测试新架构的玩家系统
2. ✅ 测试新架构的敌人系统
3. ✅ 测试新架构的UI系统
4. ⬜ 迁移技能系统
5. ⬜ 迁移任务系统
6. ⬜ 迁移成就系统
7. ⬜ 删除旧代码文件
8. ⬜ 提交到Git

## 🎉 总结

重构后的项目具有以下优势：

- ✅ 清晰的分层架构
- ✅ 统一的命名空间
- ✅ 组件化设计
- ✅ 事件驱动架构
- ✅ 易于扩展和维护
- ✅ 符合SOLID原则

新架构为未来的功能扩展提供了坚实的基础！
