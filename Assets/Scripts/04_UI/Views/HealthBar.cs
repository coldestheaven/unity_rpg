using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    /// <summary>
    /// 生命条UI组件
    /// </summary>
    public class HealthBar : Framework.Base.MonoBehaviourBase
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;

        [SerializeField] private bool showText = true;
        [SerializeField] private bool changeColorOnLowHealth = true;

        private Gameplay.Combat.Health health;

        protected override void Awake()
        {
            base.Awake();
            FindHealthComponent();
        }

        protected override void Update()
        {
            base.Update();
            UpdateHealthBar();
        }

        private void FindHealthComponent()
        {
            health = GetComponentInParent<Gameplay.Combat.Health>();

            if (health == null)
            {
                health = GetComponent<Gameplay.Combat.Health>();
            }

            if (health != null)
            {
                health.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(int currentHealth)
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (health == null) return;

            float normalizedHealth = (float)health.CurrentHealth / health.MaxHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = health.MaxHealth;
                healthSlider.value = health.CurrentHealth;
            }

            if (healthText != null && showText)
            {
                healthText.text = $"{health.CurrentHealth}/{health.MaxHealth}";
            }

            if (fillImage != null && changeColorOnLowHealth)
            {
                fillImage.color = Color.Lerp(lowHealthColor, healthyColor, normalizedHealth);
            }
        }

        public void SetHealth(int currentHealth, int maxHealth)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (healthText != null && showText)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }

            float normalizedHealth = (float)currentHealth / maxHealth;
            if (fillImage != null && changeColorOnLowHealth)
            {
                fillImage.color = Color.Lerp(lowHealthColor, healthyColor, normalizedHealth);
            }
        }
    }
}
