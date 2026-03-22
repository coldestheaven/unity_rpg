using UnityEngine;
using UnityEngine.UI;
using RPG.Core;
using RPG.Player;
using RPG.Items;

namespace RPG.UI
{
    /// <summary>
    /// HUD控制器 - 显示玩家状态
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("玩家信息")]
        public Text levelText;
        public Text healthText;
        public Text goldText;
        public Text experienceText;

        [Header("进度条")]
        public Slider healthSlider;
        public Slider experienceSlider;

        [Header("生命条")]
        public HealthBar healthBar;

        [Header("状态图标")]
        public GameObject[] statusIcons;

        private PlayerController player;
        private PlayerHealth playerHealth;
        private InventorySystem inventory;
        private PlayerState playerState;

        private void Start()
        {
            FindReferences();
            SubscribeToEvents();
            UpdateHUD();
        }

        private void FindReferences()
        {
            player = PlayerController.Instance;
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                inventory = player.GetComponent<InventorySystem>();
                playerState = player.GetComponent<PlayerState>();
            }

            if (healthSlider == null)
            {
                healthSlider = GetComponentInChildren<Slider>();
            }

            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<HealthBar>();
            }
        }

        private void SubscribeToEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += OnHealthChanged;
                playerHealth.OnPlayerDeath += OnPlayerDeath;
            }

            if (inventory != null)
            {
                inventory.OnGoldChanged += OnGoldChanged;
            }

            // 监听事件管理器的事件
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("PlayerHealthChanged", OnPlayerHealthChanged);
                EventManager.Instance.AddListener("GoldChanged", OnGoldChangedEvent);
                EventManager.Instance.AddListener("PlayerDied", OnPlayerDiedEvent);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= OnHealthChanged;
                playerHealth.OnPlayerDeath -= OnPlayerDeath;
            }

            if (inventory != null)
            {
                inventory.OnGoldChanged -= OnGoldChanged;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("PlayerHealthChanged", OnPlayerHealthChanged);
                EventManager.Instance.RemoveListener("GoldChanged", OnGoldChangedEvent);
                EventManager.Instance.RemoveListener("PlayerDied", OnPlayerDiedEvent);
            }
        }

        private void Update()
        {
            // 定期更新UI
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (playerState == null) return;

            UpdatePlayerInfo();
        }

        private void UpdatePlayerInfo()
        {
            if (levelText != null && playerState != null)
            {
                levelText.text = $"Lv.{playerState.CurrentData.level}";
            }

            if (healthText != null && playerHealth != null)
            {
                healthText.text = $"{Mathf.Floor(playerHealth.CurrentHealth)}/{Mathf.Floor(playerHealth.MaxHealth)}";
            }

            if (experienceText != null && playerState != null)
            {
                experienceText.text = $"{playerState.CurrentData.experience:F0}/{playerState.CurrentData.experienceToNextLevel:F0}";
            }
        }

        #region Event Handlers

        private void OnHealthChanged(float health)
        {
            UpdateHealthUI(health);
        }

        private void OnHealthChanged(HealthChangedEventArgs args)
        {
            UpdateHealthUI(args.currentHealth);
        }

        private void OnPlayerHealthChanged(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is HealthChangedEventArgs data)
            {
                UpdateHealthUI(data.currentHealth);
            }
        }

        private void UpdateHealthUI(float health)
        {
            if (healthSlider != null && playerHealth != null)
            {
                healthSlider.maxValue = playerHealth.MaxHealth;
                healthSlider.value = health;
            }

            if (healthBar != null && playerHealth != null)
            {
                healthBar.SetHealth((int)health);
            }
        }

        private void OnGoldChanged(float gold)
        {
            UpdateGoldUI((int)gold);
        }

        private void OnGoldChangedEvent(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is GoldEventArgs data)
            {
                UpdateGoldUI((int)data.currentGold);
            }
        }

        private void UpdateGoldUI(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"金币: {gold}";
            }
        }

        private void OnPlayerDeath()
        {
            Debug.Log("Player died - HUD updated");
        }

        private void OnPlayerDiedEvent(object[] args)
        {
            Debug.Log("Player died event - HUD updated");
        }

        #endregion

        public void UpdateExperienceUI(float current, float max)
        {
            if (experienceSlider != null)
            {
                experienceSlider.maxValue = max;
                experienceSlider.value = current;
            }

            if (experienceText != null)
            {
                experienceText.text = $"{current:F0}/{max:F0}";
            }
        }

        public void ShowStatusIcon(int index)
        {
            if (statusIcons != null && index >= 0 && index < statusIcons.Length)
            {
                statusIcons[index].SetActive(true);
            }
        }

        public void HideStatusIcon(int index)
        {
            if (statusIcons != null && index >= 0 && index < statusIcons.Length)
            {
                statusIcons[index].SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}
