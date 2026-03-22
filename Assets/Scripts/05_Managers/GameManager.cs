using UnityEngine;

namespace Managers
{
    public class GameManager : Framework.Base.SingletonMonoBehaviour<GameManager>
    {
        [Header("Game Settings")]
        [SerializeField] private bool isGamePaused = false;
        [SerializeField] private int targetFrameRate = 60;

        public bool IsGamePaused => isGamePaused;
        public bool IsGameRunning => !isGamePaused && Time.timeScale > 0;

        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = targetFrameRate;
        }

        protected override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void PauseGame()
        {
            if (isGamePaused) return;

            isGamePaused = true;
            Time.timeScale = 0f;
            Framework.Events.EventManager.Instance.TriggerEvent(Framework.Events.GameEvents.GAME_PAUSED);
            Debug.Log("Game Paused");
        }

        public void ResumeGame()
        {
            if (!isGamePaused) return;

            isGamePaused = false;
            Time.timeScale = 1f;
            Framework.Events.EventManager.Instance.TriggerEvent(Framework.Events.GameEvents.GAME_RESUMED);
            Debug.Log("Game Resumed");
        }

        public void TogglePause()
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
