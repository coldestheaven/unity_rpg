using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 数据引导组件 — 场景启动时将 <see cref="GameDataService"/> 注册为全局单例。
    ///
    /// 使用方法:
    ///   1. 在持久化 GameObject（如 GameManager 或专用的 [GameData] 对象）上
    ///      挂载此组件。
    ///   2. 在 Inspector 的 "数据服务" 字段中拖入 GameDataService 资产。
    ///   3. 确保此 GameObject 的 Awake 早于其他依赖数据的组件执行
    ///      （Script Execution Order 中提前，或放在 GameManager 的 Awake 顶部）。
    ///
    /// 如果不使用此组件，<see cref="GameDataService.Instance"/> 会自动降级
    /// 到 Resources.Load("GameData/GameDataService")。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameDataLoader : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("拖入 GameDataService ScriptableObject 资产。")]
        private GameDataService _dataService;

        private void Awake()
        {
            if (_dataService == null)
            {
                Debug.LogWarning("[GameDataLoader] 未赋值 DataService，尝试 Resources.Load 降级。");
                return;
            }

            GameDataService.Register(_dataService);
            Debug.Log("[GameDataLoader] GameDataService 注册完成，" +
                      $"Skills={_dataService.Skills?.Count ?? 0}  " +
                      $"Enemies={_dataService.Enemies?.Count ?? 0}  " +
                      $"Items={_dataService.Items?.Count ?? 0}  " +
                      $"Buffs={_dataService.Buffs?.Count ?? 0}");
        }
    }
}
