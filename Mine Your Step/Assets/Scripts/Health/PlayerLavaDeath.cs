using UnityEngine;

public class PlayerLavaDeath : MonoBehaviour
{
    private Vector3 lastCheckpointPosition;

    private void Start()
    {
        // Set the initial spawn position as the starting checkpoint
        lastCheckpointPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the player touches the lava directly (the ball can pass, but player dies)
        if (collision.gameObject.CompareTag("Lava"))
        {
            DieAndRespawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Optional: If you use trigger zones for checkpoints, update position here
        if (other.CompareTag("Checkpoint"))
        {
            lastCheckpointPosition = other.transform.position;
            Debug.Log("[Checkpoint] Checkpoint updated!");
        }
    }

    public void DieAndRespawn()
    {
        Debug.Log("[Player] Fell into the lava! Losing a life and respawning at checkpoint.");

        // TODO: Call your Game Manager here to subtract a life, e.g.:
        // GameManager.Instance.LoseLife();

        // 1. Teleport player back to the last checkpoint
        transform.position = lastCheckpointPosition;

        // 2. Kill any leftover momentum/velocity so they don't fly off on respawn
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // Public method you can call if they pass a checkpoint manually via script
    public void SetCheckpoint(Vector3 newCheckpoint)
    {
        lastCheckpointPosition = newCheckpoint;
    }
}