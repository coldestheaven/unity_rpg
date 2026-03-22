using UnityEngine;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// 暂停菜单控制器
    /// </summary>
    public class PauseMenuController : UIPanel
    {
        [Header("按钮")]
        public GameObject resumeButton;
        public GameObject settingsButton;
        public GameObject saveButton;
        public GameObject loadButton;
        public GameObject quitButton;

        private GameManager gameManager;

        protected override void Start()
        {
            base.Start();
            gameManager = GameManager.Instance;
        }

        public override void ShowPanel(bool animate = true)
        {
            base.ShowPanel(animate);
            Time.timeScale = 0f;
        }

        public override void HidePanel(bool animate = true)
        {
            base.HidePanel(animate);
            Time.timeScale = 1f;
        }

        public void OnResumeClicked()
        {
            gameManager?.TogglePause();
        }

        public void OnSettingsClicked()
        {
            // TODO: 打开设置面板
            Debug.Log("Settings clicked");
        }

        public void OnSaveClicked()
        {
            gameManager?.SaveGame();
        }

        public void OnLoadClicked()
        {
            gameManager?.LoadGame();
        }

        public void OnQuitClicked()
        {
            gameManager?.ReturnToMainMenu();
        }
    }

    /// <summary>
    /// 游戏结束菜单控制器
    /// </summary>
    public class GameOverMenuController : UIPanel
    {
        [Header("信息显示")]
        public UnityEngine.UI.Text scoreText;
        public UnityEngine.UI.Text levelText;
        public UnityEngine.UI.Text goldText;

        [Header("按钮")]
        public GameObject restartButton;
        public GameObject quitButton;

        private GameManager gameManager;

        protected override void Start()
        {
            base.Start();
            gameManager = GameManager.Instance;

            // 监听游戏结束事件
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("PlayerDied", OnPlayerDied);
            }
        }

        private void OnPlayerDied(object[] args)
        {
            ShowPanel();
            UpdateGameOverInfo();
        }

        private void UpdateGameOverInfo()
        {
            if (scoreText != null && gameManager != null)
            {
                scoreText.text = $"经验: {gameManager.playerExperience:F0}";
            }

            if (levelText != null && gameManager != null)
            {
                levelText.text = $"等级: {gameManager.playerLevel}";
            }

            if (goldText != null && gameManager != null)
            {
                goldText.text = $"金币: {gameManager.gold}";
            }
        }

        public void OnRestartClicked()
        {
            gameManager?.RestartGame();
        }

        public void OnQuitClicked()
        {
            gameManager?.ReturnToMainMenu();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("PlayerDied", OnPlayerDied);
            }
        }
    }

    /// <summary>
    /// 升级提示控制器
    /// </summary>
    public class LevelUpNotification : UIPanel
    {
        [Header("UI元素")]
        public UnityEngine.UI.Text levelText;
        public UnityEngine.UI.Text messageText;
        public float autoHideDelay = 2f;

        private Coroutine autoHideCoroutine;

        protected override void Start()
        {
            base.Start();

            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("PlayerLevelUp", OnPlayerLevelUp);
            }
        }

        private void OnPlayerLevelUp(object[] args)
        {
            int level = 1;
            if (args != null && args.Length > 0 && args[0] is int levelData)
            {
                level = levelData;
            }

            ShowLevelUp(level);
        }

        public void ShowLevelUp(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"升级到 Lv.{level}!";
            }

            if (messageText != null)
            {
                messageText.text = "能力提升!";
            }

            ShowPanel();

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHide());
        }

        private System.Collections.IEnumerator AutoHide()
        {
            yield return new WaitForSecondsRealtime(autoHideDelay);
            HidePanel();
        }

        public override void HidePanel(bool animate = true)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            base.HidePanel(animate);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("PlayerLevelUp", OnPlayerLevelUp);
            }
        }
    }
}
