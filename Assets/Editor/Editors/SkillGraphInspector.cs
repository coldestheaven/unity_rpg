using UnityEditor;
using UnityEngine;
using RPG.Skills.Graph;

/// <summary>
/// SkillGraph 资产的自定义 Inspector。
/// 显示节点 / 连线统计，并提供"在节点图编辑器中打开"快捷按钮。
/// </summary>
[CustomEditor(typeof(SkillGraph))]
public sealed class SkillGraphInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var graph = (SkillGraph)target;

        // ── Open button ────────────────────────────────────────────────────
        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 36
        };

        GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
        if (GUILayout.Button("在节点图编辑器中打开 →", btnStyle))
            SkillGraphEditorWindow.OpenGraph(graph);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("图摘要", EditorStyles.boldLabel);

        // ── Stats ──────────────────────────────────────────────────────────
        using (new EditorGUI.IndentLevelScope(1))
        {
            EditorGUILayout.LabelField("节点数量",     graph.nodes.Count.ToString());
            EditorGUILayout.LabelField("连线数量",     graph.connections.Count.ToString());

            bool hasEntry = graph.GetFirstNode<RPG.Skills.Graph.Nodes.OnCastNode>() != null;
            EditorGUILayout.LabelField("有入口节点",   hasEntry ? "✓ OnCastNode" : "✗ 缺少 OnCastNode");
        }

        EditorGUILayout.Space(4);

        // ── Node list ──────────────────────────────────────────────────────
        if (graph.nodes.Count > 0)
        {
            EditorGUILayout.LabelField("节点列表", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                foreach (var node in graph.nodes)
                {
                    string label = node != null
                        ? $"[{node.GetDisplayName()}]  id:{node.nodeId}"
                        : "(null)";
                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(4);

        // ── Validate ───────────────────────────────────────────────────────
        if (GUILayout.Button("验证并清理悬空连线"))
        {
            int removed = graph.Validate();
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SkillGraph] 已清理 {removed} 条悬空连线。");
        }
    }
}
