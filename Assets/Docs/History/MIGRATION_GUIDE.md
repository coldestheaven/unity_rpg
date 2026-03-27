# 目录迁移指南

## 迁移目标

将带数字前缀的脚本目录收敛到语义化命名目录，并保持现有命名空间与可编译代码优先。

## 目录映射

| 历史目录 | 当前目录 | 处理方式 |
|---------|---------|---------|
| `01_Framework/` | `Framework/` | 合并缺失脚本后删除历史目录 |
| `03_Gameplay/` | `Gameplay/` | 合并缺失子目录后删除历史目录 |
| `04_UI/` | `UI/` | 正式替换为当前 UI 目录 |
| `05_Managers/` | `Managers/` | 直接迁移 |
| `06_Editor/` | `Editor/` | 直接迁移 |
| `02_Data/` | `Data/` | 本仓库未采用，保持现状 |

## 已完成内容

### Framework

- 保留了当前 `Framework/` 中更兼容的接口与基类实现
- 合并了此前仅存在于历史目录中的 `ObjectPool.cs`
- 合并了此前仅存在于历史目录中的 `EventDelegates.cs`

### Gameplay

- 保留了当前 `Gameplay/Player/` 下更兼容的玩家实现
- 合并了 `Combat/`
- 合并了 `Enemy/`
- 合并了 `Inventory/`

### UI / Managers / Editor

- `UI/` 已切换为 `Base/`、`Controllers/`、`Views/` 结构
- `Managers/` 已作为正式目录启用
- `Editor/` 已作为正式 Unity 编辑器目录启用
- 旧的 `.disabled` 占位文件已清理

## 注意事项

- 本次迁移只处理“数字前缀目录命名”问题
- `Data/` 仍是当前正式数据目录，不应按旧文档误删
- `Core/`、`Items/`、`Skills/`、`Quests/`、`Achievements/` 等旧平铺目录仍需单独评估
- 旧 `Player/` 目录已迁移到 `Assets/Scripts/Legacy/Player/`，作为兼容实现保留
- 旧 `Enemy/` 目录已迁移到 `Assets/Scripts/Legacy/Enemy/`，作为兼容实现保留
- 如果在 Unity 本地工程中存在 `.meta` 文件，目录迁移后应让 Unity 完成一次重新导入

## 验证建议

1. 打开 Unity 并等待脚本重新导入完成
2. 确认 `Assets/Scripts/` 下不再存在数字前缀目录
3. 检查 `UI/`、`Managers/`、`Editor/` 是否按新结构正常编译
4. 检查 `Framework/` 和 `Gameplay/` 新增脚本是否被正常识别
