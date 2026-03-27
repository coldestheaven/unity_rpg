using UnityEngine;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(SkillData), true)]
    public class SkillDataEditor : Editor
    {
        private SerializedProperty skillNameProp;
        private SerializedProperty skillDescriptionProp;
        private SerializedProperty damageProp;
        private SerializedProperty cooldownProp;
        private SerializedProperty manaCostProp;

        private void OnEnable()
        {
            skillNameProp = serializedObject.FindProperty("skillName");
            skillDescriptionProp = serializedObject.FindProperty("skillDescription");
            damageProp = serializedObject.FindProperty("damage");
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
            EditorGUILayout.PropertyField(skillDescriptionProp, new GUIContent("Description"), true);
        }

        private void DrawCombatStats()
        {
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(damageProp, new GUIContent("Damage"));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Cooldown (s)"));
            EditorGUILayout.PropertyField(manaCostProp, new GUIContent("Mana Cost"));
        }

        private void DrawEffectSettings()
        {
            EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure skill effects here", MessageType.Info);
        }
    }

    // Placeholder SkillData class
    public class SkillData : ScriptableObject
    {
        [SerializeField] private string skillName;
        [SerializeField] [TextArea] private string skillDescription;
        [SerializeField] private int damage;
        [SerializeField] private float cooldown;
        [SerializeField] private int manaCost;

        public string SkillName => skillName;
        public string SkillDescription => skillDescription;
        public int Damage => damage;
        public float Cooldown => cooldown;
        public int ManaCost => manaCost;
    }
}
