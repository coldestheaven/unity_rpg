using UnityEngine;
using UnityEditor;
using RPG.Skills;

namespace RPG.Editor
{
    /// <summary>
    /// 技能数据自定义编辑器
    /// </summary>
    [CustomEditor(typeof(SkillData))]
    public class SkillDataEditor : Editor
    {
        private SerializedProperty skillNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty iconProp;
        private SerializedProperty skillTypeProp;
        private SerializedProperty levelProp;
        private SerializedProperty maxLevelProp;
        private SerializedProperty cooldownProp;
        private SerializedProperty manaCostProp;
        private SerializedProperty baseDamageProp;
        private SerializedProperty damageMultiplierProp;
        private SerializedProperty damageTypeProp;
        private SerializedProperty rangeProp;
        private SerializedProperty areaRadiusProp;
        private SerializedProperty targetTypeProp;
        private SerializedProperty skillEffectPrefabProp;
        private SerializedProperty castSoundProp;
        private SerializedProperty impactEffectProp;
        private SerializedProperty trailEffectProp;
        private SerializedProperty hotkeyProp;

        private bool showBasicSettings = true;
        private bool showDamageSettings = true;
        private bool showRangeSettings = true;
        private bool showEffectSettings = true;
        private bool showUpgradeSettings = true;

        private void OnEnable()
        {
            skillNameProp = serializedObject.FindProperty("skillName");
            descriptionProp = serializedObject.FindProperty("description");
            iconProp = serializedObject.FindProperty("icon");
            skillTypeProp = serializedObject.FindProperty("skillType");
            levelProp = serializedObject.FindProperty("level");
            maxLevelProp = serializedObject.FindProperty("maxLevel");
            cooldownProp = serializedObject.FindProperty("cooldown");
            manaCostProp = serializedObject.FindProperty("manaCost");
            baseDamageProp = serializedObject.FindProperty("baseDamage");
            damageMultiplierProp = serializedObject.FindProperty("damageMultiplier");
            damageTypeProp = serializedObject.FindProperty("damageType");
            rangeProp = serializedObject.FindProperty("range");
            areaRadiusProp = serializedObject.FindProperty("areaRadius");
            targetTypeProp = serializedObject.FindProperty("targetType");
            skillEffectPrefabProp = serializedObject.FindProperty("skillEffectPrefab");
            castSoundProp = serializedObject.FindProperty("castSound");
            impactEffectProp = serializedObject.FindProperty("impactEffect");
            trailEffectProp = serializedObject.FindProperty("trailEffect");
            hotkeyProp = serializedObject.FindProperty("hotkey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SkillData skill = (SkillData)target;

            EditorGUI.BeginChangeCheck();

            // 标题
            DrawHeader();

            // 基本设置
            showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "基本信息", true);
            if (showBasicSettings)
            {
                DrawBasicSettings();
            }

            EditorGUILayout.Space();

            // 伤害设置
            showDamageSettings = EditorGUILayout.Foldout(showDamageSettings, "伤害设置", true);
            if (showDamageSettings)
            {
                DrawDamageSettings();
            }

            EditorGUILayout.Space();

            // 范围设置
            showRangeSettings = EditorGUILayout.Foldout(showRangeSettings, "范围设置", true);
            if (showRangeSettings)
            {
                DrawRangeSettings();
            }

            EditorGUILayout.Space();

            // 效果设置
            showEffectSettings = EditorGUILayout.Foldout(showEffectSettings, "效果设置", true);
            if (showEffectSettings)
            {
                DrawEffectSettings();
            }

            EditorGUILayout.Space();

            // 升级设置
            showUpgradeSettings = EditorGUILayout.Foldout(showUpgradeSettings, "升级设置", true);
            if (showUpgradeSettings)
            {
                DrawUpgradeSettings();
            }

            EditorGUILayout.Space();

            // 实时预览
            DrawPreview(skill);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("技能编辑器", headerStyle);
            EditorGUILayout.HelpBox("配置技能属性、效果和升级参数", MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawBasicSettings()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(skillNameProp, new GUIContent("技能名称"));
            EditorGUILayout.PropertyField(descriptionProp, new GUIContent("描述"));
            EditorGUILayout.PropertyField(iconProp, new GUIContent("图标"));
            EditorGUILayout.PropertyField(skillTypeProp, new GUIContent("技能类型"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("基础属性", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(levelProp, new GUIContent("初始等级"));
            EditorGUILayout.PropertyField(maxLevelProp, new GUIContent("最大等级"));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("冷却时间(秒)"));
            EditorGUILayout.PropertyField(manaCostProp, new GUIContent("法力消耗"));
            EditorGUILayout.PropertyField(hotkeyProp, new GUIContent("快捷键"));

            EditorGUI.indentLevel--;
        }

        private void DrawDamageSettings()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(baseDamageProp, new GUIContent("基础伤害"));
            EditorGUILayout.PropertyField(damageMultiplierProp, new GUIContent("伤害倍率"));
            EditorGUILayout.PropertyField(damageTypeProp, new GUIContent("伤害类型"));

            EditorGUI.indentLevel--;
        }

        private void DrawRangeSettings()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(rangeProp, new GUIContent("施法范围"));
            EditorGUILayout.PropertyField(areaRadiusProp, new GUIContent("效果半径"));
            EditorGUILayout.PropertyField(targetTypeProp, new GUIContent("目标类型"));

            EditorGUI.indentLevel--;
        }

        private void DrawEffectSettings()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(skillEffectPrefabProp, new GUIContent("技能效果预制体"));
            EditorGUILayout.PropertyField(castSoundProp, new GUIContent("施法音效"));
            EditorGUILayout.PropertyField(impactEffectProp, new GUIContent("命中音效"));
            EditorGUILayout.PropertyField(trailEffectProp, new GUIContent("轨迹效果"));

            EditorGUI.indentLevel--;
        }

        private void DrawUpgradeSettings()
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownReductionPerLevel"),
                new GUIContent("每级冷却减少(秒)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damageIncreasePerLevel"),
                new GUIContent("每级伤害增加"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manaCostIncreasePerLevel"),
                new GUIContent("每级法力消耗增加"));

            EditorGUI.indentLevel--;
        }

        private void DrawPreview(SkillData skill)
        {
            EditorGUILayout.LabelField("技能预览", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (skill.icon != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(skill.icon.texture, GUILayout.Width(64), GUILayout.Height(64));
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"名称: {skill.skillName}");
                EditorGUILayout.LabelField($"类型: {skill.skillType}");
                EditorGUILayout.LabelField($"等级: {skill.level}/{skill.maxLevel}");
                EditorGUILayout.LabelField($"冷却: {skill.cooldown}秒");
                EditorGUILayout.LabelField($"伤害: {skill.GetDamage(skill.level)}");
                EditorGUILayout.LabelField($"消耗: {skill.GetManaCost(skill.level)}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("请设置技能图标", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
