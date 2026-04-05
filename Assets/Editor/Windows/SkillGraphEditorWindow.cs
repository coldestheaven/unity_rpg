using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RPG.Skills.Graph;

/// <summary>
/// 技能节点图编辑器
///
/// 菜单: RPG / 技能节点图编辑器  (Ctrl+Shift+G)
///
/// 功能:
///   • 可视化节点画布：拖拽移动、滚轮缩放、中键平移
///   • 左侧面板：反射发现的所有 [SkillNodeType] 节点，按分类折叠
///   • 右键菜单：在画布任意位置创建节点
///   • 端口连线拖拽：输出端口 → 输入端口
///   • 节点内联字段编辑（[NodeField] 反射渲染）
///   • Delete/Backspace 删除选中节点
///   • Ctrl+Z/Y Undo/Redo 完整支持
/// </summary>
public sealed class SkillGraphEditorWindow : EditorWindow
{
    // ── Menu ─────────────────────────────────────────────────────────────────

    [MenuItem("RPG/技能节点图编辑器 %#g")]
    public static void Open() => GetWindow<SkillGraphEditorWindow>("技能节点图");

    public static void OpenGraph(SkillGraph graph)
    {
        var win = GetWindow<SkillGraphEditorWindow>("技能节点图");
        win.LoadGraph(graph);
    }

    // ── Constants ────────────────────────────────────────────────────────────

    private const float PaletteWidth   = 180f;
    private const float ToolbarHeight  = 24f;
    private const float NodeWidth      = 210f;
    private const float NodeHeaderH    = 24f;
    private const float PortRowH       = 20f;
    private const float FieldRowH      = 20f;
    private const float PortRadius     = 5f;
    private const float PortHitRadius  = 8f;
    private const float MinZoom        = 0.4f;
    private const float MaxZoom        = 2.0f;
    private const float GridSpaceSmall = 20f;
    private const float GridSpaceLarge = 100f;

    // ── State ─────────────────────────────────────────────────────────────────

    private SkillGraph _graph;
    private Vector2 _panOffset     = new Vector2(200f, 100f);
    private float   _zoom          = 1f;
    private string  _selectedId;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartNodePos;
    private bool    _isDraggingNode;
    private bool    _isPanning;

    // Connection dragging
    private bool   _isConnecting;
    private string _connFromNodeId;
    private string _connFromPortId;
    private bool   _connFromIsOutput;

    // Cached reflection data
    private static List<(Type type, SkillNodeTypeAttribute attr)> _nodeTypes;
    private Dictionary<string, bool> _categoryFolded = new Dictionary<string, bool>();
    private Vector2 _paletteScroll;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        wantsMouseMove = true;
        RefreshNodeTypes();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LoadGraph(SkillGraph graph)
    {
        _graph      = graph;
        _selectedId = null;
        Repaint();
    }

    // ── Main GUI ──────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawToolbar();
        DrawPalette();

        Rect canvasRect = new Rect(PaletteWidth, ToolbarHeight,
                                   position.width - PaletteWidth,
                                   position.height - ToolbarHeight);

