using UnityEngine;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// UI管理器 - 统一管理所有UI面板
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("HUD")]
        public HUDController hudController;

        [Header("菜单")]
        public PauseMenuController pauseMenu;
        public GameOverMenuController gameOverMenu;
        public LevelUpNotification levelUpNotification;

        [Header("背包")]
        public InventoryUIController inventoryUI;

        [Header("快捷键")]
        public KeyCode inventoryKey = KeyCode.I;
        public KeyCode menuKey = KeyCode.Escape;

        private bool inventoryOpen = false;
        private bool menuOpen = false;

        protected override void Awake()
        {
            base.Awake();
            InitializeUI();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            // 确保HUD始终显示
            if (hudController != null)
            {
                hudController.gameObject.SetActive(true);
            }

            // 隐藏其他面板
            if (pauseMenu != null)
            {
                pauseMenu.HidePanel(false);
            }

            if (gameOverMenu != null)
            {
                gameOverMenu.HidePanel(false);
            }

            if (inventoryUI != null)
            {
                inventoryUI.HidePanel(false);
            }

            if (levelUpNotification != null)
            {
                levelUpNotification.HidePanel(false);
            }
        }

        private void SubscribeToEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("ShowPauseMenu", OnShowPauseMenu);
                EventManager.Instance.AddListener("HidePauseMenu", OnHidePauseMenu);
                EventManager.Instance.AddListener("ToggleInventory", OnToggleInventory);
                EventManager.Instance.AddListener("ShowLevelUp", OnShowLevelUp);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("ShowPauseMenu", OnShowPauseMenu);
                EventManager.Instance.RemoveListener("HidePauseMenu", OnHidePauseMenu);
                EventManager.Instance.RemoveListener("ToggleInventory", OnToggleInventory);
                EventManager.Instance.RemoveListener("ShowLevelUp", OnShowLevelUp);
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // 暂停菜单
            if (Input.GetKeyDown(menuKey))
            {
                TogglePauseMenu();
            }

            // 背包
            if (Input.GetKeyDown(inventoryKey))
            {
                ToggleInventory();
            }
        }

        #region Panel Control

        /// <summary>
        /// 切换暂停菜单
        /// </summary>
        public void TogglePauseMenu()
        {
            if (gameOverMenu != null && gameOverMenu.IsVisible)
            {
                return; // 游戏结束时不显示暂停菜单
            }

            if (pauseMenu != null)
            {
                if (pauseMenu.IsVisible)
                {
                    HidePauseMenu();
                }
                else
                {
                    ShowPauseMenu();
                }
            }
        }

        public void ShowPauseMenu()
        {
            if (pauseMenu != null && !pauseMenu.IsVisible)
            {
                pauseMenu.ShowPanel();
                menuOpen = true;

                if (inventoryOpen)
                {
                    HideInventory();
                }
            }
        }

        public void HidePauseMenu()
        {
            if (pauseMenu != null && pauseMenu.IsVisible)
            {
                pauseMenu.HidePanel();
                menuOpen = false;
            }
        }

        /// <summary>
        /// 切换背包
        /// </summary>
        public void ToggleInventory()
        {
            if (inventoryUI != null)
            {
                if (inventoryUI.IsVisible)
                {
                    HideInventory();
                }
                else
                {
                    ShowInventory();
                }
            }
        }

        public void ShowInventory()
        {
            if (inventoryUI != null && !inventoryUI.IsVisible)
            {
                inventoryUI.ShowPanel();
                inventoryOpen = true;

                // 如果暂停菜单打开,不关闭它
            }
        }

        public void HideInventory()
        {
            if (inventoryUI != null && inventoryUI.IsVisible)
            {
                inventoryUI.HidePanel();
                inventoryOpen = false;
            }
        }

        /// <summary>
        /// 显示游戏结束菜单
        /// </summary>
        public void ShowGameOverScreen()
        {
            if (gameOverMenu != null)
            {
                gameOverMenu.ShowPanel();

                // 隐藏其他面板
                if (pauseMenu != null && pauseMenu.IsVisible)
                {
                    pauseMenu.HidePanel(false);
                }

                if (inventoryUI != null && inventoryUI.IsVisible)
                {
                    inventoryUI.HidePanel(false);
                }
            }
        }

        public void HideGameOverScreen()
        {
            if (gameOverMenu != null && gameOverMenu.IsVisible)
            {
                gameOverMenu.HidePanel();
            }
        }

        /// <summary>
        /// 显示升级提示
        /// </summary>
        public void ShowLevelUpEffect(int level)
        {
            if (levelUpNotification != null)
            {
                levelUpNotification.ShowLevelUp(level);
            }
        }

        #endregion

        #region Event Handlers

        private void OnShowPauseMenu(object[] args)
        {
            ShowPauseMenu();
        }

        private void OnHidePauseMenu(object[] args)
        {
            HidePauseMenu();
        }

        private void OnToggleInventory(object[] args)
        {
            ToggleInventory();
        }

        private void OnShowLevelUp(object[] args)
        {
            int level = 1;
            if (args != null && args.Length > 0 && args[0] is int levelData)
            {
                level = levelData;
            }

            ShowLevelUpEffect(level);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新玩家状态UI
        /// </summary>
        public void UpdatePlayerStats()
        {
            if (hudController != null)
            {
                hudController.UpdateHUD();
            }
        }

        /// <summary>
        /// 更新经验条
        /// </summary>
        public void UpdateExperience(float current, float max)
        {
            if (hudController != null)
            {
                hudController.UpdateExperienceUI(current, max);
            }
        }

        /// <summary>
        /// 显示状态图标
        /// </summary>
        public void ShowStatusIcon(int index)
        {
            if (hudController != null)
            {
                hudController.ShowStatusIcon(index);
            }
        }

        public void HideStatusIcon(int index)
        {
            if (hudController != null)
            {
                hudController.HideStatusIcon(index);
            }
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
    }
}
