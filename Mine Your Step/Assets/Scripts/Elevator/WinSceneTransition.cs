using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class WinSceneTransition : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Exact name of the win scene.")]
    [SerializeField] private string winSceneName = "WinScene";

    [Header("Iris Transition")]
    [Tooltip("Pause after the player touches the trigger, before the circle starts closing.")]
    [SerializeField, Min(0f)] private float delayBeforeIris = 0.3f;
    [Tooltip("How long the circle takes to close completely. The scene only loads after this finishes.")]
    [SerializeField, Min(0.05f)] private float irisCloseTime = 2f;
    [Tooltip("How long the screen stays fully dark before the new scene loads.")]
    [SerializeField, Min(0f)] private float blackHoldTime = 0.5f;
    [Tooltip("Close the circle around the player instead of the screen centre.")]
    [SerializeField] private bool irisFollowsPlayer = true;
    [SerializeField] private Color fadeColor = new Color(0.10f, 0.10f, 0.12f, 1f);

    private bool started;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started) return;

        MinerController miner = other.GetComponentInParent<MinerController>();
        if (miner == null) return;

        started = true;
        StartCoroutine(TransitionRoutine(miner));
    }

    private IEnumerator TransitionRoutine(MinerController miner)
    {
        ScreenFader fader = ScreenFader.Get();
        if (fader != null) fader.SetColor(fadeColor);

        // Iris centre is taken once, at the moment the player touches the trigger.
        Vector2 center = GetIrisCenter(miner.transform);

        // Phase 1: short pause. The player is kept frozen, because the elevator script
        // would otherwise give control back when it arrives at the top.
        float t = 0f;
        while (t < delayBeforeIris)
        {
            miner.enabled = false;
            t += Time.deltaTime;
            yield return null;
        }

        // Phase 2: the iris closes. Wait for it to finish completely.
        if (fader != null)
        {
            miner.enabled = false;
            yield return StartCoroutine(HoldPlayerWhile(fader.IrisClose(irisCloseTime, center), miner));
        }

        // Phase 3: screen is fully dark, hold briefly, then load.
        t = 0f;
        while (t < blackHoldTime)
        {
            miner.enabled = false;
            t += Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(winSceneName);
    }

    // Runs the iris coroutine while keeping the player's controls switched off every frame.
    private IEnumerator HoldPlayerWhile(IEnumerator routine, MinerController miner)
    {
        while (routine.MoveNext())
        {
            miner.enabled = false;
            yield return routine.Current;
        }
    }

    private Vector2 GetIrisCenter(Transform player)
    {
        Camera cam = Camera.main;
        if (!irisFollowsPlayer || cam == null || player == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3 p = cam.WorldToScreenPoint(player.position);
        return new Vector2(p.x, p.y);
    }
}