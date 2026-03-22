using UnityEngine;

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

        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<GameState, GameState> OnStateTransition;

        protected override void Awake()
        {
            base.Awake();
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        public void SetState(GameState newState)
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
                SetState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                Time.timeScale = 1f;
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
