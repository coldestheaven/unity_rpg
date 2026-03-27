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
        private RPG.Core.PlayerProgressManager progressManager;

        protected override void Awake()
        {
            base.Awake();
            TryBindPlayer();
            TryBindProgress();
        }

        private void OnEnable()
        {
            TryBindPlayer();
            TryBindProgress();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnbindPlayer();
            UnbindProgress();
        }

        protected override void Update()
        {
            base.Update();

            if (playerHealth == null)
            {
                TryBindPlayer();
            }

            if (progressManager == null)
            {
                TryBindProgress();
            }
        }

        private void TryBindPlayer()
        {
            if (playerHealth != null) return;

            var player = Gameplay.Player.PlayerController.Instance;
            if (player == null) return;

            playerHealth = player.Health;
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(Mathf.RoundToInt(playerHealth.CurrentHealth));
        }

        private void UnbindPlayer()
        {
            if (playerHealth == null) return;
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth = null;
        }

        private void TryBindProgress()
        {
            if (progressManager != null) return;

            progressManager = RPG.Core.PlayerProgressManager.Instance;
            if (progressManager == null) return;

            progressManager.OnLevelUp += HandleLevelChanged;
            progressManager.OnExperienceGained += HandleExperienceChanged;
            progressManager.OnGoldGained += HandleGoldChanged;
            RefreshProgressDisplay();
        }

        private void UnbindProgress()
        {
            if (progressManager == null) return;

            progressManager.OnLevelUp -= HandleLevelChanged;
            progressManager.OnExperienceGained -= HandleExperienceChanged;
            progressManager.OnGoldGained -= HandleGoldChanged;
            progressManager = null;
        }

        private void HandleHealthChanged(int currentHealth)
        {
            UpdateHealthDisplay();
        }

        private void HandleLevelChanged(int level)
        {
            UpdateLevel(level);
            RefreshExperienceDisplay();
        }

        private void HandleExperienceChanged(float amount)
        {
            RefreshExperienceDisplay();
        }

        private void HandleGoldChanged(int amount)
        {
            if (progressManager != null)
            {
                UpdateGold(progressManager.GetGold());
            }
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
                healthText.text = $"{Mathf.RoundToInt(playerHealth.CurrentHealth)}/{Mathf.RoundToInt(playerHealth.MaxHealth)}";
            }
        }

        private void RefreshProgressDisplay()
        {
            if (progressManager == null) return;

            UpdateLevel(progressManager.GetLevel());
            UpdateGold(progressManager.GetGold());
            RefreshExperienceDisplay();
        }

        private void RefreshExperienceDisplay()
        {
            if (progressManager == null) return;
            UpdateExperienceUI(progressManager.GetExperience(), progressManager.GetExperienceToNextLevel());
        }

        private void RefreshAll()
        {
            UpdateHealthDisplay();
            RefreshProgressDisplay();
        }

        public void UpdateHUD()
        {
            RefreshAll();
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

        public void UpdateExperienceUI(float currentExp, float maxExp)
        {
            if (expSlider != null)
            {
                expSlider.maxValue = maxExp;
                expSlider.value = currentExp;
            }

            if (expText != null)
            {
                expText.text = $"{currentExp:0}/{maxExp:0}";
            }
        }
    }
}
