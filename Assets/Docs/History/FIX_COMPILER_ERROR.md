# 修复编译错误：删除临时目录

## 问题原因

编译错误 `CS0111: Type 'PlayerController' already defines a member called 'Heal'` 是因为存在多个临时的 `*_temp` 目录，导致同一个类被编译了两次。

## 需要删除的目录

请手动删除以下目录（使用文件资源管理器）：

```
Assets/Scripts/Framework/Framework_temp/
Assets/Scripts/Gameplay/Gameplay_temp/
Assets/Scripts/UI/UI_temp/
Assets/Editor/Editor_temp/
```

## 操作步骤

### 方法 1: 使用文件资源管理器（推荐）

1. 在 Windows 文件资源管理器中打开项目文件夹：
   ```
   d:\Unity\unity_rpg\unity_rpg\Assets\Scripts\
   ```

2. 逐个找到并删除以下文件夹：
   - `Framework/Framework_temp/`
   - `Gameplay/Gameplay_temp/`
   - `UI/UI_temp/`
   - `Editor/Editor_temp/`

3. 返回 Unity，Unity 会自动重新编译项目

### 方法 2: 使用清理脚本

双击运行项目根目录下的 `cleanup_temp_dirs.bat` 文件

### 方法 3: 使用 Git 命令

```bash
cd d:/Unity/unity_rpg/unity_rpg

# 添加到 .gitignore
echo "**/*_temp/" >> .gitignore

# 提交更改
git add .gitignore
git commit -m "chore: ignore temp directories"
```

## 验证

删除后，重新打开 Unity 或等待 Unity 自动重新编译。错误应该消失。

## 预防措施

添加以下内容到项目的 `.gitignore` 文件：

```gitignore
# Unity temp directories
**/*_temp/
**/Temp/
**/Library/
```

## 原因说明

这些 `*_temp` 目录是在之前的 Git 重命名操作（git mv）中创建的中间目录。由于某些原因，重命名没有完全成功，导致新旧两个目录同时存在，从而引起类重复定义的编译错误。

---

**创建日期**: 2026-03-22
