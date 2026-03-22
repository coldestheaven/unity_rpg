using System;

namespace Managers
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public class GameStateManager : Framework.Base.SingletonMonoBehaviour<GameStateManager>
    {
        public event Action<GameState> OnStateChanged;

        private GameState currentState;

        public GameState CurrentState => currentState;

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"Game state changed from {currentState} to {newState}");
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public bool IsState(GameState state) => currentState == state;

        public bool CanPause() => currentState == GameState.Playing;
        public bool CanInteract() => currentState == GameState.Playing;
        public bool IsGameOver() => currentState == GameState.GameOver;
    }
}
