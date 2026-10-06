using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    public bool isDead = false;

    [Header("Respawn Reference")]
    [SerializeField] private PlayerRespawn playerRespawn;

    // PascalCase Properties expected by HeartsUI & LavaHazards
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    // Event expected by HeartsUI
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        if (playerRespawn == null)
            playerRespawn = GetComponent<PlayerRespawn>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        TileVisualizer tile = collision.gameObject.GetComponent<TileVisualizer>();
        if (tile == null)
        {
            tile = collision.gameObject.GetComponentInParent<TileVisualizer>();
        }

        // STEPPED ON A MINE (Ignores detonated AND disarmed mines)
        if (tile != null && tile.isMine && !tile.isDetonated && !tile.isDisarmed)
        {
            Debug.Log($"[BOOM] Player stepped on active hidden mine at {tile.transform.position}!");
            tile.Detonate();

            // Deal ONLY 1 point of damage
            TakeDamage(1);

            // If still alive, respawn at active checkpoint & reset mines
            if (currentHealth > 0)
            {
                if (playerRespawn != null)
                {
                    playerRespawn.Respawn();
                }
                else
                {
                    Debug.LogWarning("[PlayerHealth] PlayerRespawn component reference is missing!");
                }
            }
        }
    }

    public bool TakeDamage(int damage)
    {
        if (isDead) return false;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Player took {damage} damage! Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }

        return true;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died! Game Over.");

        MinerController controller = GetComponent<MinerController>();
        if (controller != null)
        {
            controller.TriggerDeath();
        }

        OnDied?.Invoke();
    }
}