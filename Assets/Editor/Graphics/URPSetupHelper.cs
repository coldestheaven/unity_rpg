#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor.Graphics
{
    /// <summary>
    /// RPG URP 渲染扩展一键设置助手。
    ///
    /// 菜单：RPG / Graphics / URP Setup
    ///
    /// 功能：
    ///   • 检测 URP 管线状态（是否已设置为活跃渲染管线）
    ///   • 检测 URP_ENABLED 宏定义是否已添加
    ///   • 检测 RPGOutline.shader / RPGFullscreenFade.shader 是否在 Always Included Shaders
    ///   • 一键添加 URP_ENABLED 宏定义
    ///   • 一键将 RPG Shader 加入 Always Included Shaders（防止 Build 时被剥离）
    ///   • 打印详细诊断报告（用于问题排查）
    /// </summary>
    public sealed class URPSetupWindow : EditorWindow
    {
        [MenuItem("RPG/Graphics/URP Setup", priority = 50)]
        public static void Open()
        {
            var w = GetWindow<URPSetupWindow>("URP Setup");
            w.minSize = new Vector2(480, 400);
            w.Show();
        }

        // ── 诊断状态 ──────────────────────────────────────────────────────────

        private bool _urpIsActive;
        private bool _urpDefineAdded;
        private bool _outlineShaderIncluded;
        private bool _fadeShaderIncluded;
        private bool _analyzed;
        private Vector2 _scroll;

        private static readonly string OutlineShaderPath  = "Assets/Shaders/URP/RPGOutline.shader";
        private static readonly string FadeShaderPath     = "Assets/Shaders/URP/RPGFullscreenFade.shader";
        private const string Define = "URP_ENABLED";

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            GUILayout.Label("RPG URP 渲染扩展 — 设置检查", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此窗口帮助你验证并配置 URP 渲染扩展所需的环境。\n" +
                "点击「分析」后根据结果执行对应的修复操作。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🔍  分析当前项目配置", GUILayout.Height(32)))
                Analyze();

            if (!_analyzed) return;

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawStatus("URP 已设为活跃渲染管线",   _urpIsActive);
            DrawStatus($"脚本宏定义 {Define} 已添加", _urpDefineAdded);
            DrawStatus("RPGOutline.shader 已加入 Always Included",  _outlineShaderIncluded);
            DrawStatus("RPGFullscreenFade.shader 已加入 Always Included", _fadeShaderIncluded);

            EditorGUILayout.Space(8);
            GUILayout.Label("快捷修复", EditorStyles.boldLabel);

            if (!_urpDefineAdded)
            {
                if (GUILayout.Button($"➕  添加宏定义  {Define}"))
                {
                    AddScriptingDefine(Define);
                    Analyze();
                }
            }
            else
            {
                if (GUILayout.Button($"✕  移除宏定义  {Define}（测试用）"))
                {
                    RemoveScriptingDefine(Define);
                    Analyze();
                }
            }

            EditorGUILayout.Space(2);
            if (!_outlineShaderIncluded || !_fadeShaderIncluded)
            {
                if (GUILayout.Button("➕  将 RPG Shader 加入 Always Included Shaders"))
                {
                    AddRPGShadersToAlwaysIncluded();
                    Analyze();
                }
            }

            EditorGUILayout.Space(6);
            GUILayout.Label("手动步骤（无法自动化）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. 在 Project Settings > Graphics > Scriptable Render Pipeline Settings\n" +
                "   中确认已设置 URP Renderer Data Asset（Universal Render Pipeline Asset）。\n\n" +
                "2. 在 UniversalRendererData 的 Renderer Features 列表中添加：\n" +
                "   • Outline Feature（描边，挂在角色/敌人相机 Renderer）\n" +
                "   • Fullscreen Fade Feature（全屏淡化，挂在主相机 Renderer）\n\n" +
                "3. 将 FullscreenFadeController + ScreenEffectBridge 组件\n" +
                "   挂载到 DontDestroyOnLoad 的 GameManager GameObject。\n\n" +
                "4. 在需要描边的 GameObject 挂载 OutlineController 组件，\n" +
                "   并设置 Layer 在 OutlineFeature 的 OutlineLayers 中。",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ── 分析 ──────────────────────────────────────────────────────────────

        private void Analyze()
        {
            _analyzed = true;

            // 1. URP 是否为活跃管线
            _urpIsActive = GraphicsSettings.defaultRenderPipeline != null &&
                           GraphicsSettings.defaultRenderPipeline
                               .GetType().Name.Contains("UniversalRenderPipeline");

            // 2. 宏定义
            var group  = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            _urpDefineAdded = defs.Split(';').Any(d => d.Trim() == Define);

            // 3. Always Included Shaders
            var gfxSettings = UnityEngine.Object.FindObjectOfType<GraphicsSettings>();
            var alwaysIncluded = GetAlwaysIncludedShaders();

            _outlineShaderIncluded = IsShaderIncluded(alwaysIncluded, OutlineShaderPath);
            _fadeShaderIncluded    = IsShaderIncluded(alwaysIncluded, FadeShaderPath);

            Repaint();
        }

        // ── 修复操作 ──────────────────────────────────────────────────────────

        private static void AddScriptingDefine(string define)
        {
            var group  = EditorUserBuildSettings.selectedBuildTargetGroup;
            string cur = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defs   = new List<string>(cur.Split(';'));
            if (!defs.Contains(define))
            {
                defs.Add(define);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group,
                    string.Join(";", defs));
                Debug.Log($"[URPSetup] 已添加宏定义 {define}。");
            }
        }

        private static void RemoveScriptingDefine(string define)
        {
            var group  = EditorUserBuildSettings.selectedBuildTargetGroup;
            string cur = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defs   = new List<string>(cur.Split(';'));
            if (defs.Remove(define))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group,
                    string.Join(";", defs));
                Debug.Log($"[URPSetup] 已移除宏定义 {define}。");
            }
        }

        private static void AddRPGShadersToAlwaysIncluded()
        {
            var so        = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var prop      = so.FindProperty("m_AlwaysIncludedShaders");
            var shaderPaths = new[] { OutlineShaderPath, FadeShaderPath };

            foreach (string path in shaderPaths)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null)
                {
                    Debug.LogWarning($"[URPSetup] 未找到 shader: {path}");
                    continue;
                }

                bool alreadyAdded = false;
                for (int i = 0; i < prop.arraySize; i++)
                {
                    if (prop.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = shader;
                    Debug.Log($"[URPSetup] 已将 {shader.name} 加入 Always Included Shaders。");
                }
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private static List<Shader> GetAlwaysIncludedShaders()
        {
            var so   = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var prop = so.FindProperty("m_AlwaysIncludedShaders");
            var list = new List<Shader>(prop.arraySize);
            for (int i = 0; i < prop.arraySize; i++)
            {
                var s = prop.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (s != null) list.Add(s);
            }
            return list;
        }

        private static bool IsShaderIncluded(List<Shader> included, string assetPath)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            return shader != null && included.Contains(shader);
        }

        private static void DrawStatus(string label, bool ok)
        {
            var color  = ok ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.4f, 0.3f);
            var icon   = ok ? "✅" : "❌";
            var style  = new GUIStyle(EditorStyles.label);
            style.normal.textColor = color;
            EditorGUILayout.LabelField($"{icon}  {label}", style);
        }
    }
}
#endif
