using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class pauseMenu : MonoBehaviour
{
    public static bool GameIsPaused { get; private set; }
    public GameObject pauseMenuUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetPauseState()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;
    }

    void Awake()
    {
        ResetPauseState();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != GameFlow.LevelScene) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        if (pressed)
        {
            if (GameIsPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        ResetPauseState();
    }

    public void Pause()
    {
        if (pauseMenuUI == null) return;
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
            pauseMenuUI.GetComponentInChildren<UnityEngine.UI.Button>()?.gameObject);
    }

    public void MainMenu() => GameFlow.LoadScene(GameFlow.MenuScene);

    void OnDestroy() => ResetPauseState();
}
