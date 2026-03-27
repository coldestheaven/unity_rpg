# 目录命名说明

## 当前命名规范

`Assets/Scripts/` 目录统一使用语义化命名和 PascalCase：

```text
Framework/
Data/
Gameplay/
UI/
Managers/
Editor/
```

## 规则

- 使用有业务含义的英文目录名
- 不再使用数字前缀表示层级顺序
- 目录名采用 PascalCase
- 命名空间可以与目录语义保持一致，但不强制要求和物理目录完全同名

## 当前状态

- 数字前缀目录已经完成收敛
- `Framework/` 与 `Gameplay/` 为合并后的有效目录
- `UI/`、`Managers/`、`Editor/` 已完成正式命名切换
- `Data/` 保持现有命名

## 迁移原则

- 优先保留当前可编译、命名空间兼容的实现
- 对仅存在于数字前缀目录中的脚本进行补齐迁移
- 对明显废弃的 `.disabled` 占位文件直接清理
- 旧的平铺业务目录单独评估，不和“去数字前缀”混为一次操作

## 参考文档

- `DIRECTORY_STRUCTURE.md`：当前目录结构
- `MIGRATION_GUIDE.md`：本次迁移说明
