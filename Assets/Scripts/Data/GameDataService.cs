using Framework.Assets;
using Framework.Interfaces;
using RPG.Achievements;
using RPG.Buff;
using RPG.Enemy;
using RPG.Items;
using RPG.Quests;
using RPG.Skills;
using UnityEngine;

namespace RPG.Data
{
    /// <summary>
    /// 游戏数据服务 — 所有运行时数据的统一入口。
    ///
    /// ■ 唯一职责：持有所有数据库的引用，并提供类型安全的访问接口。
    ///   业务逻辑（查找道具、生成敌人、激活技能）不在此类，由各自系统处理。
    ///
    /// ■ 访问方式（任何代码均可调用，无需持有引用）：
    ///
    ///   // 按 ID 获取技能
    ///   SkillData fireball = GameDataService.Instance.Skills.GetById("skill_fireball");
    ///
    ///   // 按 ID 获取敌人
    ///   EnemyData goblin   = GameDataService.Instance.Enemies.GetById("enemy_goblin");
    ///
    ///   // 检查 Buff 是否存在
    ///   bool hasBuff = GameDataService.Instance.Buffs.Exists("buff_poison");
    ///
    /// ■ 初始化方式（二选一，优先级从高到低）：
    ///
    ///   1. 将 <see cref="GameDataLoader"/> 放在持久化 GameObject 上，
    ///      Inspector 中拖入 GameDataService 资产 → 最早在 Awake 完成注册。
    ///
    ///   2. 自动降级：将 GameDataService 资产放在
    ///      Assets/Resources/GameData/GameDataService.asset，
    ///      首次访问 <see cref="Instance"/> 时自动 Resources.Load。
    ///
    /// ■ 创建资产: Assets/Create → RPG/Data/Game Data Service
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataService", menuName = "RPG/Data/Game Data Service")]
    public class GameDataService : ScriptableObject
    {
        // ── 静态访问 ──────────────────────────────────────────────────────────

        private static GameDataService _instance;

        /// <summary>全局单例访问点。</summary>
        public static GameDataService Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // 降级：通过 AssetService 加载（默认 Resources 后端）
                _instance = AssetService.Load<GameDataService>(AssetPaths.Data.GameDataService);
                if (_instance == null)
                    Debug.LogError("[GameDataService] 未找到资产。" +
                                   "请将资产放在 Resources/GameData/GameDataService.asset，" +
                                   "或在场景中放置 GameDataLoader 组件并赋值。");
                else
                    _instance.InitializeAll();
                return _instance;
            }
        }

        /// <summary>由 <see cref="GameDataLoader"/> 在 Awake 调用，注册当前服务实例。</summary>
        internal static void Register(GameDataService service)
        {
            _instance = service;
            service.InitializeAll();
        }

        // ── 数据库引用 ────────────────────────────────────────────────────────

        [Header("玩家")]
        [SerializeField] private PlayerData            _playerData;

        [Header("物品 / 技能 / 敌人")]
        [SerializeField] private ItemDatabase          _itemDatabase;
        [SerializeField] private SkillDatabase         _skillDatabase;
        [SerializeField] private EnemyDatabase         _enemyDatabase;

        [Header("Buff / 状态效果")]
        [SerializeField] private BuffDatabase          _buffDatabase;

        [Header("任务 / 成就")]
        [SerializeField] private QuestDatabase         _questDatabase;
        [SerializeField] private AchievementDatabase   _achievementDatabase;

        // ── 类型安全访问接口 ──────────────────────────────────────────────────

        /// <summary>当前角色的玩家数据配置。</summary>
        public PlayerData                     Player       => _playerData;

        public IRepository<ItemData>          Items        => EnsureInit<ItemData>(_itemDatabase);
        public IRepository<SkillData>         Skills       => EnsureInit<SkillData>(_skillDatabase);
        public IRepository<EnemyData>         Enemies      => EnsureInit<EnemyData>(_enemyDatabase);
        public IRepository<BuffData>          Buffs        => EnsureInit<BuffData>(_buffDatabase);
        public IRepository<QuestData>         Quests       => EnsureInit<QuestData>(_questDatabase);
        public IRepository<AchievementData>   Achievements => EnsureInit<AchievementData>(_achievementDatabase);

        // ── 便捷方法 ──────────────────────────────────────────────────────────

        public SkillData  GetSkill(string id)        => Skills?.GetById(id);
        public EnemyData  GetEnemy(string id)        => Enemies?.GetById(id);
        public ItemData   GetItem(string id)         => Items?.GetById(id);
        public BuffData   GetBuff(string id)         => Buffs?.GetById(id);
        public QuestData  GetQuest(string id)        => Quests?.GetById(id);

        // ── 初始化 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化所有已赋值的数据库。
        /// 数据库均继承 <see cref="RPG.Data.RepositoryBase{T}"/>，可直接调用 Initialize()，
        /// 无需反射。
        /// </summary>
        public void InitializeAll()
        {
            _itemDatabase?.Initialize();
            _skillDatabase?.Initialize();
            _enemyDatabase?.Initialize();
            _buffDatabase?.Initialize();
            _questDatabase?.Initialize();
            _achievementDatabase?.Initialize();
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────

        private static IRepository<T> EnsureInit<T>(ScriptableObject db)
            where T : class
        {
            if (db == null) return null;
            if (db is IRepository<T> repo) return repo;
            return null;
        }

        // ── ScriptableObject 生命周期 ─────────────────────────────────────────

        private void OnEnable()
        {
            // 在编辑器里每次重新激活时重置，以便热重载
            if (_instance == this) _instance = null;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }
    }
}
