using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get the TileVisualizer component from the object the player stepped on
        TileVisualizer tile = other.GetComponent<TileVisualizer>();

        if (tile != null && tile.isMine && !tile.isFlagged)
        {
            // 1. Reveal the lava fissure on the stepped tile
            tile.Detonate();

            // 2. Trigger player death / damage logic here
            Debug.Log("Player stepped on a mine!");
        }
    }
}