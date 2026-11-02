using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class pauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
        
    }
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale= 1f; 
        GameIsPaused = false; 
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale= 0f; 
        GameIsPaused = true; 
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }
}
