using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject controlsPanel;
    public GameObject hazardsPanel;

    private void Start()
    {
        // Start by showing only the Controls panel
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (hazardsPanel != null) hazardsPanel.SetActive(false);
    }

    public void GoToHazards()
    {
        // Hide controls, show hazards
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (hazardsPanel != null) hazardsPanel.SetActive(true);
    }

    public void PlayGame()
    {
        // Load the main level
        SceneManager.LoadScene("MainScene");
    }
}