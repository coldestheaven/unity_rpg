# Unity RPG 项目目录结构说明

## 新的命名规则

去掉数字前缀，使用语义化命名：

```
Assets/Scripts/
├── Framework/              # 框架层 - 基础架构
│   ├── Core/               # 核心系统
│   │   ├── Events/         # 事件系统
│   │   ├── StateMachine/   # 状态机
│   │   ├── Patterns/       # 设计模式
│   │   └── Utils/          # 工具类
│   ├── Interfaces/         # 接口定义
│   └── Base/               # 基类
│
├── Gameplay/               # 游戏逻辑层
│   ├── Player/             # 玩家系统
│   ├── Enemy/              # 敌人系统
│   ├── Combat/             # 战斗系统
│   └── Inventory/          # 背包系统
│
├── UI/                     # UI层
│   ├── Base/               # UI基类
│   ├── Controllers/        # UI控制器
│   └── Views/              # UI视图组件
│
├── Managers/               # 管理器层
│   ├── GameManager.cs
│   ├── GameStateManager.cs
│   ├── SaveManager.cs
│   ├── AudioManager.cs
│   └── DataManager.cs
│
└── Editor/                 # Editor工具
    ├── Editors/            # 自定义编辑器
    ├── Windows/            # 编辑器窗口
    └── Tools/              # 工具脚本
```

## 命名规则

- **Framework**: 框架基础架构
- **Gameplay**: 游戏逻辑
- **UI**: 用户界面
- **Managers**: 管理器
- **Editor**: 编辑器工具

## 迁移说明

需要将以下目录重命名：
- `01_Framework/` → `Framework/`
- `03_Gameplay/` → `Gameplay/`
- `04_UI/` → `UI/`
- `05_Managers/` → `Managers/`
- `06_Editor/` → `Editor/`

数据层目录保持为 `Data/`
