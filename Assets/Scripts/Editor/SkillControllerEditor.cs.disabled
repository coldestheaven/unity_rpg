using UnityEngine;
using UnityEditor;
using RPG.Skills;

namespace RPG.Editor
{
    /// <summary>
    /// 技能控制器自定义编辑器
    /// </summary>
    [CustomEditor(typeof(SkillController))]
    [CanEditMultipleObjects]
    public class SkillControllerEditor : Editor
    {
        private SerializedProperty skillSlotsProp;

        private void OnEnable()
        {
            skillSlotsProp = serializedObject.FindProperty("skillSlots");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SkillController controller = (SkillController)target;

            DrawHeader();
            DrawSkillSlots(controller);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("技能控制器", headerStyle);
            EditorGUILayout.Space();
        }

        private void DrawSkillSlots(SkillController controller)
        {
            EditorGUILayout.LabelField("技能槽位", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("配置4个技能槽位的技能数据", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.indentLevel++;

            for (int i = 0; i < 4; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"槽位 {i + 1}", EditorStyles.boldLabel);

                SerializedProperty slotProp = skillSlotsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(slotProp, new GUIContent("技能数据"));

                SkillData skill = slotProp.objectReferenceValue as SkillData;
                if (skill != null)
                {
                    EditorGUILayout.Space();
                    DrawSkillPreview(skill, i);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSkillPreview(SkillData skill, int slotIndex)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("技能信息:");
            EditorGUILayout.LabelField($"  名称: {skill.skillName}");
            EditorGUILayout.LabelField($"  类型: {skill.skillType}");
            EditorGUILayout.LabelField($"  等级: {skill.level}");
            EditorGUILayout.LabelField($"  冷却: {skill.cooldown}秒");
            EditorGUILayout.LabelField($"  伤害: {skill.baseDamage}");
            EditorGUILayout.LabelField($"  消耗: {skill.manaCost}");

            if (skill.icon != null)
            {
                GUILayout.Box(skill.icon.texture, GUILayout.Width(32), GUILayout.Height(32));
            }

            EditorGUI.indentLevel--;
        }
    }
}
