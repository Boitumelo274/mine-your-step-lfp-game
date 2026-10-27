using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Setup")]
    [Tooltip("Where the player appears relative to this flag.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f);

    [Header("Scanner Area Settings")]
    [Tooltip("GridManager reference. Auto-finds in scene if left unassigned.")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ScreenShake screenShake;

    [Header("Audio")]
    [SerializeField] private AudioClip activateSound;
    [SerializeField, Range(0f, 1f)] private float activateVolume = 1f;
    private AudioSource audioSource;

    [Header("Visual & Event Feedback")]
    [SerializeField] private SpriteRenderer spriteToTint;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private UnityEvent onFirstActivated;

    private bool hasBeenReached;

    public Vector3 SpawnPosition => transform.position + (Vector3)spawnOffset;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteToTint = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if(screenShake == null)
        {
            screenShake = FindFirstObjectByType<ScreenShake>();
        }

        audioSource = GetComponent<AudioSource>();
if (audioSource == null)
    audioSource = gameObject.AddComponent<AudioSource>();

audioSource.playOnAwake = false;
audioSource.spatialBlend = 0f; // 2D sound, so it's equally loud wherever the camera is
    }

    private void Start()
    {
        SetActiveVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
 
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.SetCheckpoint(this);

        if (!hasBeenReached)
        {
            hasBeenReached = true;
            SetActiveVisual(true);
            onFirstActivated?.Invoke();

            if (screenShake != null) screenShake.Shake(1f);
            if (activateSound != null) audioSource.PlayOneShot(activateSound, activateVolume);
        }

        ScanAreaForHazards();
    }

    // Allows clicking the flag directly with mouse to test/activate scan
    private void OnMouseDown()
    {
        if (!hasBeenReached)
        {
            hasBeenReached = true;
            SetActiveVisual(true);
            onFirstActivated?.Invoke();

            
        }

        ScanAreaForHazards();
    }

    public void ScanAreaForHazards()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            gridManager.HandlePlatformImpact(transform.position);
            Debug.Log($"[Scanner Flag] Scanning hazards from flag position {transform.position}");
        }
        else
        {
            Debug.LogError("[Scanner Flag] Cannot scan: GridManager reference missing!");
        }
    }

    public void SetActiveVisual(bool active)
    {
        if (spriteToTint != null) spriteToTint.color = active ? activeColor : inactiveColor;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(SpawnPosition, 0.25f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 40.0f);
    }
}