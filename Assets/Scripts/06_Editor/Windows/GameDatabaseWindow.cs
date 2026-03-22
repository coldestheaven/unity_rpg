using UnityEngine;
using UnityEditor;

namespace Editor.Windows
{
    public class GameDatabaseWindow : EditorWindow
    {
        [MenuItem("RPG/Game Database")]
        public static void ShowWindow()
        {
            GetWindow<GameDatabaseWindow>("Game Database");
        }

        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private readonly string[] tabs = { "Items", "Skills", "Quests", "Achievements" };

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawItemsTab();
                    break;
                case 1:
                    DrawSkillsTab();
                    break;
                case 2:
                    DrawQuestsTab();
                    break;
                case 3:
                    DrawAchievementsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawItemsTab()
        {
            EditorGUILayout.LabelField("Items Database", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Configure game items here.", MessageType.Info);
        }

        private void DrawSkillsTab()
        {
            EditorGUILayout.LabelField("Skills Database", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Configure game skills here.", MessageType.Info);
        }

        private void DrawQuestsTab()
        {
            EditorGUILayout.LabelField("Quests Database", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Configure game quests here.", MessageType.Info);
        }

        private void DrawAchievementsTab()
        {
            EditorGUILayout.LabelField("Achievements Database", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Configure game achievements here.", MessageType.Info);
        }
    }
}
