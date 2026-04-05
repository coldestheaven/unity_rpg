using UnityEngine;
using UnityEditor;
using RPG.Skills;
using Editor.Windows;

namespace Editor
{
    /// <summary>
    /// 技能数据自定义 Inspector
    ///
    /// 在 Project 面板选中 SkillData 资产时显示。
    /// 提供完整字段编辑 + 等级预览 + 一键打开技能编辑器窗口。
    /// </summary>
    [CustomEditor(typeof(SkillData), true)]
    public class SkillDataEditor : UnityEditor.Editor
    {
        // ── Serialized properties ─────────────────────────────────────────────
        private SerializedProperty _skillName, _description, _icon;
        private SerializedProperty _skillType, _targetType, _hotkey;
        private SerializedProperty _level, _maxLevel;
        private SerializedProperty _damageType, _baseDamage, _damageMultiplier;
        private SerializedProperty _cooldown, _manaCost;
        private SerializedProperty _range, _areaRadius;
        private SerializedProperty _damageInc, _cooldownRed, _manaInc;
        private SerializedProperty _effectPrefab, _impactEffect, _trailEffect, _castSound;
        private SerializedProperty _strategy;
        private SerializedProperty _skillGraph;

        private int _previewLevel = 1;
        private bool _showScaling   = true;
        private bool _showEffects   = true;
        private bool _showStrategy  = true;
        private bool _showGraph     = true;

        private void OnEnable()
        {
            _skillName        = SP("skillName");
            _description      = SP("description");
            _icon             = SP("icon");
            _skillType        = SP("skillType");
            _targetType       = SP("targetType");
            _hotkey           = SP("hotkey");
            _level            = SP("level");
            _maxLevel         = SP("maxLevel");
            _damageType       = SP("damageType");
            _baseDamage       = SP("baseDamage");
            _damageMultiplier = SP("damageMultiplier");
            _cooldown         = SP("cooldown");
            _manaCost         = SP("manaCost");
            _range            = SP("range");
            _areaRadius       = SP("areaRadius");
            _damageInc        = SP("damageIncreasePerLevel");
            _cooldownRed      = SP("cooldownReductionPerLevel");
            _manaInc          = SP("manaCostIncreasePerLevel");
            _effectPrefab     = SP("skillEffectPrefab");
            _impactEffect     = SP("impactEffect");
            _trailEffect      = SP("trailEffect");
            _castSound        = SP("castSound");
            _strategy         = SP("executionStrategy");
            _skillGraph       = SP("skillGraph");

            var skill = (SkillData)target;
            _previewLevel = Mathf.Clamp(skill.level, 1, skill.maxLevel);
        }

        private SerializedProperty SP(string n) => serializedObject.FindProperty(n);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var skill = (SkillData)target;

            DrawOpenWindowButton();
            EditorGUILayout.Space(2);

            DrawBasicInfo(skill);
            DrawCombatStats(skill);

            _showScaling = EditorGUILayout.Foldout(_showScaling, "升级成长 & 各级预览", true);
            if (_showScaling) DrawScaling(skill);

            _showEffects = EditorGUILayout.Foldout(_showEffects, "效果资产", true);
            if (_showEffects) DrawEffects();

            _showStrategy = EditorGUILayout.Foldout(_showStrategy, "执行策略 (Strategy Pattern)", true);
            if (_showStrategy) DrawStrategy();

            _showGraph = EditorGUILayout.Foldout(_showGraph, "节点图 (Graph Pattern)", true);
            if (_showGraph) DrawGraphSection();

            serializedObject.ApplyModifiedProperties();
        }

