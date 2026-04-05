using UnityEngine;
using UnityEngine.SceneManagement;
using Framework.Events;
using Framework.Threading;
using Gameplay.Player;
using UI.Controllers;
using RPG.Simulation;

namespace RPG.Core
{
    /// <summary>
    /// 游戏管理器 - 重构版
    /// 使用模块化设计,职责分离
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("引用")]
        public GameStateManager stateManager;
        public PlayerProgressManager progressManager;
        public UIManager uiManager;

        [Header("游戏设置")]
        public float baseEnemySpawnRate = 5f;
        public int baseEnemyDamage = 10;

        // 兼容旧接口的属性(内部使用新的管理器)
        public int playerHealth => PlayerController.Instance != null ? Mathf.RoundToInt(PlayerController.Instance.Health.CurrentHealth) : 0;
        public int playerMaxHealth => PlayerController.Instance != null ? Mathf.RoundToInt(PlayerController.Instance.Health.MaxHealth) : 0;
        public int playerLevel => progressManager?.GetLevel() ?? 1;
        public float playerExperience => progressManager?.GetExperience() ?? 0f;
        public float experienceToNextLevel => progressManager?.GetExperienceToNextLevel() ?? 100f;
        public int gold => progressManager?.GetGold() ?? 0;

        private bool isInitialized = false;

        // Logic-layer simulation — runs on a background thread.
        private GameSimulation _simulation;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Ensure MainThreadDispatcher exists on the same persistent object so
                // the logic thread can safely marshal callbacks back to the main thread.
                if (GetComponent<MainThreadDispatcher>() == null)
                    gameObject.AddComponent<MainThreadDispatcher>();

                // Create and start the logic-layer simulation BEFORE any managers
                // so that PlayerProgressManager.Start() can bind to it.
                _simulation = new GameSimulation(skillSlotCount: 4, maxMana: 100f);
                _simulation.Start();

                InitializeManagers();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SubscribeToEvents();
            StartGame();
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // Stop the logic thread cleanly before the GameObject is destroyed.
            _simulation?.Dispose();
            _simulation = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeManagers()
        {
            if (stateManager == null)
            {
                stateManager = GameStateManager.Instance;
            }

            if (progressManager == null)
            {
                progressManager = PlayerProgressManager.Instance;
            }

            if (uiManager == null)
            {
                uiManager = UIManager.Instance;
            }

            isInitialized = true;
            Debug.Log("GameManager initialized");
        }

        private void SubscribeToEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener("PlayerDied", OnPlayerDied);
                EventManager.Instance.AddListener("GameVictory", OnGameVictory);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("PlayerDied", OnPlayerDied);
                EventManager.Instance.RemoveListener("GameVictory", OnGameVictory);
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        #endregion

        #region Game State Control

        public void StartGame()
        {
            if (stateManager != null)
            {
                stateManager.StartGame();
            }
        }

        public void TogglePause()
        {
            if (stateManager != null)
            {
                if (stateManager.CurrentState == GameState.Playing)
                {
                    stateManager.PauseGame();
                    uiManager?.ShowPauseMenu();
                }
                else if (stateManager.CurrentState == GameState.Paused)
                {
                    stateManager.ResumeGame();
                    uiManager?.HidePauseMenu();
                }
            }
        }

        public void GameOver()
        {
            if (stateManager != null)
            {
                stateManager.EndGame();
                uiManager?.ShowGameOverScreen();
            }
        }

        public void RestartGame()
        {
            ResetGameStats();

            if (stateManager != null)
            {
                stateManager.ResumeGame();
                stateManager.StartGame();
            }

            // 重新加载当前场景
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMainMenu()
        {
            if (stateManager != null)
            {
                stateManager.ReturnToMainMenu();
            }

            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #region Player Progress

        public void AddExperience(float amount)
        {
            progressManager?.AddExperience(amount);
        }

        public void AddGold(int amount)
        {
            progressManager?.AddGold(amount);
        }

        public void LevelUp()
        {
            // 由PlayerProgressManager处理
        }

        #endregion

        #region Player Health

        public void TakeDamage(int damage)
        {
            // 由PlayerHealth系统处理
        }

        public void Heal(int amount)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.Heal(amount);
            }
        }

        #endregion

        #region Save/Load

        public void SaveGame()
        {
            SaveSystem.Instance?.SaveGame();
            Debug.Log("Game saved");
        }

        public void LoadGame()
        {
            SaveSystem.Instance?.LoadGame();
            Debug.Log("Game loaded");
        }

        #endregion

        #region Game Reset

        public void ResetGameStats()
        {
            progressManager?.ResetProgress();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ResetPlayer();
            }

            Debug.Log("Game stats reset");
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied(object args)
        {
            GameOver();
        }

        private void OnGameVictory(object args)
        {
            if (stateManager != null)
            {
                stateManager.Victory();
            }
        }

        #endregion

        #region UI Updates

        public void ShowLevelUpEffect(int level)
        {
            uiManager?.ShowLevelUpEffect(level);
        }

        public void UpdatePlayerStats()
        {
            uiManager?.UpdatePlayerStats();
        }

        #endregion

        #region Legacy Interface (兼容旧代码)

        [Obsolete("Use stateManager.PauseGame() instead")]
        public bool isPaused => stateManager?.CurrentState == GameState.Paused ?? false;

        [Obsolete("Use stateManager.CurrentState == GameState.GameOver instead")]
        public bool isGameOver => stateManager?.CurrentState == GameState.GameOver ?? false;

        #endregion
    }
}
