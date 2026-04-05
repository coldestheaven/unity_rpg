using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RPG.Skills;

namespace Editor.Windows
{
    /// <summary>
    /// 技能编辑器 — RPG/技能编辑器
    ///
    /// 布局:
    ///   左栏  — 技能资产列表（搜索 + 过滤 + 创建）
    ///   右栏  — 分 Tab 的详细编辑面板
    ///     Tab 0  基本信息   名称 / 描述 / 图标 / 类型 / 快捷键
    ///     Tab 1  战斗属性   伤害 / 冷却 / 法力 / 范围 + 当前等级实时预览
    ///     Tab 2  升级曲线   成长参数 + 各级数值对照表
    ///     Tab 3  效果资产   Prefab / 音效 / 贴图预览
    ///     Tab 4  执行策略   Strategy SO 内联编辑 + 一键创建快捷按钮
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        // ── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("RPG/技能编辑器 %#s")]
        public static void ShowWindow()
        {
            var win = GetWindow<SkillEditorWindow>();
            win.titleContent = new GUIContent("技能编辑器", EditorGUIUtility.IconContent("d_AnimatorStateMachine Icon").image);
            win.minSize = new Vector2(760f, 480f);
            win.Show();
        }

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<SkillData> _skills = new List<SkillData>();
        private SkillData _selected;
        private SerializedObject _so;

        private string _search = "";
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private int _activeTab;
        private int _previewLevel = 1;

        // ── Styles (initialised once) ─────────────────────────────────────────
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _tableHeaderStyle;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;
        private GUIStyle _rowSelectedStyle;
        private Texture2D _selectionTex;
        private Texture2D _evenTex;
        private bool _stylesReady;

        private const float ListWidth = 210f;
        private const string DefaultDir = "Assets/Data/Skills";
        private static readonly string[] Tabs =
            { "基本信息", "战斗属性", "升级曲线", "效果资产", "执行策略" };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void OnEnable() => Reload();
        private void OnProjectChange() => Reload();

        private void Reload()
        {
            _skills.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:SkillData"))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) _skills.Add(s);
            }
            _skills.Sort((a, b) =>
                string.Compare(a.skillName, b.skillName,
                    System.StringComparison.OrdinalIgnoreCase));

