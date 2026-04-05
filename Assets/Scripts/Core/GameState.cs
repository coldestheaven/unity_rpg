using UnityEngine;
using Framework.Events;

namespace RPG.Core
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Victory,
        Cutscene
    }

    /// <summary>
    /// 自包含的游戏状态管理器。
    /// 负责状态转换、Time.timeScale 以及状态变更广播。
    /// </summary>
    public class GameStateManager : Singleton<GameStateManager>
    {
        private GameState _current;
        private GameState _previous;

        public GameState CurrentState  => _current;
        public GameState PreviousState => _previous;

        public event System.Action<GameState>             OnStateChanged;
        public event System.Action<GameState, GameState>  OnStateTransition;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetState(GameState newState)
        {
            if (_current == newState) return;

            GameState old = _current;
            _previous = _current;
            _current  = newState;

            ApplyTimeScale(newState);

            OnStateChanged?.Invoke(newState);
            OnStateTransition?.Invoke(old, newState);

            EventBus.Publish(new GameStateChangedEvent { OldState = old.ToString(), NewState = newState.ToString() });

            Debug.Log($"[GameState] {old} → {newState}");
        }

        public void StartGame()
        {
            SetState(GameState.Playing);
            EventBus.Publish(new GameStartedEvent());
        }

        public void PauseGame()
        {
            if (_current == GameState.Playing)
                SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (_current == GameState.Paused)
                SetState(GameState.Playing);
        }

        public void EndGame()
        {
            SetState(GameState.GameOver);
            EventBus.Publish(new GameEndedEvent());
        }

        public void Victory()
        {
            SetState(GameState.Victory);
            EventBus.Publish(new GameVictoryEvent());
        }

        public void ReturnToMainMenu()
        {
            SetState(GameState.MainMenu);
            EventBus.Publish(new ReturnToMainMenuEvent());
        }

        public void RestorePreviousState()
        {
            if (_previous != GameState.MainMenu)
                SetState(_previous);
        }

        // ── Query helpers ─────────────────────────────────────────────────────

        public bool IsInState(GameState state)  => _current == state;
        public bool CanPause()                  => _current == GameState.Playing;
        public bool CanPlayerAct()              => _current == GameState.Playing;
        /// <summary>别名，供仍使用旧名称的代码调用。</summary>
        public bool CanInteract()               => _current == GameState.Playing;

        // ── Internal ──────────────────────────────────────────────────────────

        private static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }
    }

}
