using Framework.Base;
using Gameplay.Combat;
using RPG.Core;
using UnityEngine;

namespace Gameplay.Enemy
{
    public class EnemyReward : MonoBehaviourBase
    {
        [Header("Rewards")]
        [SerializeField] private int experienceReward = 25;
        [SerializeField] private int goldReward = 5;

        private Health health;

        protected override void Awake()
        {
            base.Awake();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            GameManager.Instance?.AddExperience(experienceReward);
            GameManager.Instance?.AddGold(goldReward);

            Framework.Events.EventBus.Publish(new Framework.Events.EnemyDiedEvent
            {
                EnemyName = gameObject.name,
                Position = transform.position,
                XpReward = experienceReward,
                GoldReward = goldReward
            });
        }

        public void Configure(int experienceAmount, int goldAmount)
        {
            experienceReward = experienceAmount;
            goldReward = goldAmount;
        }
    }
}
