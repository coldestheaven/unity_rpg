# Unity RPG 项目目录结构说明

## 当前结构

项目脚本目录已经统一为无数字前缀的语义化命名：

```text
Assets/
├── Scripts/
│   ├── Framework/          # 框架层
│   │   ├── Base/
│   │   ├── Interfaces/
│   │   └── Core/
│   │       ├── Events/
│   │       ├── Patterns/
│   │       ├── StateMachine/
│   │       └── Utils/
│   ├── Data/               # 数据层
│   ├── Gameplay/           # 游戏逻辑层
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Combat/
│   │   └── Inventory/
│   ├── UI/                 # UI层
│   │   ├── Base/
│   │   ├── Controllers/
│   │   └── Views/
│   └── Managers/           # 管理器层
└── Editor/                 # Editor工具
```

## 命名规则

- `Framework`: 框架基础设施和通用能力
- `Data`: ScriptableObject 数据与数据库
- `Gameplay`: 运行时玩法逻辑
- `UI`: UI 基类、控制器和视图
- `Managers`: 全局管理器
- `Editor`: Unity 编辑器扩展工具

## 当前状态

- 数字前缀目录已经完成清理，不再使用 `01_`、`03_`、`04_`、`05_`、`06_` 这类命名
- `Framework/` 与 `Gameplay/` 保留了现有兼容实现，并补齐了此前仅存在于数字前缀目录中的脚本
- `UI/`、`Managers/` 已切换为正式目录名
- `Editor/` 已移动到 `Assets/Editor/`，以符合 Unity 的编辑器目录约定
- `Data/` 保持原命名，不使用 `02_Data/`

## 说明

仓库中仍保留 `Core/`、`Items/`、`Skills/`、`Quests/`、`Achievements/` 等旧的平铺目录，用于后续业务迁移和清理。旧玩家系统已迁移到 `Assets/Scripts/Legacy/Player/`，旧敌人系统已迁移到 `Assets/Scripts/Legacy/Enemy/` 作为兼容层保留。这些目录不属于本次“去数字前缀”重构范围。
