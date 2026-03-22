# Unity RPG Framework

一个现代化、模块化的Unity RPG游戏开发框架,采用事件驱动架构和ScriptableObject数据配置系统。

## 🌟 特性

- **模块化设计**: 每个系统独立,职责单一,易于维护和扩展
- **事件驱动**: 使用EventManager解耦系统间通信,降低耦合度
- **ScriptableObject**: 数据与逻辑分离,支持可视化配置
- **状态机**: 敌人AI、游戏状态使用状态机管理
- **单例模式**: 统一使用泛型Singleton基类
- **自定义编辑器**: 提供友好的Unity Editor工具

## 📁 项目结构

```
Assets/Scripts/
├── Core/                    # 核心系统
│   ├── Events/             # 事件系统
│   ├── Base/               # 基类
│   ├── Components/         # 组件
│   ├── Interfaces/         # 接口
│   ├── GameManager.cs      # 游戏管理器
│   ├── GameState.cs        # 游戏状态管理器
│   ├── PlayerProgress.cs   # 玩家进度管理器
│   ├── SaveSystem.cs       # 保存系统
│   ├── Singleton.cs        # 单例基类
│   └── CharacterStats.cs   # 角色属性
├── Player/                  # 玩家系统
│   ├── PlayerController.cs
│   ├── PlayerHealth.cs
│   ├── PlayerMovement.cs
│   ├── PlayerCombat.cs
│   ├── PlayerInput.cs
│   └── PlayerState.cs
├── Enemy/                   # 敌人系统
│   ├── EnemyData.cs
│   ├── EnemyState.cs
│   └── EnemyBase.cs
├── Items/                   # 物品系统
│   ├── ItemData.cs
│   ├── InventorySystem.cs
│   ├── ItemPickup.cs
│   ├── EquipmentSystem.cs
│   └── ItemSystem.cs
├── UI/                      # UI系统
│   ├── UIBase.cs
│   ├── HUDController.cs
│   ├── MenuController.cs
│   ├── InventoryUI.cs
│   └── UIManager.cs
├── Skills/                  # 技能系统
│   ├── SkillData.cs
│   ├── SkillController.cs
│   └── SkillEffect.cs
├── Quests/                  # 任务系统
│   ├── QuestData.cs
│   └── QuestManager.cs
├── Achievements/            # 成就系统
│   ├── AchievementData.cs
│   └── AchievementManager.cs
├── Data/                    # 数据管理
│   ├── ItemDatabase.cs
│   ├── QuestDatabase.cs
│   ├── AchievementDatabase.cs
│   └── DataManager.cs
└── Editor/                  # 编辑器工具
    ├── SkillDataEditor.cs
    ├── SkillControllerEditor.cs
    ├── SkillDatabaseEditor.cs
    └── SkillEffectEditor.cs
```

## 🚀 快速开始

### 1. 初始化系统

在游戏开始时初始化各个管理器:

```csharp
// 在GameManager或启动场景中
DataManager.Instance.InitializeAllDatabases();
EventManager.Instance.Initialize();
```

### 2. 创建ScriptableObject数据

在Unity编辑器中:
- 右键点击 `Assets/Resources`
- 选择 `Create > RPG/...`
- 创建所需的数据资产(物品、技能、任务等)

### 3. 使用事件系统

```csharp
// 触发事件
EventManager.Instance.TriggerEvent("PlayerDied", null);

// 监听事件
EventManager.Instance.AddListener("PlayerDied", OnPlayerDied);

private void OnPlayerDied(object[] args)
{
    Debug.Log("玩家死亡");
}
```

### 4. 使用管理器

```csharp
// 访问玩家进度
int level = PlayerProgressManager.Instance.GetLevel();
PlayerProgressManager.Instance.AddExperience(100);

// 访问物品系统
ItemSystem.Instance.AddItem(itemData);
ItemSystem.Instance.UseItem(consumableData);

// 访问技能系统
SkillController skillController = GetComponent<SkillController>();
skillController.TryUseSkill(0); // 使用第一个技能槽的技能
```

## 🎮 主要系统

### 玩家系统
- **PlayerState**: 玩家状态管理
- **PlayerInput**: 统一输入处理
- **PlayerMovement**: 独立移动系统
- **PlayerCombat**: 独立战斗系统
- **PlayerHealth**: 健康系统,事件驱动

### 敌人系统
- **EnemyData**: ScriptableObject敌人配置
- **EnemyState**: 状态机(Idle/Patrol/Chase/Attack/Death)
- **EnemyBase**: 敌人基类

### 物品系统
- **ItemData**: 物品基类(消耗品、装备、武器、护甲、任务物品)
- **InventorySystem**: 背包系统,支持堆叠
- **ItemPickup**: 物品拾取,自动/手动拾取
- **EquipmentSystem**: 装备系统

### UI系统
- **UIBase**: UI面板基类,支持淡入淡出
- **HUDController**: HUD显示控制器
- **MenuController**: 菜单控制器
- **InventoryUI**: 背包UI

### 技能系统
- **SkillData**: ScriptableObject技能配置
- **SkillController**: 技能控制器,冷却系统
- **SkillEffect**: 技能效果系统(投射物、范围、波浪、瞬发)

### 任务系统
- **QuestData**: ScriptableObject任务配置
- **QuestManager**: 任务管理器
- 多种任务目标(击杀、收集、对话等)

### 成就系统
- **AchievementData**: ScriptableObject成就配置
- **AchievementManager**: 成就管理器
- 多种成就条件类型

### 保存系统
- **SaveSystem**: 多存档支持,JSON序列化
- 自动保存、快速保存功能

## 🛠️ Editor工具

### 技能编辑器
- 右键 `RPG/Skill Database` 打开技能数据库窗口
- 自定义技能数据Inspector
- 技能效果编辑器

### 数据管理
- 统一的数据管理器访问所有数据库
- 支持热重载(Editor Only)

## 📖 使用示例

### 创建技能

```csharp
// 1. 创建SkillData资产
SkillData fireball = ScriptableObject.CreateInstance<SkillData>();
fireball.skillName = "火球术";
fireball.skillType = SkillType.Active;
fireball.baseDamage = 50;
fireball.cooldown = 5f;

// 2. 配置到SkillController
SkillController controller = GetComponent<SkillController>();
controller.skillSlots[0] = fireball;
```

### 添加物品

```csharp
// 创建物品数据
ConsumableData healthPotion = ScriptableObject.CreateInstance<ConsumableData>();
healthPotion.itemName = "治疗药水";
healthPotion.healAmount = 50;

// 添加到背包
InventorySystem inventory = GetComponent<InventorySystem>();
inventory.AddItem(healthPotion, 5);
```

### 创建任务

```csharp
// 创建任务数据
QuestData quest = ScriptableObject.CreateInstance<QuestData>();
quest.questId = "kill_10_slimes";
quest.questName = "消灭史莱姆";
quest.objectives = new QuestObjective[]
{
    new QuestObjective
    {
        objectiveId = "slime_kill",
        description = "消灭10只史莱姆",
        objectiveType = QuestObjectiveType.KillEnemy,
        targetAmount = 10
    }
};

// 接取任务
QuestManager.Instance.StartQuest("kill_10_slimes");
```

## 🤝 贡献

欢迎提交Issue和Pull Request!

## 📄 许可证

MIT License

## 🙏 致谢

感谢所有为这个项目做出贡献的人!

---

**版本**: 1.0.0
**Unity版本**: 2021.3+
**作者**: Your Name
**最后更新**: 2025-03-22
