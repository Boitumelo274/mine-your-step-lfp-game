using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
public class NarrativeIntro : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "MainScene";

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Automatically triggers when the video finishes playing
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    // Attach this method to your Skip button's On Click event
    public void SkipVideo()
    {
        LoadNextScene();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}