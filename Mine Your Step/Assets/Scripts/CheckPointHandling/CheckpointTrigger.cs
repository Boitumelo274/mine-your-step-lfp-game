using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        MinerController miner = other.GetComponentInParent<MinerController>();
        if (miner == null) return;

        activated = true;

        // 1. Capture world state snapshot (Jewels, Deformations, etc.)
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.CaptureAll();
        }

        // 2. Notify PlayerRespawn script if this object has a Checkpoint component
        Checkpoint checkpoint = GetComponent<Checkpoint>();
        PlayerRespawn respawnScript = miner.GetComponent<PlayerRespawn>();

        if (respawnScript != null && checkpoint != null)
        {
            respawnScript.SetCheckpoint(checkpoint);
        }
    }
}