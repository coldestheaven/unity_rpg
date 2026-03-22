using UnityEngine;
using UnityEditor;
using System.Linq;
using RPG.Skills;

namespace RPG.Editor
{
    /// <summary>
    /// 技能数据库编辑器窗口
    /// </summary>
    public class SkillDatabaseEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private SkillType selectedTypeFilter = SkillType.Active;
        private SkillData[] allSkills;
        private SkillData[] filteredSkills;

        [MenuItem("RPG/Skill Database")]
        public static void ShowWindow()
        {
            GetWindow<SkillDatabaseEditor>("技能数据库");
        }

        private void OnGUI()
        {
            // 标题
            GUILayout.Label("技能数据库管理", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 搜索栏
            DrawSearchBar();

            EditorGUILayout.Space();

            // 列表
            DrawSkillList();

            EditorGUILayout.Space();

            // 操作按钮
            DrawActionButtons();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("搜索:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            GUILayout.Label("类型筛选:", GUILayout.Width(60));
            selectedTypeFilter = (SkillType)EditorGUILayout.EnumPopup(selectedTypeFilter, EditorStyles.toolbarPopup, GUILayout.Width(100));

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                LoadSkills();
            }

            EditorGUILayout.EndHorizontal();

            // 应用筛选
            ApplyFilter();
        }

        private void DrawSkillList()
        {
            if (filteredSkills == null || filteredSkills.Length == 0)
            {
                EditorGUILayout.HelpBox("没有找到技能", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (SkillData skill in filteredSkills)
            {
                DrawSkillItem(skill);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSkillItem(SkillData skill)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 图标
            if (skill.icon != null)
            {
                GUILayout.Box(skill.icon.texture, GUILayout.Width(48), GUILayout.Height(48));
            }
            else
            {
                GUILayout.Box(GUIContent.none, GUILayout.Width(48), GUILayout.Height(48));
            }

            // 信息
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(skill.skillName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"类型: {skill.skillType}");
            EditorGUILayout.LabelField($"等级: {skill.level}");
            EditorGUILayout.LabelField($"冷却: {skill.cooldown}秒 | 伤害: {skill.baseDamage}");
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 按钮
            if (GUILayout.Button("编辑", GUILayout.Width(60)))
            {
                Selection.activeObject = skill;
            }

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(skill);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建新技能", GUILayout.Height(30)))
            {
                CreateNewSkill();
            }

            if (GUILayout.Button("导入技能", GUILayout.Height(30)))
            {
                ImportSkills();
            }

            if (GUILayout.Button("导出技能", GUILayout.Height(30)))
            {
                ExportSkills();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void LoadSkills()
        {
            // 从Resources加载所有技能
            allSkills = Resources.LoadAll<SkillData>("Skills");
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (allSkills == null)
            {
                LoadSkills();
                return;
            }

            filteredSkills = allSkills.Where(skill =>
            {
                // 搜索过滤
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) ||
                    skill.skillName.ToLower().Contains(searchFilter.ToLower());

                // 类型过滤
                bool matchesType = skill.skillType == selectedTypeFilter;

                return matchesSearch && matchesType;
            }).ToArray();
        }

        private void CreateNewSkill()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建新技能",
                "New Skill",
                "asset",
                "选择保存位置"
            );

            if (!string.IsNullOrEmpty(path))
            {
                SkillData newSkill = ScriptableObject.CreateInstance<SkillData>();
                newSkill.skillName = System.IO.Path.GetFileNameWithoutExtension(path);
                newSkill.level = 1;
                newSkill.maxLevel = 10;
                newSkill.cooldown = 10f;
                newSkill.manaCost = 20f;
                newSkill.baseDamage = 10;
                newSkill.damageType = DamageType.Physical;

                AssetDatabase.CreateAsset(newSkill, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = newSkill;
                LoadSkills();
            }
        }

        private void ImportSkills()
        {
            string path = EditorUtility.OpenFilePanel(
                "导入技能",
                "",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                // TODO: 实现导入逻辑
                Debug.Log("导入功能待实现");
            }
        }

        private void ExportSkills()
        {
            string path = EditorUtility.SaveFilePanel(
                "导出技能",
                "",
                "Skills",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                // TODO: 实现导出逻辑
                Debug.Log("导出功能待实现");
            }
        }

        private void OnEnable()
        {
            LoadSkills();
        }
    }
}
