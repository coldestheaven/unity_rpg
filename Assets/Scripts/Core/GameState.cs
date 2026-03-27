using UnityEngine;
using Framework.Events;

namespace RPG.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        MainMenu,       // 主菜单
        Loading,        // 加载中
        Playing,        // 游戏中
        Paused,         // 暂停
        GameOver,       // 游戏结束
        Victory,        // 胜利
        Cutscene        // 过场动画
    }

    /// <summary>
    /// 游戏状态管理器
    /// </summary>
    public class GameStateManager : Singleton<GameStateManager>
    {
        private GameState currentState;
        private GameState previousState;
        private Managers.GameStateManager runtimeStateManager;

        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<GameState, GameState> OnStateTransition;

        protected override void Awake()
        {
            base.Awake();
            runtimeStateManager = Managers.GameStateManager.Instance;

            if (runtimeStateManager != null)
            {
                runtimeStateManager.OnStateChanged += HandleRuntimeStateChanged;
                HandleRuntimeStateChanged(runtimeStateManager.CurrentState);
            }
            else
            {
                SetState(GameState.MainMenu);
            }
        }

        private void OnDestroy()
        {
            if (runtimeStateManager != null)
            {
                runtimeStateManager.OnStateChanged -= HandleRuntimeStateChanged;
            }
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            if (runtimeStateManager != null && TryMapToRuntime(newState, out Managers.GameState runtimeState))
            {
                runtimeStateManager.ChangeState(runtimeState);
                return;
            }

            ApplyState(newState);
        }

        private void HandleRuntimeStateChanged(Managers.GameState newState)
        {
            ApplyState(MapFromRuntime(newState));
        }

        private void ApplyState(GameState newState)
        {
            if (currentState == newState) return;

            GameState oldState = currentState;
            previousState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(newState);
            OnStateTransition?.Invoke(oldState, newState);

            EventManager.Instance?.TriggerEvent("GameStateChanged", new GameStateChangedEventArgs
            {
                oldState = oldState,
                newState = newState
            });

            Debug.Log($"Game state changed: {oldState} -> {newState}");
        }

        private bool TryMapToRuntime(GameState state, out Managers.GameState runtimeState)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    runtimeState = Managers.GameState.MainMenu;
                    return true;
                case GameState.Loading:
                    runtimeState = Managers.GameState.Loading;
                    return true;
                case GameState.Playing:
                    runtimeState = Managers.GameState.Playing;
                    return true;
                case GameState.Paused:
                    runtimeState = Managers.GameState.Paused;
                    return true;
                case GameState.GameOver:
                    runtimeState = Managers.GameState.GameOver;
                    return true;
                case GameState.Victory:
                    runtimeState = Managers.GameState.Victory;
                    return true;
                default:
                    runtimeState = Managers.GameState.Playing;
                    return false;
            }
        }

        private GameState MapFromRuntime(Managers.GameState state)
        {
            switch (state)
            {
                case Managers.GameState.MainMenu:
                    return GameState.MainMenu;
                case Managers.GameState.Loading:
                    return GameState.Loading;
                case Managers.GameState.Playing:
                    return GameState.Playing;
                case Managers.GameState.Paused:
                    return GameState.Paused;
                case Managers.GameState.GameOver:
                    return GameState.GameOver;
                case Managers.GameState.Victory:
                    return GameState.Victory;
                default:
                    return GameState.Playing;
            }
        }

        /// <summary>
        /// 恢复到上一个状态
        /// </summary>
        public void RestorePreviousState()
        {
            if (previousState != GameState.MainMenu)
            {
                SetState(previousState);
            }
        }

        /// <summary>
        /// 检查是否处于指定状态
        /// </summary>
        public bool IsInState(GameState state)
        {
            return currentState == state;
        }

        /// <summary>
        /// 检查是否可以暂停
        /// </summary>
        public bool CanPause()
        {
            return currentState == GameState.Playing || currentState == GameState.Paused;
        }

        /// <summary>
        /// 检查是否可以进行游戏操作
        /// </summary>
        public bool CanPlayerAct()
        {
            return currentState == GameState.Playing;
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            SetState(GameState.Playing);
            EventManager.Instance?.TriggerEvent("GameStarted", null);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                Managers.GameManager.Instance?.PauseGame();
                SetState(GameState.Paused);
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                Managers.GameManager.Instance?.ResumeGame();
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        public void EndGame()
        {
            SetState(GameState.GameOver);
            EventManager.Instance?.TriggerEvent("GameEnded", null);
        }

        /// <summary>
        /// 游戏胜利
        /// </summary>
        public void Victory()
        {
            SetState(GameState.Victory);
            EventManager.Instance?.TriggerEvent("GameVictory", null);
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            SetState(GameState.MainMenu);
            Time.timeScale = 1f;
            EventManager.Instance?.TriggerEvent("ReturnToMainMenu", null);
        }
    }

    [System.Serializable]
    public class GameStateChangedEventArgs
    {
        public GameState oldState;
        public GameState newState;
    }
}