        DrawCanvas(canvasRect);
        HandleGlobalKeys();
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        GUILayout.BeginArea(new Rect(0, 0, position.width, ToolbarHeight));
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("打开图资产", EditorStyles.toolbarButton, GUILayout.Width(80)))
            PickGraph();

        if (_graph != null && GUILayout.Button("新建节点图", EditorStyles.toolbarButton, GUILayout.Width(80)))
            CreateNewGraph();

        GUILayout.Space(8);

        if (_graph != null)
        {
            GUILayout.Label(_graph.name, EditorStyles.toolbarButton);
            GUILayout.Space(8);
        }

        GUILayout.FlexibleSpace();

        if (_graph != null && GUILayout.Button("⟳ 刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            _graph.Validate();
            EditorUtility.SetDirty(_graph);
            Repaint();
        }

        if (GUILayout.Button("⟲ 重置视图", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            _panOffset = new Vector2(200f, 100f);
            _zoom = 1f;
            Repaint();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ── Palette ───────────────────────────────────────────────────────────────

    private void DrawPalette()
    {
        Rect paletteRect = new Rect(0, ToolbarHeight, PaletteWidth, position.height - ToolbarHeight);

        GUILayout.BeginArea(paletteRect);

        // Header
        EditorGUI.DrawRect(new Rect(0, 0, PaletteWidth, 22), new Color(0.18f, 0.18f, 0.18f));
        GUI.Label(new Rect(6, 3, PaletteWidth - 12, 18),
                  "节点面板", EditorStyles.boldLabel);

        GUILayout.Space(24);

        _paletteScroll = GUILayout.BeginScrollView(_paletteScroll);

        if (_nodeTypes != null)
        {
            var byCategory = _nodeTypes.GroupBy(t => t.attr.Category)
                                        .OrderBy(g => g.Key);

            foreach (var group in byCategory)
            {
                string cat = group.Key;
                if (!_categoryFolded.ContainsKey(cat)) _categoryFolded[cat] = false;

                _categoryFolded[cat] = !EditorGUILayout.Foldout(!_categoryFolded[cat], cat, true,
                                                                  EditorStyles.foldoutHeader);
                if (!_categoryFolded[cat])
                {
                    foreach (var (type, attr) in group)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        Color headerColor = ParseHex(attr.HexColor);
                        var origColor = GUI.backgroundColor;
                        GUI.backgroundColor = headerColor * 1.5f;

                        GUIContent btnContent = new GUIContent(attr.DisplayName, attr.Tooltip);
                        if (GUILayout.Button(btnContent, GUILayout.Height(22)))
                        {
                            if (_graph != null)
                                CreateNodeOfType(type, _panOffset + new Vector2(200, 200) / _zoom * -1 + new Vector2(300, 200));
                        }

                        GUI.backgroundColor = origColor;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);
                    }
                }
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // Separator
        EditorGUI.DrawRect(new Rect(PaletteWidth - 1, ToolbarHeight, 1, position.height - ToolbarHeight),
                           new Color(0.12f, 0.12f, 0.12f));
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private void DrawCanvas(Rect canvasRect)
    {
        // Background
        EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.15f));

        // Clipped area for nodes / grid
        GUI.BeginClip(canvasRect);
        DrawGrid(canvasRect.size);

        if (_graph != null)
        {
            // Draw node bodies (without ports / connections yet)
            foreach (var node in _graph.nodes)
                DrawNode(node);
        }

        GUI.EndClip();

        // Bezier connections drawn in full-window space (Handles API)
        if (_graph != null)
        {
            Handles.BeginGUI();
            foreach (var conn in _graph.connections)
                DrawConnection(conn, canvasRect);

            if (_isConnecting)
                DrawActiveConnectionLine(canvasRect, Event.current.mousePosition);

            Handles.EndGUI();
        }

        HandleCanvasEvents(canvasRect);
    }

    // ── Grid ──────────────────────────────────────────────────────────────────

    private void DrawGrid(Vector2 size)
    {
        float small = GridSpaceSmall * _zoom;
        float large = GridSpaceLarge * _zoom;

        DrawGridLines(size, small, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        DrawGridLines(size, large, new Color(0.15f, 0.15f, 0.15f, 0.8f));
    }

    private void DrawGridLines(Vector2 size, float spacing, Color color)
    {
        Handles.color = color;
        float offsetX = _panOffset.x % spacing;
        float offsetY = _panOffset.y % spacing;

        for (float x = offsetX; x < size.x; x += spacing)
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, size.y));
        for (float y = offsetY; y < size.y; y += spacing)
            Handles.DrawLine(new Vector3(0, y), new Vector3(size.x, y));
    }

    // ── Node rendering ────────────────────────────────────────────────────────

    private void DrawNode(SkillNode node)
    {
        var attr    = node.GetNodeTypeAttribute();
        var color   = attr != null ? ParseHex(attr.HexColor) : new Color(0.3f, 0.5f, 0.7f);
        bool selected = node.nodeId == _selectedId;

        Rect nodeRect = GetNodeCanvasRect(node);

        // Shadow
        EditorGUI.DrawRect(new Rect(nodeRect.x + 3, nodeRect.y + 3, nodeRect.width, nodeRect.height),
                           new Color(0, 0, 0, 0.4f));

        // Body
        EditorGUI.DrawRect(nodeRect, selected
            ? new Color(0.27f, 0.27f, 0.27f)
            : new Color(0.22f, 0.22f, 0.22f));

        // Header
        Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, NodeHeaderH);
        EditorGUI.DrawRect(headerRect, color * (selected ? 1.3f : 1f));

        // Selection border
        if (selected)
        {
            DrawBorder(nodeRect, new Color(0.9f, 0.7f, 0.2f), 2f);
        }
        else
        {
            DrawBorder(nodeRect, new Color(0.1f, 0.1f, 0.1f), 1f);
        }

        // Node title
        GUI.Label(new Rect(headerRect.x + 8, headerRect.y + 4, headerRect.width - 24, 18),
                  node.GetDisplayName(),
                  new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, fontSize = 11 });

        // Delete button (×)
        if (GUI.Button(new Rect(headerRect.xMax - 18, headerRect.y + 4, 14, 14), "×",
                       new GUIStyle(EditorStyles.miniLabel)
                       { normal = { textColor = new Color(1, 0.5f, 0.5f) }, alignment = TextAnchor.MiddleCenter }))
        {
            DeleteNode(node.nodeId);
            return;
        }

        // Ports
        DrawNodePorts(node, nodeRect);

        // Fields
        DrawNodeFields(node, nodeRect);
    }

    private void DrawNodePorts(SkillNode node, Rect nodeRect)
    {
        var inputs  = node.GetInputPorts();
        var outputs = node.GetOutputPorts();
        float portY = nodeRect.y + NodeHeaderH;

        int portRows = Mathf.Max(inputs.Length, outputs.Length);

        for (int i = 0; i < portRows; i++)
        {
            float rowY = portY + i * PortRowH + PortRowH * 0.5f;

            // Input port
            if (i < inputs.Length)
            {
                var port = inputs[i];
                Vector2 portCenter = new Vector2(nodeRect.xMin, rowY);
                DrawPort(portCenter, port, false);

                // Label (right of port circle)
                GUI.Label(new Rect(nodeRect.xMin + 10, rowY - 9, 90, 18),
                          port.Label,
                          new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } });
            }

            // Output port
            if (i < outputs.Length)
            {
                var port = outputs[i];
                Vector2 portCenter = new Vector2(nodeRect.xMax, rowY);
                DrawPort(portCenter, port, true);

                // Label (left of port circle)
                GUI.Label(new Rect(nodeRect.xMax - 100, rowY - 9, 90, 18),
                          port.Label,
                          new GUIStyle(EditorStyles.miniLabel)
                          {
                              normal   = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                              alignment = TextAnchor.MiddleRight
                          });
            }
        }
    }

    private void DrawPort(Vector2 center, NodePortDefinition port, bool isOutput)
    {
        Color portColor = GetPortColor(port.DataType);

        // Check if connected
        bool connected = _graph != null &&
                         _graph.IsPortConnected(_graph.nodes
                                                      .FirstOrDefault(n => PortBelongsTo(n, port.Id, isOutput))
                                                      ?.nodeId ?? "", port.Id, isOutput);

        Rect pRect = new Rect(center.x - PortRadius, center.y - PortRadius,
                              PortRadius * 2, PortRadius * 2);
        EditorGUI.DrawRect(pRect, connected ? portColor : portColor * 0.5f);
        DrawBorder(pRect, portColor * 1.5f, 1f);
    }

    private bool PortBelongsTo(SkillNode node, string portId, bool isOutput)
    {
        if (node == null) return false;
        var ports = isOutput ? node.GetOutputPorts() : node.GetInputPorts();
        return ports.Any(p => p.Id == portId);
    }

    // ── Inline field editor (reflection) ─────────────────────────────────────

    private void DrawNodeFields(SkillNode node, Rect nodeRect)
    {
        int portRows = Mathf.Max(node.GetInputPorts().Length, node.GetOutputPorts().Length);
        float fieldsStartY = nodeRect.y + NodeHeaderH + portRows * PortRowH + 4;

        var fields = GetNodeFields(node.GetType());

        foreach (var (fi, attr) in fields)
        {
            Rect labelRect = new Rect(nodeRect.x + 6, fieldsStartY, 80 * _zoom, FieldRowH);
            Rect valueRect = new Rect(nodeRect.x + 88, fieldsStartY,
                                      nodeRect.width - 94, FieldRowH - 2);

            string label = attr.Label ?? ObjectNames.NicifyVariableName(fi.Name);
            GUI.Label(labelRect,
                      TruncateLabel(label, (int)(80 * _zoom / 6.5f)),
                      new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } });

            object currentVal = fi.GetValue(node);
            EditorGUI.BeginChangeCheck();
            object newVal = DrawFieldControl(valueRect, fi.FieldType, currentVal, attr);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_graph, $"Edit {label}");
                fi.SetValue(node, newVal);
                EditorUtility.SetDirty(_graph);
            }

            fieldsStartY += FieldRowH + 2;
        }
    }

    private object DrawFieldControl(Rect rect, Type type, object value, NodeFieldAttribute attr)
    {
        if (type == typeof(float))
        {
            float v = value is float f ? f : 0f;
            return attr.HasRange
                ? EditorGUI.Slider(rect, v, attr.Min, attr.Max)
                : EditorGUI.FloatField(rect, v, EditorStyles.miniTextField);
        }
        if (type == typeof(int))
        {
            int v = value is int i ? i : 0;
            return attr.HasRange
                ? (int)EditorGUI.Slider(rect, v, attr.Min, attr.Max)
                : EditorGUI.IntField(rect, v, EditorStyles.miniTextField);
        }
        if (type == typeof(bool))
            return EditorGUI.Toggle(rect, value is bool b && b);
        if (type == typeof(string))
            return EditorGUI.TextField(rect, value as string ?? "", EditorStyles.miniTextField);
        if (type == typeof(Vector2))
        {
            EditorGUIUtility.labelWidth = 12;
            return EditorGUI.Vector2Field(rect, GUIContent.none, value is Vector2 v2 ? v2 : Vector2.zero);
        }
        if (type.IsEnum)
            return EditorGUI.EnumPopup(rect, value as Enum ?? (Enum)Enum.GetValues(type).GetValue(0));
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return EditorGUI.ObjectField(rect, value as UnityEngine.Object, type, false);

        GUI.Label(rect, $"({type.Name})", EditorStyles.miniLabel);
        return value;
    }

    // ── Bezier connections ────────────────────────────────────────────────────

    private void DrawConnection(NodeConnection conn, Rect canvasRect)
    {
        var fromNode = _graph.GetNode(conn.fromNodeId);
        var toNode   = _graph.GetNode(conn.toNodeId);
        if (fromNode == null || toNode == null) return;

        Vector2 fromPos = GetOutputPortWindowPos(fromNode, conn.fromPortId, canvasRect);
        Vector2 toPos   = GetInputPortWindowPos(toNode,   conn.toPortId,   canvasRect);

        DrawBezierCurve(fromPos, toPos, new Color(0.7f, 0.8f, 1f, 0.85f));

        // Click-to-delete midpoint
        Vector2 mid = (fromPos + toPos) * 0.5f;
        Rect delRect = new Rect(mid.x - 8, mid.y - 8, 16, 16);
        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 1 &&
            delRect.Contains(Event.current.mousePosition))
        {
            Undo.RecordObject(_graph, "Delete Connection");
            _graph.Disconnect(conn.fromNodeId, conn.fromPortId, conn.toNodeId, conn.toPortId);
            EditorUtility.SetDirty(_graph);
            Event.current.Use();
        }
    }

    private void DrawActiveConnectionLine(Rect canvasRect, Vector2 mousePosWindow)
    {
        var fromNode = _graph.GetNode(_connFromNodeId);
        if (fromNode == null) return;

        Vector2 start = _connFromIsOutput
            ? GetOutputPortWindowPos(fromNode, _connFromPortId, canvasRect)
            : GetInputPortWindowPos(fromNode, _connFromPortId, canvasRect);

        DrawBezierCurve(start, mousePosWindow, new Color(1f, 0.8f, 0.3f, 0.8f));
    }

    private static void DrawBezierCurve(Vector2 start, Vector2 end, Color color)
    {
        float dx = Mathf.Abs(end.x - start.x);
        float tangentStrength = Mathf.Max(dx * 0.5f, 50f);
        Vector3 startTan = new Vector3(start.x + tangentStrength, start.y, 0);
        Vector3 endTan   = new Vector3(end.x   - tangentStrength, end.y,   0);
        Handles.DrawBezier(start, end, startTan, endTan, color, null, 2.5f);
    }

    // ── Event handling ────────────────────────────────────────────────────────

    private void HandleCanvasEvents(Rect canvasRect)
    {
        Event e = Event.current;
        if (!canvasRect.Contains(e.mousePosition) && e.type != EventType.MouseUp) return;

        // Convert mouse to canvas-local space
        Vector2 canvasMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
        Vector2 graphMouse  = (canvasMouse - _panOffset) / _zoom;

        switch (e.type)
        {
            case EventType.MouseDown:
                HandleMouseDown(e, canvasMouse, graphMouse, canvasRect);
                break;
            case EventType.MouseDrag:
                HandleMouseDrag(e, canvasMouse);
                break;
            case EventType.MouseUp:
                HandleMouseUp(e, canvasMouse, graphMouse, canvasRect);
                break;
            case EventType.ScrollWheel:
                HandleScroll(e, canvasMouse);
                break;
            case EventType.ContextClick:
                if (canvasRect.Contains(e.mousePosition))
                    ShowContextMenu(graphMouse);
                break;
            case EventType.MouseMove:
                if (_isConnecting) Repaint();
                break;
        }
    }

    private void HandleMouseDown(Event e, Vector2 canvasMouse, Vector2 graphMouse, Rect canvasRect)
    {
        if (_graph == null) return;

        // Left button
        if (e.button == 0)
        {
            // Check port hit first
            if (TryStartConnection(canvasMouse, canvasRect, graphMouse)) { e.Use(); return; }

            // Check node hit
            var hit = HitTestNode(graphMouse);
            if (hit != null)
            {
                _selectedId         = hit.nodeId;
                _isDraggingNode     = true;
                _dragStartMouse     = e.mousePosition;
                _dragStartNodePos   = hit.editorPosition;
                e.Use();
            }
            else
            {
                // Deselect + start panning
                _selectedId  = null;
                _isPanning   = true;
                _dragStartMouse = e.mousePosition;
                e.Use();
            }
        }

        // Middle button
        if (e.button == 2)
        {
            _isPanning = true;
            _dragStartMouse = e.mousePosition;
            e.Use();
        }

        Repaint();
    }

    private bool TryStartConnection(Vector2 canvasMouse, Rect canvasRect, Vector2 graphMouse)
    {
        if (_graph == null) return false;

        foreach (var node in _graph.nodes)
        {
            Rect nodeRect = GetNodeCanvasRect(node);

            // Output ports (right edge)
            var outputs = node.GetOutputPorts();
            for (int i = 0; i < outputs.Length; i++)
            {
                Vector2 portCenter = new Vector2(nodeRect.xMax, nodeRect.y + NodeHeaderH + i * PortRowH + PortRowH * 0.5f);
                if (Vector2.Distance(canvasMouse, portCenter) <= PortHitRadius)
                {
                    _isConnecting      = true;
                    _connFromNodeId    = node.nodeId;
                    _connFromPortId    = outputs[i].Id;
                    _connFromIsOutput  = true;
                    return true;
                }
            }

            // Input ports (left edge)
            var inputs = node.GetInputPorts();
            for (int i = 0; i < inputs.Length; i++)
            {
                Vector2 portCenter = new Vector2(nodeRect.xMin, nodeRect.y + NodeHeaderH + i * PortRowH + PortRowH * 0.5f);
                if (Vector2.Distance(canvasMouse, portCenter) <= PortHitRadius)
                {
                    _isConnecting      = true;
                    _connFromNodeId    = node.nodeId;
                    _connFromPortId    = inputs[i].Id;
                    _connFromIsOutput  = false;
                    return true;
                }
            }
        }
        return false;
    }

    private void HandleMouseDrag(Event e, Vector2 canvasMouse)
    {
        if (_isDraggingNode && _selectedId != null)
        {
            var node = _graph?.GetNode(_selectedId);
            if (node != null)
            {
                Vector2 delta = (e.mousePosition - _dragStartMouse) / _zoom;
                node.editorPosition = _dragStartNodePos + delta;
                EditorUtility.SetDirty(_graph);
                e.Use();
                Repaint();
            }
        }
        else if (_isPanning)
        {
            _panOffset += e.delta;
            e.Use();
            Repaint();
        }
        else if (_isConnecting)
        {
            Repaint();
        }
    }

    private void HandleMouseUp(Event e, Vector2 canvasMouse, Vector2 graphMouse, Rect canvasRect)
    {
        _isDraggingNode = false;
        _isPanning      = false;

        if (_isConnecting && e.button == 0)
        {
            _isConnecting = false;
            TryCompleteConnection(canvasMouse, graphMouse);
            e.Use();
            Repaint();
        }
    }

    private void TryCompleteConnection(Vector2 canvasMouse, Vector2 graphMouse)
    {
        if (_graph == null) return;

        foreach (var node in _graph.nodes)
        {
            if (node.nodeId == _connFromNodeId) continue;
            Rect nodeRect = GetNodeCanvasRect(node);

            // Try to connect to input port
            if (_connFromIsOutput)
            {
                var inputs = node.GetInputPorts();
                for (int i = 0; i < inputs.Length; i++)
                {
                    Vector2 portCenter = new Vector2(nodeRect.xMin,
                        nodeRect.y + NodeHeaderH + i * PortRowH + PortRowH * 0.5f);
                    if (Vector2.Distance(canvasMouse, portCenter) <= PortHitRadius)
                    {
                        Undo.RecordObject(_graph, "Connect Nodes");
                        _graph.Connect(_connFromNodeId, _connFromPortId, node.nodeId, inputs[i].Id);
                        EditorUtility.SetDirty(_graph);
                        return;
                    }
                }
            }
            else
            {
                // Dragged from input → connect from output
                var outputs = node.GetOutputPorts();
                for (int i = 0; i < outputs.Length; i++)
                {
                    Vector2 portCenter = new Vector2(nodeRect.xMax,
                        nodeRect.y + NodeHeaderH + i * PortRowH + PortRowH * 0.5f);
                    if (Vector2.Distance(canvasMouse, portCenter) <= PortHitRadius)
                    {
                        Undo.RecordObject(_graph, "Connect Nodes");
                        _graph.Connect(node.nodeId, outputs[i].Id, _connFromNodeId, _connFromPortId);
                        EditorUtility.SetDirty(_graph);
                        return;
                    }
                }
            }
        }
    }

    private void HandleScroll(Event e, Vector2 canvasMouse)
    {
        float oldZoom = _zoom;
        _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, MinZoom, MaxZoom);

        // Zoom toward mouse position
        float scale = _zoom / oldZoom;
        _panOffset = canvasMouse - scale * (canvasMouse - _panOffset);

        e.Use();
        Repaint();
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private void ShowContextMenu(Vector2 graphPos)
    {
        if (_graph == null) return;

        var menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("创建节点"));
        menu.AddSeparator("");

        var byCategory = _nodeTypes?.GroupBy(t => t.attr.Category).OrderBy(g => g.Key)
                         ?? Enumerable.Empty<IGrouping<string, (Type, SkillNodeTypeAttribute)>>();

        foreach (var group in byCategory)
        {
            foreach (var (type, attr) in group)
            {
                var capturedType = type;
                var capturedPos  = graphPos;
                menu.AddItem(new GUIContent($"{attr.Category}/{attr.DisplayName}", attr.Tooltip),
                             false,
                             () => CreateNodeOfType(capturedType, capturedPos));
            }
        }

        menu.AddSeparator("");
        if (_selectedId != null)
        {
            var capturedId = _selectedId;
            menu.AddItem(new GUIContent("删除选中节点"), false, () => DeleteNode(capturedId));
        }

        menu.ShowAsContext();
        Event.current.Use();
    }

    // ── Global keys ───────────────────────────────────────────────────────────

    private void HandleGlobalKeys()
    {
        if (Event.current.type != EventType.KeyDown) return;
        if (_selectedId == null) return;

        if (Event.current.keyCode == KeyCode.Delete ||
            Event.current.keyCode == KeyCode.Backspace)
        {
            DeleteNode(_selectedId);
            Event.current.Use();
        }
    }

    // ── Node operations ───────────────────────────────────────────────────────

    private void CreateNodeOfType(Type type, Vector2 graphPos)
    {
        if (_graph == null) return;
        var node = (SkillNode)Activator.CreateInstance(type);
        node.editorPosition = graphPos;
        Undo.RecordObject(_graph, "Add Node");
        _graph.AddNode(node);
        _selectedId = node.nodeId;
        EditorUtility.SetDirty(_graph);
        Repaint();
    }

    private void DeleteNode(string nodeId)
    {
        if (_graph == null) return;
        Undo.RecordObject(_graph, "Delete Node");
        _graph.RemoveNode(nodeId);
        if (_selectedId == nodeId) _selectedId = null;
        EditorUtility.SetDirty(_graph);
        Repaint();
    }

    // ── Asset operations ──────────────────────────────────────────────────────

    private void PickGraph()
    {
        string path = EditorUtility.OpenFilePanel("选择技能图", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = "Assets" + path.Replace(Application.dataPath, "");
        var graph = AssetDatabase.LoadAssetAtPath<SkillGraph>(path);
        if (graph != null) LoadGraph(graph);
    }

    private void CreateNewGraph()
    {
        string path = EditorUtility.SaveFilePanelInProject("新建技能图", "NewSkillGraph", "asset",
                                                            "选择保存路径",
                                                            "Assets/Gameplay/Skills/Graphs");
        if (string.IsNullOrEmpty(path)) return;

        var graph = CreateInstance<SkillGraph>();
        // Auto-create OnCastNode at center
        var entry = (SkillNode)Activator.CreateInstance(typeof(RPG.Skills.Graph.Nodes.OnCastNode));
        entry.editorPosition = new Vector2(100, 150);
        graph.AddNode(entry);

        AssetDatabase.CreateAsset(graph, path);
        AssetDatabase.SaveAssets();
        LoadGraph(graph);
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    private Rect GetNodeCanvasRect(SkillNode node)
    {
        Vector2 pos = node.editorPosition * _zoom + _panOffset;
        return new Rect(pos.x, pos.y, NodeWidth * _zoom, GetNodeHeight(node) * _zoom);
    }

    private float GetNodeHeight(SkillNode node)
    {
        int portRows   = Mathf.Max(node.GetInputPorts().Length, node.GetOutputPorts().Length);
        int fieldCount = GetNodeFields(node.GetType()).Count;
        return NodeHeaderH + portRows * PortRowH + fieldCount * (FieldRowH + 2) + 10f;
    }

    private SkillNode HitTestNode(Vector2 graphPos)
    {
        if (_graph == null) return null;
        // Iterate in reverse (top-most drawn last → hit first)
        for (int i = _graph.nodes.Count - 1; i >= 0; i--)
        {
            var node = _graph.nodes[i];
            var nodeRect = new Rect(node.editorPosition, new Vector2(NodeWidth, GetNodeHeight(node)));
            if (nodeRect.Contains(graphPos)) return node;
        }
        return null;
    }

    // Port world positions (window space, for Handles)
    private Vector2 GetOutputPortWindowPos(SkillNode node, string portId, Rect canvasRect)
    {
        var ports = node.GetOutputPorts();
        int idx = Array.FindIndex(ports, p => p.Id == portId);
        if (idx < 0) idx = 0;

        Rect nodeCanvasRect = GetNodeCanvasRect(node);
        float portY = nodeCanvasRect.y + NodeHeaderH * _zoom + idx * PortRowH * _zoom + PortRowH * _zoom * 0.5f;
        return new Vector2(canvasRect.x + nodeCanvasRect.xMax, canvasRect.y + portY);
    }

    private Vector2 GetInputPortWindowPos(SkillNode node, string portId, Rect canvasRect)
    {
        var ports = node.GetInputPorts();
        int idx = Array.FindIndex(ports, p => p.Id == portId);
        if (idx < 0) idx = 0;

        Rect nodeCanvasRect = GetNodeCanvasRect(node);
        float portY = nodeCanvasRect.y + NodeHeaderH * _zoom + idx * PortRowH * _zoom + PortRowH * _zoom * 0.5f;
        return new Vector2(canvasRect.x + nodeCanvasRect.xMin, canvasRect.y + portY);
    }

    // ── Reflection helpers ────────────────────────────────────────────────────

    private static void RefreshNodeTypes()
    {
        _nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => !t.IsAbstract &&
                        t.IsSubclassOf(typeof(SkillNode)) &&
                        t.GetCustomAttribute<SkillNodeTypeAttribute>() != null)
            .Select(t => (t, t.GetCustomAttribute<SkillNodeTypeAttribute>()))
            .OrderBy(x => x.Item2.Category)
            .ThenBy(x => x.Item2.DisplayName)
            .ToList();
    }

    private static readonly Dictionary<Type, List<(FieldInfo fi, NodeFieldAttribute attr)>> _fieldCache
        = new Dictionary<Type, List<(FieldInfo, NodeFieldAttribute)>>();

    private static List<(FieldInfo fi, NodeFieldAttribute attr)> GetNodeFields(Type type)
    {
        if (_fieldCache.TryGetValue(type, out var cached)) return cached;

        var list = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(fi => (fi, fi.GetCustomAttribute<NodeFieldAttribute>()))
            .Where(x => x.Item2 != null)
            .ToList();

        _fieldCache[type] = list;
        return list;
    }

    // ── Drawing utilities ─────────────────────────────────────────────────────

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width,     thickness), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness,     rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private static Color GetPortColor(PortDataType type)
    {
        switch (type)
        {
            case PortDataType.Execution:  return new Color(0.9f, 0.9f, 0.9f);
            case PortDataType.Float:      return new Color(0.4f, 0.9f, 0.4f);
            case PortDataType.Int:        return new Color(0.6f, 1.0f, 0.6f);
            case PortDataType.Bool:       return new Color(0.9f, 0.9f, 0.2f);
            case PortDataType.GameObject: return new Color(0.4f, 0.6f, 1.0f);
            case PortDataType.String:     return new Color(1.0f, 0.7f, 0.3f);
            default:                      return Color.white;
        }
    }

    private static Color ParseHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
        return new Color(0.3f, 0.5f, 0.7f);
    }

    private static string TruncateLabel(string s, int maxChars)
        => s.Length <= maxChars ? s : s.Substring(0, Mathf.Max(1, maxChars - 1)) + "…";
}
