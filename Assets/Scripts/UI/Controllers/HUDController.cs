using UnityEngine;
using UnityEngine.UI;

namespace UI.Controllers
{
    /// <summary>
    /// HUD控制器 - 显示玩家信息
    /// </summary>
    public class HUDController : Framework.Base.MonoBehaviourBase
    {
        [Header("Health")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText;

        [Header("Stats")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text expText;

        [Header("Experience")]
        [SerializeField] private Slider expSlider;

        private Gameplay.Player.PlayerHealth playerHealth;

        protected override void Awake()
        {
            base.Awake();
            FindPlayer();
        }

        protected override void Update()
        {
            base.Update();
            UpdateHealthDisplay();
        }

        private void FindPlayer()
        {
            var player = Gameplay.Player.PlayerController.Instance;
            if (player != null)
            {
                playerHealth = player.Health;

                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += HandleHealthChanged;
                    HandleHealthChanged(playerHealth.CurrentHealth);
                }
            }
        }

        private void HandleHealthChanged(int currentHealth)
        {
            UpdateHealthDisplay();
        }

        private void UpdateHealthDisplay()
        {
            if (playerHealth == null) return;

            if (healthSlider != null)
            {
                healthSlider.maxValue = playerHealth.MaxHealth;
                healthSlider.value = playerHealth.CurrentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
            }
        }

        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lvl {level}";
            }
        }

        public void UpdateGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = gold.ToString();
            }
        }

        public void UpdateExperience(int currentExp, int maxExp)
        {
            if (expSlider != null)
            {
                expSlider.maxValue = maxExp;
                expSlider.value = currentExp;
            }

            if (expText != null)
            {
                expText.text = $"{currentExp}/{maxExp}";
            }
        }
    }
}
