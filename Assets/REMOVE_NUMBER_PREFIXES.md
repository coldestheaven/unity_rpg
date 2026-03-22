# 移除数字前缀目录 - 操作指南

## 📋 当前状态

项目中存在以下带数字前缀的目录需要重命名：

- `01_Framework/` → `Framework/`
- `03_Gameplay/` → `Gameplay/`
- `04_UI/` → `UI/`
- `05_Managers/` → `Managers/`
- `06_Editor/` → `Editor/`

## 🎯 推荐操作步骤

### 方法1: 在Unity中手动重命名（最简单）

1. **关闭Unity项目**

2. **在文件管理器中重命名目录**
   - 打开 `d:/Unity/unity_rpg/unity_rpg/Assets/Scripts/`
   - 重命名以下目录：
     ```
     01_Framework/   → Framework/
     03_Gameplay/    → Gameplay/
     04_UI/          → UI/
     05_Managers/    → Managers/
     06_Editor/      → Editor/
     ```

3. **删除重复的旧目录**
   - 删除以下旧目录（如果存在）：
     ```
     Core/
     Enemy/
     Player/
     Items/
     Quests/
     Skills/
     Achievements/
     Data/
     ```

4. **重新打开Unity项目**
   - Unity会自动重新导入所有脚本
   - 等待导入完成

5. **验证项目**
   - 检查控制台是否有错误
   - 确认所有脚本正常编译

6. **提交到Git**
   ```bash
   cd d:/Unity/unity_rpg/unity_rpg
   git add .
   git commit -m "refactor: remove number prefixes from directory names"
   git push
   ```

### 方法2: 使用Git命令（推荐开发者）

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
git commit -m "refactor: remove number prefixes from directory names"
git push
```

## ✅ 预期结果

重命名后的目录结构：

```
Assets/Scripts/
├── Framework/              # 框架层
├── Gameplay/               # 游戏逻辑层
├── UI/                     # UI层
├── Managers/               # 管理器层
└── Editor/                 # Editor工具
```

## ⚠️ 注意事项

1. **Unity项目关闭**: 重命名目录前必须关闭Unity，否则Unity可能无法正确识别文件
2. **备份**: 建议在操作前备份项目
3. **Git跟踪**: 使用`git mv`命令可以让Git跟踪重命名操作
4. **脚本引用**: Unity会自动处理脚本引用的更新
5. **编译检查**: 重命名后检查所有脚本是否正常编译

## 🔍 验证清单

- [ ] 所有数字前缀的目录已重命名
- [ ] 旧的重复目录已删除
- [ ] Unity项目正常打开
- [ ] 控制台无错误
- [ ] 所有脚本正常编译
- [ ] Git提交成功

## 📝 常见问题

### Q: Unity提示"Missing Script"怎么办？
A: 这通常是暂时的，等待Unity重新导入完成后应该自动解决。如果问题持续，检查脚本的命名空间是否正确。

### Q: Git无法识别重命名怎么办？
A: 确保使用`git mv`而不是直接重命名，这样Git会跟踪重命名操作。

### Q: 有哪些目录需要保留？
A: 只需要保留新重命名的目录（Framework, Gameplay, UI, Managers, Editor），其他旧的目录都可以删除。

## 📚 相关文档

- `DIRECTORY_STRUCTURE.md` - 详细的目录结构说明
- `MIGRATION_GUIDE.md` - 迁移指南

## 🎉 完成标志

操作完成后，项目应该具有：
- ✅ 清晰的语义化目录名称
- ✅ 无数字前缀
- ✅ 统一的命名规范
- ✅ 正常运行的Unity项目
