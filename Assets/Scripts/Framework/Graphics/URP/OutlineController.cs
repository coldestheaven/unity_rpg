using UnityEngine;

namespace Framework.Graphics.URP
{
    /// <summary>
    /// 标记一个 GameObject 参与 URP 轮廓描边渲染（与 <see cref="OutlineFeature"/> 配合使用）。
    ///
    /// <para>
    /// 挂载到需要描边的对象（敌人、可交互物品等）。
    /// 组件的 Layer 必须在 <see cref="OutlineFeature"/> 设置的
    /// <c>OutlineLayers</c> LayerMask 中。
    /// </para>
    ///
    /// 使用示例：
    /// <code>
    /// // 高亮选中的敌人
    /// enemy.GetComponent&lt;OutlineController&gt;()?.Show(Color.red);
    ///
    /// // 取消高亮
    /// enemy.GetComponent&lt;OutlineController&gt;()?.Hide();
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RPG/Graphics/Outline Controller")]
    public sealed class OutlineController : MonoBehaviour
    {
        [Tooltip("默认轮廓颜色。可在运行时通过 Show(color) 覆盖。")]
        [SerializeField] private Color _defaultColor = new Color(1f, 0.85f, 0.1f, 1f);

        [Tooltip("组件启用时自动激活轮廓。")]
        [SerializeField] private bool _showOnEnable = false;

        // ── 属性 ──────────────────────────────────────────────────────────────

        /// <summary>当前是否已激活轮廓描边。</summary>
        public bool  IsOutlined    { get; private set; }

        /// <summary>当前激活的轮廓颜色。</summary>
        public Color OutlineColor  { get; private set; }

        // ── Unity 生命周期 ────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_showOnEnable) Show();
        }

        private void OnDisable() => Hide();

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>以默认颜色激活轮廓。</summary>
        public void Show() => Show(_defaultColor);

        /// <summary>以指定颜色激活轮廓。</summary>
        public void Show(Color color)
        {
            OutlineColor = color;
            IsOutlined   = true;
            OutlineRegistry.Register(this);
        }

        /// <summary>取消轮廓描边。</summary>
        public void Hide()
        {
            IsOutlined = false;
            OutlineRegistry.Unregister(this);
        }

        /// <summary>切换轮廓显示状态。</summary>
        public void Toggle()
        {
            if (IsOutlined) Hide();
            else Show();
        }
    }
}
