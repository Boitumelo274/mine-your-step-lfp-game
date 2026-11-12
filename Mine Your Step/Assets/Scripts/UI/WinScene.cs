using UnityEngine;
using UnityEngine.SceneManagement;

public class WinSceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string mainMenuScene = "Main Menu";
    public string mainGameScene = "MainScene";

    private void Start()
    {
        // Ensure normal game speed and default mouse cursor on the win screen
        Time.timeScale = 1f;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(mainGameScene);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}