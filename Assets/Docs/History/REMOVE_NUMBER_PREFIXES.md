# 移除数字前缀目录

## 当前状态

`Assets/Scripts/` 下的数字前缀目录已经完成清理，当前正式目录为：

```text
Framework/
Gameplay/
UI/
Managers/
Editor/
```

`Data/` 继续沿用原有命名，不使用数字前缀。

## 本次处理原则

- 保留现有无前缀目录中更兼容的实现
- 将仅存在于数字前缀目录中的有效脚本补充迁移到正式目录
- 清理无前缀目录中废弃的 `.disabled` 占位文件
- 不在本次操作中删除旧的平铺业务目录

## 迁移结果

- `Framework/` 补齐了对象池和事件委托相关脚本
- `Gameplay/` 补齐了 `Combat/`、`Enemy/`、`Inventory/` 子目录
- `UI/` 切换为新的 `Base/`、`Controllers/`、`Views/` 结构
- `Managers/` 已成为正式管理器目录
- `Editor/` 已成为正式 Unity 编辑器目录

## 验证建议

- 在 Unity 中重新导入脚本并观察 Console
- 检查 `Framework`、`Gameplay`、`UI`、`Managers`、`Editor` 目录是否正常显示
- 确认不再存在数字前缀目录
- 确认 `Editor` 目录中的编辑器脚本仅在编辑器环境下编译

## 说明

如果后续需要继续清理 `Core/`、`Items/`、`Skills/`、`Quests/`、`Achievements/` 等旧目录，应作为单独的业务迁移任务处理，而不是混在本次目录命名重构中。旧 `Player/` 目录已迁移到 `Assets/Scripts/Legacy/Player/`，旧 `Enemy/` 目录已迁移到 `Assets/Scripts/Legacy/Enemy/`，作为兼容实现保留。
