# Unity RPG 项目重构文档

## 当前目录结构

项目当前采用无数字前缀的分层目录结构：

```text
Assets/
├── Scripts/
│   ├── Framework/          # 框架层
│   ├── Data/               # 数据层
│   ├── Gameplay/           # 游戏逻辑层
│   ├── UI/                 # UI层
│   └── Managers/           # 管理器层
└── Editor/                 # Unity 编辑器工具
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

## 🔄 迁移说明

### 本次目录重构

| 历史目录 | 当前目录 | 备注 |
|--------|--------|------|
| `01_Framework/` | `Framework/` | 已合并缺失脚本 |
| `03_Gameplay/` | `Gameplay/` | 已合并缺失子目录 |
| `04_UI/` | `UI/` | 已切换为正式 UI 结构 |
| `05_Managers/` | `Managers/` | 已切换为正式管理器目录 |
| `06_Editor/` | `Editor/` | 已切换为正式 Editor 目录 |

### 旧平铺目录到新分层目录

| 旧位置 | 当前推荐位置 | 命名空间 |
|--------|-------------|---------|
| `Core/Singleton.cs` | `Framework/Core/Patterns/Singleton.cs` | `Framework.Core.Patterns` |
| `Core/EventManager.cs` | `Framework/Core/Events/EventManager.cs` | `Framework.Events` |
| `Player/PlayerController.cs` | `Gameplay/Player/Controllers/PlayerController.cs` | `Gameplay.Player` |
| `Player/PlayerHealth.cs` | `Gameplay/Player/Components/PlayerHealth.cs` | `Gameplay.Player` |
| `UI/UIManager.cs` | `UI/Controllers/UIManager.cs` | `UI.Controllers` |
| `GameManager.cs` | `Managers/GameManager.cs` | `Managers` |

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

1. 本次已完成的是目录命名收敛，不等于旧业务目录已经全部迁移完毕
2. `Data/` 仍是正式目录，不属于待删除目录
3. `Core/`、`Items/`、`Skills/`、`Quests/`、`Achievements/` 等旧目录仍需后续逐步迁移
4. 旧 `Player/` 目录已迁移到 `Assets/Scripts/Legacy/Player/`，不再作为主路径使用
5. 旧 `Enemy/` 目录已迁移到 `Assets/Scripts/Legacy/Enemy/`，不再作为主路径使用
6. 在 Unity 中应重新导入脚本并检查 Console
7. 旧代码删除应以场景、Prefab 和资源引用验证为前提

## 📚 下一步

1. 在 Unity 中验证脚本重新导入结果
2. 评估旧平铺业务目录的真实引用关系
3. 按系统拆分后续业务迁移，而不是继续做大范围目录删除

## 🎉 总结

重构后的项目具有以下优势：

- ✅ 清晰的分层架构
- ✅ 统一的命名空间
- ✅ 组件化设计
- ✅ 事件驱动架构
- ✅ 易于扩展和维护
- ✅ 符合SOLID原则

新架构为未来的功能扩展提供了坚实的基础！
