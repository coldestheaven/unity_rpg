# 命名空间问题修复指南

## 问题说明

项目中存在新旧两套代码，使用不同的命名空间，导致编译错误：

### 新命名空间
- `Framework` - 框架层
- `Gameplay.Player` - 玩家系统
- `Gameplay.Combat` - 战斗系统
- `Gameplay.Enemy` - 敌人系统
- `Gameplay.Inventory` - 背包系统
- `Managers` - 管理器层
- `UI` - UI层（待定义）

### 旧命名空间（不存在）
- `RPG.Core`
- `RPG.Player`
- `RPG.Items`
- `RPG.UI`

## 受影响的文件

以下文件引用了不存在的命名空间：

### UI 层
- `Assets/Scripts/UI/HUDController.cs`
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/UIBase.cs`
- `Assets/Scripts/UI/MenuController.cs`
- `Assets/Scripts/UI/InventoryUI.cs`

### Editor 层
- `Assets/Scripts/Editor/SkillControllerEditor.cs`
- `Assets/Scripts/Editor/SkillDataEditor.cs`
- `Assets/Scripts/Editor/SkillDatabaseEditor.cs`
- `Assets/Scripts/Editor/SkillEffectEditor.cs`

## 解决方案

### 选项 1: 更新旧文件（推荐）

将旧文件更新为使用新的命名空间：

```csharp
// 旧代码
using RPG.Core;
using RPG.Player;
using RPG.Items;

// 新代码
using Framework;
using Gameplay.Player;
using Gameplay.Inventory;
```

### 选项 2: 暂时禁用旧文件

将旧文件重命名为 `.cs.bak` 或移动到其他目录，避免编译：

```bash
# 重命名文件以避免编译
mv HUDController.cs HUDController.cs.bak
mv UIManager.cs UIManager.cs.bak
```

### 选项 3: 删除旧文件

如果这些文件不再使用，可以直接删除：

```bash
# 删除旧的UI文件
rm Assets/Scripts/UI/HUDController.cs
rm Assets/Scripts/UI/UIManager.cs
rm Assets/Scripts/UI/UIBase.cs
rm Assets/Scripts/UI/MenuController.cs
rm Assets/Scripts/UI/InventoryUI.cs
```

## 当前状态

✅ 已修复: `PlayerHealth.cs` - 移除了 `using Gameplay.Combat;`

⏳ 待修复: UI 和 Editor 文件的命名空间引用

## 建议操作

1. **短期**: 选项 2 - 暂时禁用旧文件，让项目可以编译
2. **中期**: 选项 1 - 更新旧文件使用新命名空间
3. **长期**: 确定哪些文件需要保留，哪些可以删除

---

**创建日期**: 2026-03-22