            // Rehydrate selection after domain reload
            if (_selected != null)
                _so = new SerializedObject(_selected);
        }

        // ── Styles ────────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 0, 0, 0)
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) }
            };

            _tableHeaderStyle = new GUIStyle(EditorStyles.miniButtonMid)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _selectionTex = MakeTex(new Color(0.24f, 0.49f, 0.91f, 0.45f));
            _evenTex      = MakeTex(new Color(0.5f, 0.5f, 0.5f, 0.05f));

            _rowSelectedStyle = new GUIStyle(GUIStyle.none)
            {
                normal = { background = _selectionTex },
                padding = new RectOffset(6, 4, 3, 3)
            };
            _rowEvenStyle = new GUIStyle(GUIStyle.none)
            {
                normal = { background = _evenTex },
                padding = new RectOffset(6, 4, 3, 3)
            };
            _rowOddStyle = new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(6, 4, 3, 3)
            };
        }

        private static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawListPanel();
            DrawSeparator();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("  RPG 技能编辑器", GUILayout.Width(130));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("＋ 新建技能", EditorStyles.toolbarButton, GUILayout.Width(80)))
                CreateSkill();

            if (GUILayout.Button("⟳", EditorStyles.toolbarButton, GUILayout.Width(28)))
                Reload();

            EditorGUILayout.EndHorizontal();
        }

        // ── Left panel: skill list ────────────────────────────────────────────
        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(_search) &&
                GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _search = "";
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));

            bool alt = false;
            foreach (var skill in _skills)
            {
                if (skill == null) continue;
                if (!MatchesSearch(skill)) continue;

                bool isSelected = skill == _selected;
                var rowStyle = isSelected ? _rowSelectedStyle
                             : alt        ? _rowEvenStyle
                                          : _rowOddStyle;

                EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(22));

                // Skill type colour dot
                DrawTypeDot(skill.skillType);

                // Name button
                var nameLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
                };
                if (GUILayout.Button(
                    string.IsNullOrEmpty(skill.skillName) ? "(未命名)" : skill.skillName,
                    nameLabelStyle, GUILayout.ExpandWidth(true)))
                {
                    Select(skill);
                    GUIUtility.keyboardControl = 0;
                }

                EditorGUILayout.EndHorizontal();
                alt = !alt;
            }

            EditorGUILayout.EndScrollView();

            // Footer
            EditorGUILayout.LabelField(
                $"共 {_skills.Count} 个技能",
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Height(18));

            EditorGUILayout.EndVertical();
        }

        private static void DrawTypeDot(SkillType type)
        {
            Color col = type switch
            {
                SkillType.Ultimate => new Color(1f, 0.55f, 0f),
                SkillType.Passive  => new Color(0.35f, 0.9f, 0.35f),
                SkillType.Toggle   => new Color(0.4f, 0.8f, 1f),
                _                  => new Color(0.85f, 0.85f, 0.85f)
            };
            var r = GUILayoutUtility.GetRect(8f, 22f, GUILayout.Width(10));
            r.x += 1; r.y += 7; r.width = 7; r.height = 7;
            EditorGUI.DrawRect(r, col);
        }

        private bool MatchesSearch(SkillData s)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            string q = _search.ToLowerInvariant();
            return (s.skillName ?? "").ToLowerInvariant().Contains(q) ||
                   s.skillType.ToString().ToLowerInvariant().Contains(q) ||
                   s.damageType.ToString().ToLowerInvariant().Contains(q);
        }

        // ── Separator ─────────────────────────────────────────────────────────
        private static void DrawSeparator()
        {
            var r = GUILayoutUtility.GetRect(1f, 1f,
                GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.1f, 0.6f));
        }

        // ── Right panel: detail ───────────────────────────────────────────────
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selected == null || _so == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("← 请从左侧选择一个技能",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            // Guard: asset deleted externally
            if (!_so.targetObject)
            {
                _selected = null; _so = null;
                EditorGUILayout.EndVertical();
                return;
            }

            _so.Update();

            DrawDetailHeader();

            EditorGUILayout.Space(2);
            _activeTab = GUILayout.Toolbar(_activeTab, Tabs, EditorStyles.toolbarButton);
            EditorGUILayout.Space(4);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            switch (_activeTab)
            {
                case 0: DrawTabInfo();     break;
                case 1: DrawTabCombat();   break;
                case 2: DrawTabScaling();  break;
                case 3: DrawTabEffects();  break;
                case 4: DrawTabStrategy(); break;
            }

            EditorGUILayout.EndScrollView();

            if (_so.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selected);

            EditorGUILayout.EndVertical();
        }

        // ── Detail header ─────────────────────────────────────────────────────
        private void DrawDetailHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(54));

            // Icon
            if (_selected.icon != null)
            {
                var tex = AssetPreview.GetAssetPreview(_selected.icon);
                if (tex != null)
                    GUILayout.Label(tex, GUILayout.Width(48), GUILayout.Height(48));
                else
                    GUILayout.Space(52);
            }
            else
            {
                // Placeholder box
                var r = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                EditorGUI.DrawRect(r, new Color(0.25f, 0.25f, 0.25f));
                GUI.Label(r, "?", new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    normal = { textColor = Color.gray }
                });
            }

            EditorGUILayout.BeginVertical();
            string name = string.IsNullOrEmpty(_selected.skillName) ? "(未命名)" : _selected.skillName;
            GUILayout.Label(name, _titleStyle, GUILayout.Height(24));

            // Type badge row
            Color prevColor = GUI.color;
            GUI.color = SkillTypeColor(_selected.skillType) * 1.3f;
            GUILayout.Label(
                $"  {_selected.skillType}   {_selected.targetType}   {_selected.damageType}   Lv {_selected.level}/{_selected.maxLevel}",
                EditorStyles.miniLabel);
            GUI.color = prevColor;
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Buttons
            if (GUILayout.Button("定位", GUILayout.Width(40), GUILayout.Height(22)))
                EditorGUIUtility.PingObject(_selected);

            if (GUILayout.Button("复制", GUILayout.Width(40), GUILayout.Height(22)))
                DuplicateSkill();

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.42f, 0.42f);
            if (GUILayout.Button("删除", GUILayout.Width(40), GUILayout.Height(22)))
                TryDeleteSelected();
            GUI.backgroundColor = prevBg;

            EditorGUILayout.EndHorizontal();
        }

        private static Color SkillTypeColor(SkillType t) => t switch
        {
            SkillType.Ultimate => new Color(1f, 0.55f, 0f),
            SkillType.Passive  => new Color(0.3f, 0.9f, 0.3f),
            SkillType.Toggle   => new Color(0.3f, 0.75f, 1f),
            _                  => Color.white
        };

        // ── Tab 0: 基本信息 ───────────────────────────────────────────────────
        private void DrawTabInfo()
        {
            Section("基本信息");
            Prop("skillName",   "技能名称");
            Prop("description", "描述");
            Prop("icon",        "图标");
            Prop("skillType",   "技能类型");
            Prop("targetType",  "目标类型");
            Prop("hotkey",      "快捷键");

            Space();
            Section("等级配置");
            Prop("level",    "当前等级");
            Prop("maxLevel", "最大等级");
        }

        // ── Tab 1: 战斗属性 ───────────────────────────────────────────────────
        private void DrawTabCombat()
        {
            Section("伤害");
            Prop("damageType",       "伤害类型");
            Prop("baseDamage",       "基础伤害");
            Prop("damageMultiplier", "伤害倍率");

            Space();
            Section("消耗 & 冷却");
            Prop("cooldown",  "冷却时间 (s)");
            Prop("manaCost",  "法力消耗");

            Space();
            Section("范围");
            Prop("range",      "施放距离");
            Prop("areaRadius", "范围半径");

            Space();
            Section("当前等级实时预览");
            _previewLevel = EditorGUILayout.IntSlider("预览等级", _previewLevel, 1, _selected.maxLevel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            int   dmg  = _selected.GetDamage(_previewLevel);
            float cd   = _selected.GetCooldown(_previewLevel);
            float mana = _selected.GetManaCost(_previewLevel);
            float dps  = cd > 0f ? dmg / cd : 0f;

            EditorGUILayout.BeginHorizontal();
            StatBadge("伤害",    dmg.ToString(),    new Color(1f, 0.55f, 0.35f));
            StatBadge("冷却",    $"{cd:F1}s",       new Color(0.4f, 0.7f, 1f));
            StatBadge("法力",    mana.ToString("F0"), new Color(0.5f, 0.8f, 0.5f));
            StatBadge("DPS",    dps.ToString("F1"), new Color(1f, 0.85f, 0.3f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void StatBadge(string label, string value, Color col)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(80));
            var prevColor = GUI.color;
            GUI.color = col;
            GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            }, GUILayout.Height(26));
            GUI.color = prevColor;
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── Tab 2: 升级曲线 ───────────────────────────────────────────────────
        private void DrawTabScaling()
        {
            Section("升级成长参数");
            Prop("damageIncreasePerLevel",    "每级伤害增量");
            Prop("cooldownReductionPerLevel", "每级冷却减少 (s)");
            Prop("manaCostIncreasePerLevel",  "每级法力增量");

            Space();
            Section("各级数值对照表");

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            Col("等级",    48, _tableHeaderStyle);
            Col("伤害",    68, _tableHeaderStyle);
            Col("冷却(s)", 72, _tableHeaderStyle);
            Col("法力",    60, _tableHeaderStyle);
            Col("DPS",    70, _tableHeaderStyle);
            EditorGUILayout.EndHorizontal();

            int maxRows = Mathf.Min(_selected.maxLevel, 20);
            int step    = maxRows <= 10 ? 1 : 2;

            for (int lv = 1; lv <= _selected.maxLevel; lv += step)
            {
                int   dmg  = _selected.GetDamage(lv);
                float cd   = _selected.GetCooldown(lv);
                float mana = _selected.GetManaCost(lv);
                float dps  = cd > 0f ? dmg / cd : 0f;
                bool  sel  = lv == _previewLevel;

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = sel
                    ? new Color(0.25f, 0.5f, 1f, 0.4f)
                    : (lv % 2 == 0 ? new Color(1f, 1f, 1f, 0.03f) : Color.clear);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(20));
                GUI.backgroundColor = prevBg;

                var cellStyle = sel
                    ? new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter }
                    : new GUIStyle(EditorStyles.label)     { alignment = TextAnchor.MiddleCenter };

                Col($"Lv {lv}",          48, cellStyle);
                Col(dmg.ToString(),       68, cellStyle);
                Col(cd.ToString("F1"),    72, cellStyle);
                Col(mana.ToString("F0"),  60, cellStyle);
                Col(dps.ToString("F1"),   70, cellStyle);
                EditorGUILayout.EndHorizontal();
            }

            if (_selected.maxLevel > 20)
                EditorGUILayout.LabelField(
                    $"（已截断，共 {_selected.maxLevel} 级）",
                    EditorStyles.centeredGreyMiniLabel);
        }

        private static void Col(string text, float width, GUIStyle style)
            => GUILayout.Label(text, style, GUILayout.Width(width));

        // ── Tab 3: 效果资产 ───────────────────────────────────────────────────
        private void DrawTabEffects()
        {
            Section("视觉效果");
            Prop("skillEffectPrefab", "技能效果 Prefab");
            Prop("impactEffect",      "命中效果 Prefab");
            Prop("trailEffect",       "轨迹效果 Prefab");

            Space();
            Section("音频");
            Prop("castSound", "施放音效");

            // Asset thumbnails
            bool hasAny = _selected.skillEffectPrefab || _selected.castSound ||
                          _selected.impactEffect || _selected.trailEffect || _selected.icon;
            if (!hasAny) return;

            Space();
            Section("预览");
            EditorGUILayout.BeginHorizontal();

            AssetThumb(_selected.icon,              "图标");
            AssetThumb(_selected.skillEffectPrefab, "效果");
            AssetThumb(_selected.impactEffect,      "命中");
            AssetThumb(_selected.trailEffect,       "轨迹");

            if (_selected.castSound != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(72));
                GUILayout.Label("♪", new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 24, alignment = TextAnchor.MiddleCenter
                }, GUILayout.Width(64), GUILayout.Height(54));
                GUILayout.Label(_selected.castSound.name,
                    EditorStyles.centeredGreyMiniLabel, GUILayout.Width(64));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void AssetThumb(Object asset, string label)
        {
            if (asset == null) return;
            var tex = AssetPreview.GetAssetPreview(asset);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(72));
            if (tex != null)
                GUILayout.Label(tex, GUILayout.Width(64), GUILayout.Height(54));
            else
            {
                var r = GUILayoutUtility.GetRect(64, 54, GUILayout.Width(64), GUILayout.Height(54));
                EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
            }
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(64));
            EditorGUILayout.EndVertical();
        }

        // ── Tab 4: 执行策略 ───────────────────────────────────────────────────
        private void DrawTabStrategy()
        {
            Section("Strategy Pattern");
            EditorGUILayout.HelpBox(
                "指定一个 SkillExecutionStrategy ScriptableObject。\n" +
                "留空则退回到 SkillController 的 legacy switch。\n" +
                "内置策略: Projectile / Area / Melee / Buff",
                MessageType.Info);
            Space();

            Prop("executionStrategy", "执行策略");

            var stratProp = _so.FindProperty("executionStrategy");
            if (stratProp.objectReferenceValue != null)
            {
                Space();
                Section("策略参数（内联编辑）");

                var stratSO = new SerializedObject(stratProp.objectReferenceValue);
                stratSO.Update();

                var iter = stratSO.GetIterator();
                bool enterChildren = true;
                while (iter.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iter.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(iter, true);
                }

                if (stratSO.ApplyModifiedProperties())
                    EditorUtility.SetDirty(stratProp.objectReferenceValue);

                Space();
                if (GUILayout.Button("清除策略（回退到 legacy）", GUILayout.Height(24)))
                {
                    stratProp.objectReferenceValue = null;
                    _so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_selected);
                }
            }
            else
            {
                Space();
                Section("一键创建策略资产");
                EditorGUILayout.BeginHorizontal();
                QuickStrategy<ProjectileSkillStrategy>("弹射物", stratProp);
                QuickStrategy<AreaSkillStrategy>       ("范围",   stratProp);
                QuickStrategy<MeleeSkillStrategy>      ("近战",   stratProp);
                QuickStrategy<BuffSkillStrategy>       ("增益",   stratProp);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void QuickStrategy<T>(string label, SerializedProperty stratProp)
            where T : SkillExecutionStrategy
        {
            if (!GUILayout.Button($"＋ {label}", GUILayout.Height(30))) return;

            string skillPath = AssetDatabase.GetAssetPath(_selected);
            string dir = string.IsNullOrEmpty(skillPath) ? DefaultDir : Path.GetDirectoryName(skillPath);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{dir}/{typeof(T).Name}_{_selected.name}.asset");

            EnsureDirectory(dir);
            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            stratProp.objectReferenceValue = asset;
            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selected);
            EditorGUIUtility.PingObject(asset);
        }

        // ── Common helpers ────────────────────────────────────────────────────
        private void Prop(string propName, string displayName)
        {
            var p = _so.FindProperty(propName);
            if (p != null)
                EditorGUILayout.PropertyField(p, new GUIContent(displayName), true);
        }

        private void Section(string title)
        {
            EditorGUILayout.LabelField(title, _sectionStyle);
            var r = GUILayoutUtility.GetLastRect();
            r.y += r.height; r.height = 1f;
            EditorGUI.DrawRect(r, new Color(0.4f, 0.6f, 0.9f, 0.4f));
            EditorGUILayout.Space(3);
        }

        private static void Space() => EditorGUILayout.Space(8);

        // ── Asset operations ──────────────────────────────────────────────────
        private void Select(SkillData skill)
        {
            _selected = skill;
            _so = new SerializedObject(skill);
            _previewLevel = Mathf.Clamp(skill.level, 1, skill.maxLevel);
            Selection.activeObject = skill;
        }

        private void CreateSkill()
        {
            EnsureDirectory(DefaultDir);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultDir}/NewSkill.asset");
            var skill = CreateInstance<SkillData>();
            skill.skillName = "New Skill";
            skill.level     = 1;
            skill.maxLevel  = 10;
            skill.baseDamage = 10;
            skill.cooldown  = 5f;
            skill.manaCost  = 20f;
            AssetDatabase.CreateAsset(skill, path);
            AssetDatabase.SaveAssets();
            Reload();
            Select(skill);
        }

        private void DuplicateSkill()
        {
            string srcPath = AssetDatabase.GetAssetPath(_selected);
            string dir     = Path.GetDirectoryName(srcPath);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{dir}/{_selected.name}_Copy.asset");
            AssetDatabase.CopyAsset(srcPath, newPath);
            AssetDatabase.SaveAssets();
            Reload();
            Select(AssetDatabase.LoadAssetAtPath<SkillData>(newPath));
        }

        private void TryDeleteSelected()
        {
            if (!EditorUtility.DisplayDialog("删除技能",
                    $"确认删除「{_selected.skillName}」？此操作不可撤销。",
                    "删除", "取消")) return;

            string path = AssetDatabase.GetAssetPath(_selected);
            _selected = null;
            _so = null;
            AssetDatabase.DeleteAsset(path);
            Reload();
        }

        private static void EnsureDirectory(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                string folder = Path.GetFileName(dir);
                if (!AssetDatabase.IsValidFolder(parent))
                    Directory.CreateDirectory(
                        Path.Combine(Application.dataPath, parent.Substring("Assets/".Length)));
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
