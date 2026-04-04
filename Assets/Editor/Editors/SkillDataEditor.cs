using UnityEngine;
using UnityEditor;
using RPG.Skills;

namespace Editor
{
    [CustomEditor(typeof(SkillData), true)]
    public class SkillDataEditor : UnityEditor.Editor
    {
        private SerializedProperty skillNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty baseDamageProp;
        private SerializedProperty cooldownProp;
        private SerializedProperty manaCostProp;

        private void OnEnable()
        {
            skillNameProp = serializedObject.FindProperty("skillName");
            descriptionProp = serializedObject.FindProperty("description");
            baseDamageProp = serializedObject.FindProperty("baseDamage");
            cooldownProp = serializedObject.FindProperty("cooldown");
            manaCostProp = serializedObject.FindProperty("manaCost");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBasicInfo();
            EditorGUILayout.Space();
            DrawCombatStats();
            EditorGUILayout.Space();
            DrawEffectSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBasicInfo()
        {
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillNameProp, new GUIContent("Skill Name"));
            EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description"), true);
        }

        private void DrawCombatStats()
        {
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(baseDamageProp, new GUIContent("Base Damage"));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Cooldown (s)"));
            EditorGUILayout.PropertyField(manaCostProp, new GUIContent("Mana Cost"));
        }

        private void DrawEffectSettings()
        {
            EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure skill effects here", MessageType.Info);
        }
    }
}
