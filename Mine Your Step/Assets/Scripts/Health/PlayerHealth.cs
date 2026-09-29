using System;
using UnityEngine;

/// <summary>
/// Holds the player's health as a whole number of hearts. Anything that hurts the player
/// (lava now, enemies later) just calls TakeDamage(). When health reaches 0, OnDied fires —
/// hook your death/respawn/game-over logic to that event when you build it.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 5;
    [Tooltip("Seconds after a hit during which further damage is ignored (stops one lava touch from costing several hearts).")]
    [SerializeField, Min(0f)] private float invincibilityTime = 1f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>(currentHealth, maxHealth)</summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary>Fires once when health hits 0. Connect your death logic here later.</summary>
    public event Action OnDied;

    private float lastHitTime = -999f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        // Push the starting value so any hearts UI draws itself correctly.
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <returns>true if the damage was actually applied.</returns>
    public bool TakeDamage(int amount = 1)
    {
        if (IsDead || amount <= 0) return false;
        if (Time.time - lastHitTime < invincibilityTime) return false;

        lastHitTime = Time.time;
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            Debug.Log("[PlayerHealth] Health reached 0 : death would trigger here.", this);
            OnDied?.Invoke();
        }
        return true;
    }

    public void Heal(int amount = 1)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>Restores full health (use after respawn / restart).</summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        lastHitTime = -999f;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}