using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MineDisarmer : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleDisarm(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleDisarm(collision.gameObject);
    }

    private void HandleDisarm(GameObject targetObj)
    {
        TileVisualizer tile = targetObj.GetComponent<TileVisualizer>();
        if (tile == null)
        {
            tile = targetObj.GetComponentInParent<TileVisualizer>();
        }

        if (tile != null)
        {
            if (tile.isMine)
            {
                tile.DisarmTile();
            }

            // Destroy the circle disarmer projectile on tile impact
            Destroy(gameObject);
        }
    }
}