using UnityEngine;

/// <summary>
/// Put on the lava object (its Collider2D must have "Is Trigger" ticked).
/// When the player touches it: explosion effect plays, player loses hearts, and (optionally)
/// is moved to a respawn point so they don't keep sitting in the lava.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LavaHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Optional. Particle/animation prefab spawned at the player when they hit the lava.")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifetime = 2f;
    [Tooltip("Optional. If set, the player is moved here after being hurt (and hearts remain lost).")]
    [SerializeField] private Transform respawnPoint;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        // TakeDamage returns false during invincibility or when already dead,
        // so we only explode/respawn on hits that actually count.
        if (!health.TakeDamage(damage)) return;

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, health.transform.position, Quaternion.identity);
            Destroy(fx, explosionLifetime);
        }

        if (respawnPoint != null && !health.IsDead)
        {
            health.transform.position = respawnPoint.position;

            Rigidbody2D rb = health.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}