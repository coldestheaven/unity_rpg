using UnityEngine;
using UnityEditor;
using RPG.Skills;

namespace RPG.Editor
{
    /// <summary>
    /// 技能效果组件编辑器
    /// </summary>
    [CustomEditor(typeof(ProjectileEffect))]
    public class ProjectileEffectEditor : Editor
    {
        private SerializedProperty speedProp;
        private SerializedProperty lifetimeProp;
        private SerializedProperty homingProp;
        private SerializedProperty homingStrengthProp;

        private void OnEnable()
        {
            speedProp = serializedObject.FindProperty("speed");
            lifetimeProp = serializedObject.FindProperty("lifetime");
            homingProp = serializedObject.FindProperty("homing");
            homingStrengthProp = serializedObject.FindProperty("homingStrength");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawSettings();
            DrawPreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("投射物效果设置", headerStyle);
            EditorGUILayout.HelpBox("配置投射物飞行和追踪参数", MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(speedProp, new GUIContent("飞行速度"));
            EditorGUILayout.PropertyField(lifetimeProp, new GUIContent("生命周期(秒)"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("追踪设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(homingProp, new GUIContent("启用追踪"));
            if (homingProp.boolValue)
            {
                EditorGUILayout.PropertyField(homingStrengthProp, new GUIContent("追踪强度"));
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            ProjectileEffect effect = (ProjectileEffect)target;
            float travelDistance = effect.speed * effect.lifetime;
            EditorGUILayout.LabelField($"最大射程: {travelDistance:F1} 单位");
        }
    }

    [CustomEditor(typeof(AreaEffect))]
    public class AreaEffectEditor : Editor
    {
        private SerializedProperty durationProp;
        private SerializedProperty tickRateProp;
        private SerializedProperty tickCountProp;

        private void OnEnable()
        {
            durationProp = serializedObject.FindProperty("duration");
            tickRateProp = serializedObject.FindProperty("tickRate");
            tickCountProp = serializedObject.FindProperty("tickCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawSettings();
            DrawPreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("范围效果设置", headerStyle);
            EditorGUILayout.HelpBox("配置持续伤害范围效果", MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(durationProp, new GUIContent("持续时间(秒)"));
            EditorGUILayout.PropertyField(tickRateProp, new GUIContent("伤害间隔(秒)"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"预期伤害次数: {Mathf.FloorToInt(durationProp.floatValue / tickRateProp.floatValue)}");
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            AreaEffect effect = (AreaEffect)target;
            float totalTicks = effect.duration / effect.tickRate;
            EditorGUILayout.LabelField($"总伤害次数: {Mathf.FloorToInt(totalTicks)}");
            EditorGUILayout.LabelField($"每秒伤害次数: {Mathf.RoundToInt(1f / effect.tickRate)}");
        }
    }

    [CustomEditor(typeof(WaveEffect))]
    public class WaveEffectEditor : Editor
    {
        private SerializedProperty expansionSpeedProp;
        private SerializedProperty maxRadiusProp;
        private SerializedProperty damagePerSecondProp;

        private void OnEnable()
        {
            expansionSpeedProp = serializedObject.FindProperty("expansionSpeed");
            maxRadiusProp = serializedObject.FindProperty("maxRadius");
            damagePerSecondProp = serializedObject.FindProperty("damagePerSecond");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawSettings();
            DrawPreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("波浪效果设置", headerStyle);
            EditorGUILayout.HelpBox("配置扩散波浪效果", MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("扩展设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(expansionSpeedProp, new GUIContent("扩展速度"));
            EditorGUILayout.PropertyField(maxRadiusProp, new GUIContent("最大半径"));
            EditorGUILayout.PropertyField(damagePerSecondProp, new GUIContent("每秒伤害"));
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            WaveEffect effect = (WaveEffect)target;
            float duration = effect.maxRadius / effect.expansionSpeed;
            EditorGUILayout.LabelField($"扩散时间: {duration:F2} 秒");
            EditorGUILayout.LabelField($"最大半径: {effect.maxRadius} 单位");
            EditorGUILayout.LabelField($"总伤害: {effect.damagePerSecond * duration:F0}");
        }
    }
}
