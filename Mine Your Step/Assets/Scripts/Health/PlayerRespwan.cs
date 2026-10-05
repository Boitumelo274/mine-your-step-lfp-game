using System.Collections;
using UnityEngine;

/// <summary>
/// Goes on the player. Remembers where the player started and the last checkpoint touched.
/// Respawn() plays a cinematic sequence: short pause, fade to black, teleport while the
/// screen is dark, resets/reshuffles mines & re-scans hazards, holds, then fades back in.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    public enum TransitionStyle { SmoothFade, RadialIris }

    [Header("Cinematic Respawn")]
    [Tooltip("SmoothFade = whole screen fades to colour. RadialIris = a circle closes in and opens back out.")]
    [SerializeField] private TransitionStyle transitionStyle = TransitionStyle.RadialIris;
    [Tooltip("RadialIris only: close/open the circle around the player instead of the screen centre.")]
    [SerializeField] private bool irisFollowsPlayer = true;
    [Tooltip("Pause after the hit (lets the lava explosion play) before the screen starts to darken.")]
    [SerializeField, Min(0f)] private float delayBeforeFade = 0.4f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.5f;
    [Tooltip("How long the screen stays black. The camera catches up to the checkpoint during this time, so raise it if the camera still visibly slides on screen.")]
    [SerializeField, Min(0f)] private float blackHoldTime = 0.8f;
    [SerializeField, Min(0f)] private float fadeInTime = 0.7f;
    [Tooltip("Colour of the screen during the respawn. Default is a dark charcoal instead of pure black.")]
    [SerializeField] private Color fadeColor = new Color(0.10f, 0.10f, 0.12f, 1f);

    [Tooltip("Drag your player control scripts here (e.g. MinerController). They're switched off from the hit until the fade-in begins, so the player can't move in the dark.")]
    [SerializeField] private Behaviour[] disableWhileRespawning;

    private Vector3 startPosition;
    private Checkpoint currentCheckpoint;
    private bool isRespawning;
    private Rigidbody2D rb;

    public Checkpoint CurrentCheckpoint => currentCheckpoint;
    public bool IsRespawning => isRespawning;

    private void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Called by a Checkpoint when the player touches it.</summary>
    public void SetCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == currentCheckpoint) return;

        if (currentCheckpoint != null) currentCheckpoint.SetActiveVisual(false);
        currentCheckpoint = checkpoint;
        currentCheckpoint.SetActiveVisual(true);
    }

    /// <summary>Cinematic respawn at the last checkpoint (or the start if none reached).</summary>
    public void Respawn()
    {
        if (isRespawning) return;
        StartCoroutine(RespawnRoutine());
    }

    /// <summary>Teleports straight to the respawn point with no fade (e.g. for debugging).</summary>
    public void RespawnInstant()
    {
        Teleport();
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        ScreenFader fader = ScreenFader.Get();
        if (fader != null) fader.SetColor(fadeColor);

        // Freeze the player right away so they can't walk or fall around in the lava.
        SetControl(false);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Unity 6. On older Unity use rb.velocity
            rb.simulated = false;
        }

        yield return new WaitForSeconds(delayBeforeFade);

        if (fader != null)
        {
            if (transitionStyle == TransitionStyle.RadialIris)
                yield return fader.IrisClose(fadeOutTime, GetIrisCenter());
            else
                yield return fader.FadeTo(1f, fadeOutTime);
        }

        // --- SCREEN IS BLACK: TELEPORT, RESHUFFLE MINES, RE-SCAN HAZARDS ---
        Teleport();

        yield return new WaitForSeconds(blackHoldTime);

        // Give control back as the light returns.
        if (rb != null) rb.simulated = true;
        SetControl(true);

        if (fader != null)
        {
            if (transitionStyle == TransitionStyle.RadialIris)
                yield return fader.IrisOpen(fadeInTime, GetIrisCenter());
            else
                yield return fader.FadeTo(0f, fadeInTime);
        }

        isRespawning = false;
    }

    // Where the circle is centred, in screen pixels.
    private Vector2 GetIrisCenter()
    {
        Camera cam = Camera.main;
        if (!irisFollowsPlayer || cam == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3 p = cam.WorldToScreenPoint(transform.position);
        return new Vector2(p.x, p.y);
    }

    private void Teleport()
    {
        Vector3 target = currentCheckpoint != null ? currentCheckpoint.SpawnPosition : startPosition;
        transform.position = target;

        if (rb != null)
        {
            rb.position = target;
            rb.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();

        // 1. Reshuffle and place 15 fresh hidden mines across the level
        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGridSystem();
        }

        // 2. Re-trigger hazard scan around the active checkpoint while screen is black
        if (currentCheckpoint != null)
        {
            currentCheckpoint.ScanAreaForHazards();
        }
        else if (GridManager.Instance != null)
        {
            GridManager.Instance.HandlePlatformImpact(startPosition);
        }
    }

    private void SetControl(bool enabled)
    {
        foreach (Behaviour b in disableWhileRespawning)
        {
            if (b != null) b.enabled = enabled;
        }
    }
}