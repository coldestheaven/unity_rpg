# 目录迁移指南 - 去除数字前缀

## 迁移说明

将数字前缀的目录重命名为语义化名称。

## 目录映射关系

| 旧目录名 | 新目录名 | 状态 |
|---------|---------|------|
| `01_Framework/` | `Framework/` | ✅ 已创建 |
| `02_Data/` | `Data/` | 保持不变 |
| `03_Gameplay/` | `Gameplay/` | ✅ 已创建部分 |
| `04_UI/` | `UI/` | 待创建 |
| `05_Managers/` | `Managers/` | 待创建 |
| `06_Editor/` | `Editor/` | 待创建 |

## 迁移步骤

### 方式1: 手动迁移（推荐）

在Unity中或文件管理器中手动重命名目录：

1. 重命名 `01_Framework/` 为 `Framework/`
2. 重命名 `03_Gameplay/` 为 `Gameplay/`
3. 重命名 `04_UI/` 为 `UI/`
4. 重命名 `05_Managers/` 为 `Managers/`
5. 重命名 `06_Editor/` 为 `Editor/`

### 方式2: 使用Git命令

```bash
cd d:/Unity/unity_rpg/unity_rpg/Assets/Scripts
git mv 01_Framework Framework
git mv 03_Gameplay Gameplay
git mv 04_UI UI
git mv 05_Managers Managers
git mv 06_Editor Editor
```

### 方式3: 重新创建（当前方式）

在新目录下重新创建所有文件，然后删除旧目录。

## 已完成的迁移

### Framework/
- ✅ Core/Events/EventManager.cs
- ✅ Core/Events/EventDelegates.cs
- ✅ Core/Patterns/Singleton.cs
- ✅ Core/Patterns/ObjectPool.cs
- ✅ Core/StateMachine/StateMachine.cs
- ✅ Core/Utils/Extensions.cs
- ✅ Interfaces/IGameInterfaces.cs
- ✅ Base/BaseClasses.cs

### Gameplay/Player/
- ✅ Controllers/PlayerController.cs
- ✅ Controllers/PlayerInputController.cs
- ✅ Components/PlayerHealth.cs
- ✅ Components/PlayerMovement.cs
- ✅ Components/PlayerCombat.cs

## 待迁移的文件

### Gameplay/
- Combat/Health.cs
- Combat/Hitbox.cs
- Enemy/Controllers/EnemyController.cs
- Enemy/AI/EnemyAI.cs
- Inventory/InventorySystem.cs

### UI/
- Base/UIElementBase.cs
- Base/UIPanelBase.cs
- Controllers/UIManager.cs
- Controllers/HUDController.cs
- Controllers/InventoryUIController.cs
- Views/HealthBar.cs
- Views/ItemSlot.cs

### Managers/
- GameManager.cs
- GameStateManager.cs
- SaveManager.cs
- AudioManager.cs
- DataManager.cs

### Editor/
- Editors/PlayerEditor.cs
- Editors/EnemyEditor.cs
- Editors/SkillDataEditor.cs
- Windows/GameDatabaseWindow.cs
- Tools/DataGenerator.cs

## 注意事项

1. **Unity项目刷新**: 重命名目录后，Unity会自动刷新项目
2. **命名空间**: 命名空间保持不变，只改变目录结构
3. **脚本引用**: Unity会自动处理脚本引用的更新
4. **元数据**: Unity会保留.meta文件

## 验证迁移

迁移完成后，检查以下几点：

1. ✅ 所有文件都能正常编译
2. ✅ 没有Missing Script错误
3. ✅ 旧目录已被删除
4. ✅ Git提交没有问题

## Git提交

迁移完成后：

```bash
git add .
git commit -m "refactor: remove number prefixes from directory names"
git push
```

## 下一步

1. 完成所有文件的迁移
2. 删除旧的带数字前缀的目录
3. 测试所有功能
4. 提交到Git
