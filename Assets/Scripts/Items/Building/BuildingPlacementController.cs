using System;
using UnityEngine;
using Framework.Events;

namespace RPG.Building
{
    // ──────────────────────────────────────────────────────────────────────────
    // BuildingPlacementController
    //
    // 职责：
    //   • 管理"建造模式"的视觉预览与玩家输入。
    //   • 跟随鼠标/触摸移动 Ghost 预览体，绿色=可放，红色=不可放。
    //   • 按 R 键旋转，Escape/中键取消，左键/调用 ConfirmPlacement 确认。
    //   • 确认后委托给 BuildingSystem.PlaceBuilding。
    //
    // 使用方式：
    //   BuildingPlacementController.Instance.StartPlacement(someBuildingData);
    //
    // 集成：
    //   • 挂载在场景中的持久化 GameObject（PlayerInput 或专属建造管理器上）。
    //   • Camera.main 用于屏幕→世界空间的 Raycast；如需支持多摄像机请替换 _cam。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 建筑放置控制器 — 管理预览 Ghost、输入处理与最终放置。
    /// </summary>
    public sealed class BuildingPlacementController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("放置设置")]
        [Tooltip("地面 Raycast 层（Camera.main 射线检测）。")]
        [SerializeField] private LayerMask _groundLayer;

        [Tooltip("是否将放置坐标吸附到网格。")]
        [SerializeField] private bool _snapToGrid = false;

        [Tooltip("网格大小（世界单位），仅 snapToGrid=true 时生效。")]
        [SerializeField, Min(0.1f)] private float _gridSize = 1f;

        [Tooltip("预览 Ghost 的可放材质（绿色）。")]
        [SerializeField] private Material _validMaterial;

        [Tooltip("预览 Ghost 的不可放材质（红色）。")]
        [SerializeField] private Material _invalidMaterial;

        [Header("键位")]
        [SerializeField] private KeyCode _cancelKey  = KeyCode.Escape;
        [SerializeField] private KeyCode _rotateKey  = KeyCode.R;
        [SerializeField] private KeyCode _confirmKey = KeyCode.Mouse0;

        // ── 事件 ─────────────────────────────────────────────────────────────

        public event Action<BuildingData>  OnPlacementStarted;
        public event Action                OnPlacementCancelled;
        public event Action<PlacedBuilding> OnBuildingPlaced;

        // ── 运行时状态 ────────────────────────────────────────────────────────

        public bool         IsPlacing      { get; private set; }
        public BuildingData PendingBuilding { get; private set; }

        private GameObject _previewGo;
        private Renderer[] _previewRenderers;
        private bool        _isValidPlacement;
        private float       _currentRotationY;
        private Camera      _cam;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (!IsPlacing) return;

            HandleInput();
            UpdatePreview();
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>进入建造模式，显示 Ghost 预览。</summary>
        public void StartPlacement(BuildingData data)
        {
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning("[BuildingPlacement] BuildingData 无效，无法开始放置。");
                return;
            }

            if (IsPlacing)
                CancelPlacement();

            PendingBuilding   = data;
            IsPlacing         = true;
            _currentRotationY = 0f;

            SpawnPreview(data);

            EventBus.Publish(new BuildingPlacementStartedEvent(data.buildingId));
            OnPlacementStarted?.Invoke(data);
        }

        /// <summary>取消建造模式，销毁预览。</summary>
        public void CancelPlacement()
        {
            if (!IsPlacing) return;

            DestroyPreview();
            IsPlacing       = false;
            PendingBuilding = null;

            EventBus.Publish(new BuildingPlacementCancelledEvent());
            OnPlacementCancelled?.Invoke();
        }

        /// <summary>
        /// 在当前预览位置确认放置。
        /// 若位置无效或材料不足则返回 false。
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!IsPlacing || !_isValidPlacement) return false;

            var position = _previewGo != null
                ? _previewGo.transform.position
                : Vector3.zero;
            var rotation = Quaternion.Euler(0f, _currentRotationY, 0f);

            var placed = BuildingSystem.Instance?.PlaceBuilding(PendingBuilding, position, rotation);
            if (placed == null) return false;

            OnBuildingPlaced?.Invoke(placed);

            // 放置后自动进入下一个同类建造，或退出放置模式
            // 默认退出，若需连续放置可在子类 Override 或事件中重新调用 StartPlacement
            CancelPlacement();
            return true;
        }

        // ── 输入处理 ──────────────────────────────────────────────────────────

        private void HandleInput()
        {
            if (Input.GetKeyDown(_cancelKey))
            {
                CancelPlacement();
                return;
            }

            if (Input.GetKeyDown(_rotateKey))
            {
                _currentRotationY = (_currentRotationY + 90f) % 360f;
            }

            if (Input.GetKeyDown(_confirmKey))
            {
                ConfirmPlacement();
            }
        }

        // ── 预览更新 ──────────────────────────────────────────────────────────

        private void UpdatePreview()
        {
            if (_previewGo == null) return;

            if (TryGetPlacementPosition(out Vector3 pos))
            {
                _previewGo.transform.position = pos;
                _previewGo.transform.rotation = Quaternion.Euler(0f, _currentRotationY, 0f);

                _isValidPlacement = CheckPlacementValid(pos);
            }
            else
            {
                _isValidPlacement = false;
            }

            ApplyPreviewMaterial(_isValidPlacement);
        }

        private bool TryGetPlacementPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_cam == null) return false;

            var ray = _cam.ScreenPointToRay(Input.mousePosition);

            if (_groundLayer.value != 0)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 500f, _groundLayer))
                {
                    position = _snapToGrid ? SnapToGrid(hit.point) : hit.point;
                    return true;
                }
                // 2D 回退
                if (Physics2D.GetRayIntersection(ray, Mathf.Infinity, _groundLayer) is { } hit2d && hit2d.collider != null)
                {
                    position = _snapToGrid ? SnapToGrid(hit2d.point) : (Vector3)hit2d.point;
                    return true;
                }
                return false;
            }

            // 无地面层：使用摄像机近平面投影（纯 2D 视角）
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                var p = ray.GetPoint(enter);
                position = _snapToGrid ? SnapToGrid(p) : p;
                return true;
            }
            return false;
        }

        private bool CheckPlacementValid(Vector3 position)
        {
            var data = PendingBuilding;
            if (data == null) return false;

            // 碰撞检测（3D OverlapBox）
            if (data.blockedByLayers.value != 0)
            {
                var half = new Vector3(data.footprintSize.x * 0.5f - 0.05f,
                                       0.4f,
                                       data.footprintSize.y * 0.5f - 0.05f);
                var rot  = Quaternion.Euler(0f, _currentRotationY, 0f);
                var cols = Physics.OverlapBox(position + Vector3.up * 0.4f,
                                              half, rot, data.blockedByLayers);
                if (cols.Length > 0) return false;
            }

            // 2D 碰撞检测
            if (data.blockedByLayers.value != 0)
            {
                var half2d = new Vector2(data.footprintSize.x * 0.5f - 0.05f,
                                         data.footprintSize.y * 0.5f - 0.05f);
                var cols2d = Physics2D.OverlapBoxAll(position, half2d * 2f, _currentRotationY,
                                                     data.blockedByLayers);
                if (cols2d.Length > 0) return false;
            }

            // 可选：检查是否在有效地面层上
            if (data.requiresGroundContact && data.validGroundLayers.value != 0)
            {
                bool onGround = Physics.Raycast(position + Vector3.up * 0.1f,
                                                Vector3.down, 0.3f, data.validGroundLayers);
                if (!onGround) return false;
            }

            // 费用检查
            if (BuildingSystem.Instance != null && !BuildingSystem.Instance.CanAfford(data))
                return false;

            return true;
        }

        private void ApplyPreviewMaterial(bool valid)
        {
            if (_previewRenderers == null) return;

            var mat = valid ? _validMaterial : _invalidMaterial;
            if (mat == null) return;

            foreach (var r in _previewRenderers)
                if (r != null) r.sharedMaterial = mat;
        }

        // ── 预览 GameObject 管理 ──────────────────────────────────────────────

        private void SpawnPreview(BuildingData data)
        {
            var prefab = data.previewPrefab != null ? data.previewPrefab : data.buildingPrefab;
            if (prefab == null) return;

            _previewGo        = Instantiate(prefab);
            _previewRenderers = _previewGo.GetComponentsInChildren<Renderer>(true);

            // 禁用所有碰撞体，避免预览体与场景发生碰撞
            foreach (var col in _previewGo.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            foreach (var col2d in _previewGo.GetComponentsInChildren<Collider2D>(true))
                col2d.enabled = false;

            // 禁用所有脚本逻辑
            foreach (var mb in _previewGo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && !(mb is Transform))
                    mb.enabled = false;
            }
        }

        private void DestroyPreview()
        {
            if (_previewGo != null)
            {
                Destroy(_previewGo);
                _previewGo        = null;
                _previewRenderers = null;
            }
        }

        // ── 工具 ─────────────────────────────────────────────────────────────

        private Vector3 SnapToGrid(Vector3 pos)
        {
            float g = _gridSize;
            return new Vector3(
                Mathf.Round(pos.x / g) * g,
                pos.y,
                Mathf.Round(pos.z / g) * g);
        }

        private void OnDestroy()
        {
            DestroyPreview();
        }
    }
}
