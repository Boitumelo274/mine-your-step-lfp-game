using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject controlsPanel;

    private void Start()
    {
        // Ensure the controls panel is hidden when the menu loads
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OpenControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }

    public void CloseControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!"); 
        Application.Quit();
    }
}