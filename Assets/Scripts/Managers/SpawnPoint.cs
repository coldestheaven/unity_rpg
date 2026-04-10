using UnityEngine;

namespace RPG.Scene
{
    /// <summary>
    /// 场景中的出生点标记。
    ///
    /// ■ 用途：
    ///   • 在关卡编辑器中标记玩家可出现的位置。
    ///   • <see cref="SceneController"/> 通过 <see cref="SpawnId"/> 查找并传送玩家。
    ///   • 也可用于传送门目的地：在两个场景各放一个 SpawnPoint，互相引用对方 SpawnId。
    ///
    /// ■ 挂载：将此脚本拖到空 GameObject，设置 SpawnId，确保唯一。
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("唯一标识符，与 SceneController.LoadScene(targetSpawn) 匹配。")]
        public string SpawnId = "Default";

        [Tooltip("出生点类型：默认、存档点、传送门出口等。")]
        public SpawnPointType Type = SpawnPointType.Default;

        [Tooltip("出生时玩家朝向（仅 Y 轴旋转）。")]
        public float FacingYaw = 0f;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void OnEnable()  => SceneController.RegisterSpawnPoint(this);
        private void OnDisable() => SceneController.UnregisterSpawnPoint(this);

        // ── 工具方法 ──────────────────────────────────────────────────────────

        /// <summary>返回出生点世界坐标。</summary>
        public Vector3 GetPosition() => transform.position;

        /// <summary>返回出生朝向四元数（仅 Y 轴）。</summary>
        public Quaternion GetFacing() => Quaternion.Euler(0f, FacingYaw, 0f);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Type switch
            {
                SpawnPointType.Save   => Color.cyan,
                SpawnPointType.Portal => Color.yellow,
                _                     => Color.green,
            };
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + GetFacing() * Vector3.forward * 0.6f);

            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f,
                $"[{Type}] {SpawnId}");
        }
#endif
    }

    public enum SpawnPointType
    {
        Default,
        Save,
        Portal,
        EnemySpawn,
    }
}
