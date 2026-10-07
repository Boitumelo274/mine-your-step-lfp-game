using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("The miner's Animator. Used to wait until the death animation has finished.")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("The whole panel (dark background). Needs a CanvasGroup.")]
    [SerializeField] private CanvasGroup panelGroup;
    [Tooltip("Holds the title, message and buttons. Needs a CanvasGroup. Fades in after the background.")]
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button retryButton;
    [Tooltip("Optional.")]
    [SerializeField] private Button quitButton;

    [Header("Text")]
    [SerializeField] private string title = "THE MINE CLAIMED YOU";
    [Tooltip("One of these is picked at random each time.")]
    [SerializeField, TextArea]
    private string[] messages =
    {
        "You should have watched your step.",
        "Every mine hides something. This one hid you.",
        "The dark below keeps what it takes.",
        "One wrong step is all it ever takes.",
        "The tunnel is quiet again."
    };

    [Header("Timing")]
    [Tooltip("Name of the death state in your Animator Controller.")]
    [SerializeField] private string deathStateName = "Die";
    [Tooltip("Extra pause after the animation ends, before anything appears.")]
    [SerializeField, Min(0f)] private float delayAfterAnimation = 0.6f;
    [Tooltip("Safety limit so the panel still appears if the animation state is never found.")]
    [SerializeField, Min(0.5f)] private float maxWaitForAnimation = 5f;
    [SerializeField, Min(0.1f)] private float panelFadeTime = 1.2f;
    [SerializeField, Min(0f)] private float contentDelay = 0.5f;
    [SerializeField, Min(0.1f)] private float contentFadeTime = 0.8f;
    [Tooltip("The title starts this much bigger and settles down to normal size as it fades in.")]
    [SerializeField, Min(1f)] private float titleStartScale = 1.25f;
    [SerializeField, Min(0f)] private float retryFadeTime = 0.6f;

    private bool shown;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerAnimator == null && playerHealth != null) playerAnimator = playerHealth.GetComponent<Animator>();

        panelGroup.alpha = 0f;
        contentGroup.alpha = 0f;
        SetInteractive(false);

        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
    }

    private void Update()
    {
        if (shown || playerHealth == null) return;

        if (playerHealth.IsDead)
        {
            shown = true;
            StartCoroutine(ShowSequence());
        }
    }

    private IEnumerator ShowSequence()
    {
        panelGroup.gameObject.SetActive(true);

        // 1. Wait for the death animation to finish.
        if (playerAnimator != null)
        {
            float waited = 0f;
            while (waited < maxWaitForAnimation)
            {
                AnimatorStateInfo info = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(deathStateName) && info.normalizedTime >= 1f) break;
                waited += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        yield return new WaitForSeconds(delayAfterAnimation);

        // 2. Pick the words.
        if (titleText != null) titleText.text = title;
        if (messageText != null && messages != null && messages.Length > 0)
            messageText.text = messages[Random.Range(0, messages.Length)];

        // 3. Fade the background in, then the content a moment later.
        float total = Mathf.Max(panelFadeTime, contentDelay + contentFadeTime);
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;

            panelGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / panelFadeTime));

            float c = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - contentDelay) / contentFadeTime));
            contentGroup.alpha = c;
            if (titleRect != null) titleRect.localScale = Vector3.one * Mathf.Lerp(titleStartScale, 1f, c);

            yield return null;
        }
        panelGroup.alpha = 1f;
        contentGroup.alpha = 1f;
        if (titleRect != null) titleRect.localScale = Vector3.one;

        // 4. Now the buttons can be clicked.
        SetInteractive(true);
        if (retryButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    private void SetInteractive(bool value)
    {
        panelGroup.interactable = value;
        panelGroup.blocksRaycasts = value;
        contentGroup.interactable = value;
        contentGroup.blocksRaycasts = value;
    }

    private void OnRetry()
    {
        SetInteractive(false);
        StartCoroutine(RetryRoutine());
    }

    private IEnumerator RetryRoutine()
    {
        yield return ScreenFader.Get().FadeTo(1f, retryFadeTime);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}