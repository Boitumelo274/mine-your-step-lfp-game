using UnityEngine;

public class HealthIncrease : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = collision.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return; // not the player

        if (playerHealth.AddHealth(healAmount))
        {
            Destroy(gameObject); // consume the pickup
        }
    }
}