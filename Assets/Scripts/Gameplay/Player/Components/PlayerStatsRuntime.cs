using System;
using Core.Stats;
using RPG.Core;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStatsRuntime : Framework.Base.MonoBehaviourBase
    {
        [Header("Base Stats")]
        [SerializeField] private bool initializeBaseStatsFromComponents = true;
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseAttackDamage = 10f;
        [SerializeField] private float baseDefense = 0f;
        [SerializeField] private float baseMoveSpeed = 5f;

        [Header("Level Growth")]
        [SerializeField] private float healthPerLevel = 10f;
        [SerializeField] private float attackPerLevel = 2f;
        [SerializeField] private float defensePerLevel = 1f;
        [SerializeField] private float moveSpeedPerLevel = 0f;

        private PlayerController controller;
        private PlayerHealth health;
        private IPlayerStatModifierSource[] modifierSources = Array.Empty<IPlayerStatModifierSource>();

        public PlayerStatBlock CurrentStats { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();

            if (initializeBaseStatsFromComponents)
            {
                CaptureBaseStatsFromComponents();
            }
        }

        private void Start()
        {
            RefreshStats();
        }

        private void OnEnable()
        {
            CacheModifierSources();
            SubscribeToModifierSources();

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnProgressChanged += HandleProgressChanged;
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromModifierSources();

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnProgressChanged -= HandleProgressChanged;
            }
        }

        public void RefreshStats()
        {
            ApplyStats(BuildStats());
        }

        private void CaptureBaseStatsFromComponents()
        {
            if (controller == null)
            {
                return;
            }

            if (health != null)
            {
                baseMaxHealth = health.MaxHealth;
                baseDefense = health.Defense;
            }

            baseAttackDamage = controller.AttackDamage;
            baseMoveSpeed = controller.MoveSpeed;
        }

        private void CacheModifierSources()
        {
            var behaviours = GetComponents<MonoBehaviour>();
            var sources = new System.Collections.Generic.List<IPlayerStatModifierSource>();

            foreach (var behaviour in behaviours)
            {
                if (behaviour is IPlayerStatModifierSource source)
                {
                    sources.Add(source);
                }
            }

            modifierSources = sources.ToArray();
        }

        private void SubscribeToModifierSources()
        {
            foreach (var source in modifierSources)
            {
                source.ModifiersChanged += HandleModifiersChanged;
            }
        }

        private void UnsubscribeFromModifierSources()
        {
            foreach (var source in modifierSources)
            {
                source.ModifiersChanged -= HandleModifiersChanged;
            }
        }

        private PlayerStatBlock BuildStats()
        {
            PlayerStatBlock stats = new PlayerStatBlock(
                baseMaxHealth,
                baseAttackDamage,
                baseDefense,
                baseMoveSpeed);

            int level = Mathf.Max(1, PlayerProgressManager.Instance?.GetLevel() ?? 1);
            int levelOffset = Mathf.Max(0, level - 1);
            stats.Add(
                healthPerLevel * levelOffset,
                attackPerLevel * levelOffset,
                defensePerLevel * levelOffset,
                moveSpeedPerLevel * levelOffset);

            foreach (var source in modifierSources)
            {
                source.ApplyModifiers(ref stats);
            }

            stats.MaxHealth = Mathf.Max(1f, stats.MaxHealth);
            stats.AttackDamage = Mathf.Max(0f, stats.AttackDamage);
            stats.Defense = Mathf.Max(0f, stats.Defense);
            stats.MoveSpeed = Mathf.Max(0f, stats.MoveSpeed);
            return stats;
        }

        private void ApplyStats(PlayerStatBlock stats)
        {
            if (controller == null)
            {
                return;
            }

            float previousMaxHealth = health != null ? health.MaxHealth : stats.MaxHealth;

            controller.SetAttackDamage(stats.AttackDamage);
            controller.SetDefense(stats.Defense);
            controller.SetMoveSpeed(stats.MoveSpeed);
            controller.SetMaxHealth(stats.MaxHealth);

            if (health != null && stats.MaxHealth > previousMaxHealth)
            {
                health.Heal(stats.MaxHealth - previousMaxHealth);
            }

            CurrentStats = stats;
        }

        private void HandleModifiersChanged()
        {
            RefreshStats();
        }

        private void HandleProgressChanged(PlayerProgress progress)
        {
            RefreshStats();
        }
    }
}
