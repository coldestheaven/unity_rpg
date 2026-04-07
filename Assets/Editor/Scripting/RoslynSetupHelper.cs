#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripting
{
    /// <summary>
    /// 一键安装 / 删除 Roslyn 运行时 DLL。
    ///
    /// 菜单：RPG / Scripting / Install Roslyn DLLs
    ///
    /// 流程：
    ///   1. 从 NuGet.org 下载 Microsoft.CodeAnalysis.CSharp nupkg。
    ///   2. 解包（zip）并提取所需 DLL 到 Assets/Plugins/Roslyn/。
    ///   3. 自动设置 .meta（仅 Editor + Standalone，不包含 WebGL / Mobile IL2CPP）。
    ///   4. 追加脚本宏定义  ROSLYN_ENABLED  到当前 Build Target Group。
    ///
    /// 若已安装，菜单显示"Uninstall Roslyn DLLs"，执行清理并移除宏定义。
    /// </summary>
    public static class RoslynSetupHelper
    {
        // ── 配置 ──────────────────────────────────────────────────────────────

        /// <summary>Roslyn 包版本（对应 .NET Standard 2.0 兼容分支，兼容 Unity 2021+）。</summary>
        private const string RoslynVersion = "4.8.0";

        /// <summary>DLL 输出目录（相对于项目根）。</summary>
        private const string PluginDir = "Assets/Plugins/Roslyn";

        private const string ScriptingDefine = "ROSLYN_ENABLED";

        // NuGet 下载 URL 模板
        private static string NugetUrl(string packageId, string version) =>
            $"https://www.nuget.org/api/v2/package/{packageId}/{version}";

        // 需要从各 nupkg 中提取的 DLL 名称及对应包（lib/netstandard2.0/）
        private static readonly (string packageId, string dll)[] RequiredDlls =
        {
            ("Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.dll"),
            ("Microsoft.CodeAnalysis.Common", "Microsoft.CodeAnalysis.dll"),
            ("System.Collections.Immutable",  "System.Collections.Immutable.dll"),
            ("System.Reflection.Metadata",    "System.Reflection.Metadata.dll"),
            ("System.Runtime.CompilerServices.Unsafe",
                                              "System.Runtime.CompilerServices.Unsafe.dll"),
        };

        // ── 菜单 ──────────────────────────────────────────────────────────────

        [MenuItem("RPG/Scripting/Install Roslyn DLLs", priority = 20)]
        public static async void InstallRoslyn()
        {
            if (!EditorUtility.DisplayDialog("安装 Roslyn DLL",
                $"将从 NuGet.org 下载以下包（{RoslynVersion}）并解压到 {PluginDir}/：\n\n" +
                "• Microsoft.CodeAnalysis.CSharp\n" +
                "• Microsoft.CodeAnalysis.Common\n" +
                "• System.Collections.Immutable\n" +
                "• System.Reflection.Metadata\n" +
                "• System.Runtime.CompilerServices.Unsafe\n\n" +
                "完成后会自动添加脚本宏定义 ROSLYN_ENABLED。",
                "安装", "取消"))
                return;

            try
            {
                await DownloadAndExtractDlls();
                AddScriptingDefine();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("安装完成",
                    $"Roslyn DLL 已安装至 {PluginDir}/\n\n" +
                    "已添加脚本宏定义 ROSLYN_ENABLED。\n" +
                    "请等待 Unity 重新编译后使用 RPG/Scripting/C# Console 打开控制台。",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoslynSetup] 安装失败: {e}");
                EditorUtility.DisplayDialog("安装失败",
                    $"下载或解压过程中出现错误：\n{e.Message}\n\n" +
                    "请检查网络连接或手动将 DLL 放入 Assets/Plugins/Roslyn/。",
                    "OK");
            }
        }

        [MenuItem("RPG/Scripting/Uninstall Roslyn DLLs", priority = 21)]
        public static void UninstallRoslyn()
        {
            if (!EditorUtility.DisplayDialog("卸载 Roslyn DLL",
                $"将删除 {PluginDir}/ 目录下的所有文件，并移除 ROSLYN_ENABLED 宏定义。",
                "卸载", "取消"))
                return;

            if (Directory.Exists(PluginDir))
            {
                Directory.Delete(PluginDir, recursive: true);
                string metaFile = PluginDir + ".meta";
                if (File.Exists(metaFile)) File.Delete(metaFile);
            }

            RemoveScriptingDefine();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("卸载完成", "Roslyn DLL 已卸载，宏定义 ROSLYN_ENABLED 已移除。", "OK");
        }

        // ── 下载 + 解压 ───────────────────────────────────────────────────────

        private static async Task DownloadAndExtractDlls()
        {
            Directory.CreateDirectory(PluginDir);
            string tempDir = Path.Combine(Path.GetTempPath(), "RoslynSetup");
            Directory.CreateDirectory(tempDir);

            int total = RequiredDlls.Length;
            for (int i = 0; i < total; i++)
            {
                var (packageId, dllName) = RequiredDlls[i];
                EditorUtility.DisplayProgressBar("安装 Roslyn DLL",
                    $"正在下载 {packageId}…", (float)i / total);

                string nupkgPath = Path.Combine(tempDir, $"{packageId}.nupkg");
                string url       = NugetUrl(packageId, RoslynVersion);

                await DownloadFileAsync(url, nupkgPath);

                // nupkg 本质是 zip
                string extractDir = Path.Combine(tempDir, packageId);
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);

                System.IO.Compression.ZipFile.ExtractToDirectory(nupkgPath, extractDir);

                // 寻找 netstandard2.0 下的目标 DLL
                string found = FindDll(extractDir, dllName);
                if (found == null)
                    throw new FileNotFoundException(
                        $"在 {packageId} 包中未找到 {dllName}（lib/netstandard2.0/）。");

                string dest = Path.Combine(PluginDir, dllName);
                File.Copy(found, dest, overwrite: true);
                Debug.Log($"[RoslynSetup] 已安装: {dllName}");
            }

            EditorUtility.ClearProgressBar();

            // 清理临时目录
            try { Directory.Delete(tempDir, true); }
            catch { /* 临时目录清理失败可忽略 */ }
        }

        private static string FindDll(string root, string dllName)
        {
            // 优先 netstandard2.0，其次 netstandard2.1、net461
            var preferred = new[] { "netstandard2.0", "netstandard2.1", "net461", "net45" };
            foreach (string tfm in preferred)
            {
                string candidate = Path.Combine(root, "lib", tfm, dllName);
                if (File.Exists(candidate)) return candidate;
            }
            // fallback: 递归查找
            foreach (string file in Directory.EnumerateFiles(root, dllName,
                SearchOption.AllDirectories))
                return file;
            return null;
        }

        private static async Task DownloadFileAsync(string url, string destPath)
        {
            using var client = new WebClient();
            var tcs = new TaskCompletionSource<bool>();
            client.DownloadFileCompleted += (_, e) =>
            {
                if (e.Error != null) tcs.TrySetException(e.Error);
                else tcs.TrySetResult(true);
            };
            client.DownloadFileAsync(new Uri(url), destPath);
            await tcs.Task;
        }

        // ── 脚本宏定义管理 ────────────────────────────────────────────────────

        private static void AddScriptingDefine()
        {
            var group  = EditorUserBuildSettings.selectedBuildTargetGroup;
            string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!current.Contains(ScriptingDefine))
            {
                string updated = string.IsNullOrEmpty(current)
                    ? ScriptingDefine
                    : current + ";" + ScriptingDefine;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, updated);
                Debug.Log($"[RoslynSetup] 已添加宏定义 {ScriptingDefine}。");
            }
        }

        private static void RemoveScriptingDefine()
        {
            var group  = EditorUserBuildSettings.selectedBuildTargetGroup;
            string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!current.Contains(ScriptingDefine)) return;

            var defines = new List<string>(current.Split(';'));
            defines.Remove(ScriptingDefine);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group,
                string.Join(";", defines));
            Debug.Log($"[RoslynSetup] 已移除宏定义 {ScriptingDefine}。");
        }

        // ── 安装状态检查（可用于 MenuItem 的 Validate 版本） ──────────────────

        [MenuItem("RPG/Scripting/Install Roslyn DLLs", validate = true)]
        private static bool ValidateInstall()
        {
            // 已安装则禁用"Install"菜单
            return !IsInstalled();
        }

        [MenuItem("RPG/Scripting/Uninstall Roslyn DLLs", validate = true)]
        private static bool ValidateUninstall() => IsInstalled();

        public static bool IsInstalled()
        {
            string csharpDll = Path.Combine(PluginDir, "Microsoft.CodeAnalysis.CSharp.dll");
            return File.Exists(csharpDll);
        }
    }
}
#endif
