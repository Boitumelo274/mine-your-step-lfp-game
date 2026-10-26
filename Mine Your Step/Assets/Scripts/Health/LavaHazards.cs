using UnityEngine;

/// <summary>
/// Put on the lava object (its Collider2D must have "Is Trigger" ticked).
/// When the player touches it: explosion effect plays, player loses hearts, and if the
/// player has a PlayerRespawn they are sent back to their last checkpoint.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LavaHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Optional. Particle/animation prefab spawned at the player when they hit the lava.")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifetime = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip lavaHitSound;
    [SerializeField, Range(0f, 1f)] private float lavaHitVolume = 1f;
    private AudioSource audioSource;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound, same volume wherever the camera is
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        // TakeDamage returns false during invincibility or when already dead,
        // so we only explode/respawn on hits that actually count.
        if (!health.TakeDamage(damage)) return;

        // Lava sound plays here, only on hits that count
        if (lavaHitSound != null) audioSource.PlayOneShot(lavaHitSound, lavaHitVolume);

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, health.transform.position, Quaternion.identity);
            Destroy(fx, explosionLifetime);
        }

        if (!health.IsDead)
        {
            PlayerRespawn respawn = health.GetComponentInParent<PlayerRespawn>();
            if (respawn != null) respawn.Respawn();
        }
    }
}