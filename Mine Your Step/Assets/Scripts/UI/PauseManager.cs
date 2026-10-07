using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    public GameObject controlsPanel;

    [Header("In-Game UI to Hide")]
    public GameObject[] hudElements; // Array to hold health bars, scores, etc.

    private bool isPaused = false;

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    private void Update()
    {
        // Detects the Escape key using the New Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze game physics and animations
        pauseMenuPanel.SetActive(true);

        // Hide all assigned in-game UI elements
        foreach (GameObject ui in hudElements)
        {
            if (ui != null) ui.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreeze the game
        pauseMenuPanel.SetActive(false);
        controlsPanel.SetActive(false);

        // Show the in-game UI elements again
        foreach (GameObject ui in hudElements)
        {
            if (ui != null) ui.SetActive(true);
        }
    }

    public void OpenControls()
    {
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // MUST unfreeze time before loading a new scene
        SceneManager.LoadScene(0); // Loads the Main Menu (Index 0 in Build Settings)
    }
}