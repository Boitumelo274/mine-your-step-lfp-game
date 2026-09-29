using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drop this on any object with a Collider2D (set to Is Trigger) and place as many as you like.
/// Touching one makes it the player's respawn point. No numbering or lists to maintain.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Where the player appears, relative to this object. (0, 0.5) puts them just above it.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f);

    [Header("Feedback (all optional)")]
    [SerializeField] private SpriteRenderer spriteToTint;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.white;
    [Tooltip("Runs the first time the player reaches this checkpoint. Hook up a sound or particles here.")]
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
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.SetCheckpoint(this);

        if (!hasBeenReached)
        {
            hasBeenReached = true;
            onFirstActivated?.Invoke();
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
    }
}