        // ── Open in window button ─────────────────────────────────────────────
        private void DrawOpenWindowButton()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("技能数据", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("在技能编辑器中打开 →", GUILayout.Width(160), GUILayout.Height(20)))
            {
                SkillEditorWindow.ShowWindow();
                // The window will select this asset via Selection.activeObject
                Selection.activeObject = target;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── Basic info ────────────────────────────────────────────────────────
        private void DrawBasicInfo(SkillData skill)
        {
            SectionHeader("基本信息");

            EditorGUILayout.BeginHorizontal();

            // Icon preview on the left
            EditorGUILayout.BeginVertical(GUILayout.Width(56));
            if (skill.icon != null)
            {
                var tex = AssetPreview.GetAssetPreview(skill.icon);
                if (tex != null)
                    GUILayout.Label(tex, GUILayout.Width(52), GUILayout.Height(52));
            }
            else
            {
                var r = GUILayoutUtility.GetRect(52, 52, GUILayout.Width(52), GUILayout.Height(52));
                EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
            }
            EditorGUILayout.EndVertical();

            // Fields on the right
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_skillName,  new GUIContent("名称"));
            EditorGUILayout.PropertyField(_skillType,  new GUIContent("类型"));
            EditorGUILayout.PropertyField(_targetType, new GUIContent("目标"));
            EditorGUILayout.PropertyField(_hotkey,     new GUIContent("快捷键"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_icon,        new GUIContent("图标"));
            EditorGUILayout.PropertyField(_description, new GUIContent("描述"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_level,    new GUIContent("当前等级"));
            EditorGUILayout.PropertyField(_maxLevel, new GUIContent("最大等级"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        // ── Combat stats ──────────────────────────────────────────────────────
        private void DrawCombatStats(SkillData skill)
        {
            SectionHeader("战斗属性");

            EditorGUILayout.PropertyField(_damageType,       new GUIContent("伤害类型"));
            EditorGUILayout.PropertyField(_baseDamage,       new GUIContent("基础伤害"));
            EditorGUILayout.PropertyField(_damageMultiplier, new GUIContent("伤害倍率"));

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_cooldown, new GUIContent("冷却时间 (s)"));
            EditorGUILayout.PropertyField(_manaCost, new GUIContent("法力消耗"));

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_range,      new GUIContent("施放距离"));
            EditorGUILayout.PropertyField(_areaRadius, new GUIContent("范围半径"));

            // Live preview
            EditorGUILayout.Space(4);
            _previewLevel = EditorGUILayout.IntSlider("预览等级", _previewLevel, 1, skill.maxLevel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                int   dmg  = skill.GetDamage(_previewLevel);
                float cd   = skill.GetCooldown(_previewLevel);
                float mana = skill.GetManaCost(_previewLevel);
                EditorGUILayout.LabelField($"伤害 {dmg}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"冷却 {cd:F1}s", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"法力 {mana:F0}", EditorStyles.boldLabel);
                if (cd > 0f)
                    EditorGUILayout.LabelField($"DPS {dmg / cd:F1}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(4);
        }

        // ── Scaling ───────────────────────────────────────────────────────────
        private void DrawScaling(SkillData skill)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_damageInc,   new GUIContent("每级伤害增量"));
            EditorGUILayout.PropertyField(_cooldownRed, new GUIContent("每级冷却减少"));
            EditorGUILayout.PropertyField(_manaInc,     new GUIContent("每级法力增量"));

            EditorGUILayout.Space(4);

            // Mini table
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("等级",    EditorStyles.miniButtonMid, GUILayout.Width(36));
            GUILayout.Label("伤害",    EditorStyles.miniButtonMid, GUILayout.Width(52));
            GUILayout.Label("冷却",    EditorStyles.miniButtonMid, GUILayout.Width(52));
            GUILayout.Label("法力",    EditorStyles.miniButtonMid, GUILayout.Width(52));
            EditorGUILayout.EndHorizontal();

            int max  = Mathf.Min(skill.maxLevel, 10);
            int step = skill.maxLevel <= 10 ? 1 : skill.maxLevel <= 20 ? 2 : 5;

            for (int lv = 1; lv <= skill.maxLevel; lv += step)
            {
                bool isCur = lv == _previewLevel;
                var prevBg = GUI.backgroundColor;
                if (isCur) GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.4f);

                EditorGUILayout.BeginHorizontal(isCur ? EditorStyles.helpBox : GUIStyle.none);
                GUI.backgroundColor = prevBg;

                var st = isCur ? EditorStyles.boldLabel : EditorStyles.label;
                GUILayout.Label($"Lv {lv}",
                    st, GUILayout.Width(36));
                GUILayout.Label(skill.GetDamage(lv).ToString(),
                    st, GUILayout.Width(52));
                GUILayout.Label(skill.GetCooldown(lv).ToString("F1"),
                    st, GUILayout.Width(52));
                GUILayout.Label(skill.GetManaCost(lv).ToString("F0"),
                    st, GUILayout.Width(52));
                EditorGUILayout.EndHorizontal();
            }

            if (skill.maxLevel > 10)
                EditorGUILayout.LabelField(
                    $"（以 {step} 为步长显示，共 {skill.maxLevel} 级）",
                    EditorStyles.centeredGreyMiniLabel);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ── Effects ───────────────────────────────────────────────────────────
        private void DrawEffects()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_effectPrefab, new GUIContent("技能效果 Prefab"));
            EditorGUILayout.PropertyField(_impactEffect, new GUIContent("命中效果 Prefab"));
            EditorGUILayout.PropertyField(_trailEffect,  new GUIContent("轨迹效果 Prefab"));
            EditorGUILayout.PropertyField(_castSound,    new GUIContent("施放音效"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ── Strategy ──────────────────────────────────────────────────────────
        private void DrawStrategy()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_strategy, new GUIContent("执行策略 SO"));

            if (_strategy.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    $"当前策略类型: {_strategy.objectReferenceValue.GetType().Name}\n" +
                    "详细参数请在技能编辑器窗口的「执行策略」Tab 中编辑。",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "未设置策略 — 回退到 SkillController 的 legacy switch。\n" +
                    "点击「在技能编辑器中打开 →」可一键创建策略资产。",
                    MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ── Graph section ─────────────────────────────────────────────────────
        private void DrawGraphSection()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_skillGraph, new GUIContent("技能节点图"));

            var graphAsset = _skillGraph.objectReferenceValue as RPG.Skills.Graph.SkillGraph;

            if (graphAsset != null)
            {
                EditorGUILayout.HelpBox(
                    $"节点数: {graphAsset.nodes.Count}  |  连线数: {graphAsset.connections.Count}\n" +
                    "节点图优先于「执行策略」执行。",
                    MessageType.None);

                if (GUILayout.Button("在节点图编辑器中打开 →"))
                    SkillGraphEditorWindow.OpenGraph(graphAsset);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "未设置节点图。赋值后将使用节点图驱动技能逻辑，优先级最高。\n" +
                    "通过 RPG → 技能节点图编辑器 → 新建节点图 创建资产。",
                    MessageType.Info);

                if (GUILayout.Button("创建并绑定新节点图"))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "新建技能节点图",
                        ((SkillData)target).name + "_Graph",
                        "asset",
                        "保存节点图资产",
                        "Assets/Gameplay/Skills/Graphs");

                    if (!string.IsNullOrEmpty(path))
                    {
                        var graph = CreateInstance<RPG.Skills.Graph.SkillGraph>();
                        var entry = (RPG.Skills.Graph.SkillNode)
                            System.Activator.CreateInstance(typeof(RPG.Skills.Graph.Nodes.OnCastNode));
                        entry.editorPosition = new UnityEngine.Vector2(100, 150);
                        graph.AddNode(entry);

                        AssetDatabase.CreateAsset(graph, path);
                        AssetDatabase.SaveAssets();

                        serializedObject.FindProperty("skillGraph").objectReferenceValue = graph;
                        serializedObject.ApplyModifiedProperties();

                        SkillGraphEditorWindow.OpenGraph(graph);
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static void SectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var r = GUILayoutUtility.GetLastRect();
            r.y += r.height; r.height = 1f;
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}
