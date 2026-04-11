using UnityEngine;
using UnityEditor;

namespace RPGEditorTools
{
    [CustomEditor(typeof(Gameplay.Player.PlayerController))]
    public class PlayerControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty movementProp;
        private SerializedProperty combatProp;
        private SerializedProperty healthProp;
        private SerializedProperty inputProp;
        private SerializedProperty animatorProp;

        private void OnEnable()
        {
            movementProp = serializedObject.FindProperty("movement");
            combatProp = serializedObject.FindProperty("combat");
            healthProp = serializedObject.FindProperty("health");
            inputProp = serializedObject.FindProperty("input");
            animatorProp = serializedObject.FindProperty("animator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Controller", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(movementProp, new GUIContent("Movement"));
            EditorGUILayout.PropertyField(combatProp, new GUIContent("Combat"));
            EditorGUILayout.PropertyField(healthProp, new GUIContent("Health"));
            EditorGUILayout.PropertyField(inputProp, new GUIContent("Input"));
            EditorGUILayout.PropertyField(animatorProp, new GUIContent("Animator"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var controller = (Gameplay.Player.PlayerController)target;
            EditorGUILayout.LabelField($"Is Grounded: {controller.IsGrounded}");
            EditorGUILayout.LabelField($"Is Alive: {controller.IsAlive}");
            EditorGUILayout.LabelField($"Is Attacking: {controller.IsAttacking}");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
