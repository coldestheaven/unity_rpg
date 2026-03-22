using UnityEngine;
using UnityEditor;
using System.IO;

namespace Editor.Tools
{
    public class DataGenerator
    {
        [MenuItem("RPG/Generate Default Data")]
        public static void GenerateDefaultData()
        {
            string databasePath = "Assets/Resources/Databases";

            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            // Generate Item Database
            CreateDatabase<Managers.ItemDatabase>(databasePath, "ItemDatabase");

            // Generate Skill Database
            CreateDatabase<Managers.SkillDatabase>(databasePath, "SkillDatabase");

            // Generate Quest Database
            CreateDatabase<Managers.QuestDatabase>(databasePath, "QuestDatabase");

            // Generate Achievement Database
            CreateDatabase<Managers.AchievementDatabase>(databasePath, "AchievementDatabase");

            AssetDatabase.Refresh();
            Debug.Log("Default databases created successfully!");
        }

        private static void CreateDatabase<T>(string path, string fileName) where T : ScriptableObject
        {
            string fullPath = Path.Combine(path, $"{fileName}.asset");

            if (!File.Exists(fullPath))
            {
                T database = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(database, fullPath);
                Debug.Log($"Created {fileName}");
            }
            else
            {
                Debug.Log($"{fileName} already exists");
            }
        }

        [MenuItem("RPG/Clear All Data")]
        public static void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("All player data cleared");
        }
    }
}
