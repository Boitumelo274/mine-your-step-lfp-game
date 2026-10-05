using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Setup")]
    [Tooltip("Where the player appears relative to this flag.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f);

    [Header("Scanner Area Settings")]
    [Tooltip("How far out this flag scans for platform tiles to reveal cracks/hazards.")]
    [SerializeField] private float scanRadius = 18.0f;
    [Tooltip("The layer assigned to your platform/rock tiles.")]
    [SerializeField] private LayerMask platformLayer;

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

    private void Start()
    {
        SetActiveVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Boitumelo's Respawn Hook
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.SetCheckpoint(this);

        if (!hasBeenReached)
        {
            hasBeenReached = true;
            SetActiveVisual(true);
            onFirstActivated?.Invoke();
        }

        // 2. Tiara's Scanner System Execution
        ScanAreaForHazards();
    }

    public void ScanAreaForHazards()
    {
        Collider2D[] platforms = Physics2D.OverlapCircleAll(transform.position, scanRadius, platformLayer);

        foreach (var col in platforms)
        {
            TileVisualizer tile = col.GetComponent<TileVisualizer>();
            if (tile != null)
            {
                tile.ExposeVisualClues();
            }
        }

        Debug.Log($"[Scanner Flag] Scanned area! Visual clues revealed on {platforms.Length} tiles.");
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
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}