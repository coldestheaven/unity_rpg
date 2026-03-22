# 目录清理总结

## 📊 当前项目状态

项目中有两套并行的目录结构：

### 新目录（推荐使用）
```
Assets/Scripts/
├── Framework/              # 框架层 - 8个文件
├── Gameplay/               # 游戏逻辑层 - 5个文件
├── UI/                     # UI层 - 12个文件（04_UI重命名）
├── Managers/               # 管理器层 - 5个文件（05_Managers重命名）
└── Editor/                 # Editor工具 - 4个文件（06_Editor重命名）
```

### 旧目录（需要删除）
```
Assets/Scripts/
├── 01_Framework/           # 旧的框架层 - 8个文件
├── 03_Gameplay/            # 旧的游戏逻辑层 - 10个文件
├── 04_UI/                  # 旧的UI层 - 7个文件
├── 05_Managers/            # 旧的管理器层 - 5个文件
├── 06_Editor/              # 旧的Editor工具 - 4个文件
├── Core/                   # 旧的核心层 - 13个文件
├── Enemy/                  # 旧的敌人系统 - 5个文件
├── Player/                 # 旧的玩家系统 - 7个文件
├── Items/                  # 旧的物品系统 - 6个文件
├── Quests/                 # 旧的任务系统 - 2个文件
├── Skills/                 # 旧的技能系统 - 3个文件
├── Achievements/           # 旧的成就系统 - 2个文件
└── Data/                   # 旧的数据层 - 4个文件
```

## 🎯 清理建议

### 选项1: 完全迁移（推荐）

1. **在文件管理器中重命名目录**
   ```
   01_Framework/   → Framework/
   03_Gameplay/    → Gameplay/
   04_UI/          → UI/
   05_Managers/    → Managers/
   06_Editor/      → Editor/
   ```

2. **删除旧的重复目录**
   - 删除: `Core/`, `Enemy/`, `Player/`, `Items/`, `Quests/`, `Skills/`, `Achievements/`, `Data/`

3. **在Unity中刷新项目**

### 选项2: 只删除旧目录（保留数字前缀）

如果不想重命名，可以只删除旧的重复目录：
- 删除: `Core/`, `Enemy/`, `Player/`, `Items/`, `Quests/`, `Skills/`, `Achievements/`, `Data/`
- 保留: `01_Framework/`, `03_Gameplay/`, `04_UI/`, `05_Managers/`, `06_Editor/`

## ✅ 最终目录结构

清理后的标准结构：

```
Assets/Scripts/
├── Framework/              # 框架层
│   ├── Base/               # 基类
│   ├── Core/               # 核心系统
│   │   ├── Events/         # 事件系统
│   │   ├── Patterns/       # 设计模式
│   │   ├── StateMachine/   # 状态机
│   │   └── Utils/          # 工具类
│   └── Interfaces/         # 接口定义
│
├── Gameplay/               # 游戏逻辑层
│   ├── Player/             # 玩家系统
│   │   ├── Controllers/    # 控制器
│   │   └── Components/     # 组件
│   ├── Enemy/              # 敌人系统
│   │   ├── Controllers/    # 控制器
│   │   └── AI/            # AI系统
│   ├── Combat/             # 战斗系统
│   └── Inventory/          # 背包系统
│
├── UI/                     # UI层
│   ├── Base/               # UI基类
│   ├── Controllers/        # UI控制器
│   └── Views/              # UI视图
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

## 📋 操作检查清单

完成以下操作以确保项目正确清理：

- [ ] 重命名 `01_Framework/` → `Framework/`
- [ ] 重命名 `03_Gameplay/` → `Gameplay/`
- [ ] 重命名 `04_UI/` → `UI/`
- [ ] 重命名 `05_Managers/` → `Managers/`
- [ ] 重命名 `06_Editor/` → `Editor/`
- [ ] 删除 `Core/`
- [ ] 删除 `Enemy/`
- [ ] 删除 `Player/`
- [ ] 删除 `Items/`
- [ ] 删除 `Quests/`
- [ ] 删除 `Skills/`
- [ ] 删除 `Achievements/`
- [ ] 删除 `Data/`
- [ ] 在Unity中刷新项目
- [ ] 检查控制台无错误
- [ ] 提交到Git

## 🚀 快速操作（使用Git）

```bash
cd d:/Unity/unity_rpg/unity_rpg/Assets/Scripts

# 重命名目录
git mv 01_Framework Framework
git mv 03_Gameplay Gameplay
git mv 04_UI UI
git mv 05_Managers Managers
git mv 06_Editor Editor

# 删除旧目录
git rm -r Core Enemy Player Items Quests Skills Achievements Data

# 提交更改
cd ../..
git add .
git commit -m "cleanup: remove old directories and rename to remove number prefixes"
git push
```

## 📚 相关文档

- `REMOVE_NUMBER_PREFIXES.md` - 详细的操作指南
- `DIRECTORY_STRUCTURE.md` - 目录结构说明
- `DIRECTORY_NAMING.md` - 目录命名规范

## ⚠️ 重要提醒

1. **关闭Unity**: 在重命名目录前必须关闭Unity项目
2. **备份**: 操作前建议备份项目
3. **Git提交**: 使用`git mv`可以保留文件历史
4. **Unity刷新**: 重命名后需要在Unity中刷新项目

## 🎉 完成效果

完成后项目将具有：
- ✅ 语义化的目录名称
- ✅ 无数字前缀
- ✅ 无重复的旧目录
- ✅ 清晰的项目结构
- ✅ 易于维护和扩展
