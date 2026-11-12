using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Panels")]
    public GameObject controlsPanel;
    public GameObject hazardsPanel;
    public GameObject mineDisarmingPanel;
    public GameObject winConditionPanel;

    [Header("Scene Transition")]
    public string nextSceneName = "Intro Scene";

    private void Start()
    {
        ShowPanel(controlsPanel);
    
        // Reset to the default system arrow
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        ShowPanel(controlsPanel);
    
}

    public void GoToControls()
    {
        ShowPanel(controlsPanel);
    }

    public void GoToHazards()
    {
        ShowPanel(hazardsPanel);
    }

    public void GoToMineDisarming()
    {
        ShowPanel(mineDisarmingPanel);
    }

    public void GoToWinCondition()
    {
        ShowPanel(winConditionPanel);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Intro Scene");
    }

    private void ShowPanel(GameObject panelToActivate)
    {
        if (controlsPanel != null) controlsPanel.SetActive(panelToActivate == controlsPanel);
        if (hazardsPanel != null) hazardsPanel.SetActive(panelToActivate == hazardsPanel);
        if (mineDisarmingPanel != null) mineDisarmingPanel.SetActive(panelToActivate == mineDisarmingPanel);
        if (winConditionPanel != null) winConditionPanel.SetActive(panelToActivate == winConditionPanel);
    }
}