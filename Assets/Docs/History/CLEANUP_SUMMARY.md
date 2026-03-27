# 目录清理总结

## 当前结论

本次清理已经完成“数字前缀目录收敛”目标，`Assets/Scripts/` 当前正式目录为：

```text
Framework/
Data/
Gameplay/
UI/
Managers/
Editor/
```

## 本次已完成

- 移除数字前缀目录的使用
- 将 `Framework/` 补齐历史目录中缺失的脚本
- 将 `Gameplay/` 补齐 `Combat/`、`Enemy/`、`Inventory/`
- 将 `UI/`、`Managers/`、`Editor/` 切换为正式目录
- 删除 `UI/` 与 `Editor/` 中遗留的 `.disabled` 占位文件

## 本次未处理

以下旧平铺目录仍在仓库中，需要结合场景、Prefab、资源引用单独评估后再迁移：

```text
Core/
Player/
Enemy/
Items/
Skills/
Quests/
Achievements/
Data/
```

说明：`Data/` 目前仍是正式目录，不是待删除目录。

## 建议的后续清理方式

1. 先在 Unity 中验证当前无数字前缀目录是否全部正常编译
2. 再按系统拆分清理旧平铺目录，而不是一次性全部删除
3. 每迁移一个系统，就同步验证场景和 Prefab 引用

## 相关文档

- `REMOVE_NUMBER_PREFIXES.md`
- `DIRECTORY_STRUCTURE.md`
- `MIGRATION_GUIDE.md`
