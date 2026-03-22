# 项目文档索引

本目录包含Unity RPG项目的所有文档。

## 📚 文档列表

### 架构与重构
- **REFACTORING_GUIDE.md** - 详细的重构指南，包括新架构说明和迁移步骤
- **DIRECTORY_STRUCTURE.md** - 新的目录结构说明和命名规范

### 迁移与清理
- **MIGRATION_GUIDE.md** - 从旧架构到新架构的完整迁移指南
- **DIRECTORY_NAMING.md** - 目录命名说明和推荐方案
- **REMOVE_NUMBER_PREFIXES.md** - 移除数字前缀的详细操作指南
- **CLEANUP_SUMMARY.md** - 目录清理总结和检查清单

## 🎯 快速导航

**如果你需要：**
- 了解项目架构 → 阅读 `REFACTORING_GUIDE.md`
- 查看目录结构 → 阅读 `DIRECTORY_STRUCTURE.md`
- 迁移到新架构 → 阅读 `MIGRATION_GUIDE.md`
- 移除数字前缀 → 阅读 `REMOVE_NUMBER_PREFIXES.md`
- 清理旧目录 → 阅读 `CLEANUP_SUMMARY.md`

## 📂 项目结构概览

```
Assets/
├── Scripts/               # 代码目录
│   ├── Framework/         # 框架层
│   ├── Gameplay/          # 游戏逻辑层
│   ├── UI/                # UI层
│   ├── Managers/          # 管理器层
│   └── Editor/            # Editor工具
├── Docs/                  # 📖 项目文档（本目录）
└── README.md              # 项目主README
```

## 🔗 相关链接

- **Unity官方文档**: https://docs.unity3d.com/
- **Git版本控制**: 使用Git进行版本管理和协作
- **命名规范**: 参见 `DIRECTORY_NAMING.md`

---

**最后更新**: 2026-03-22
