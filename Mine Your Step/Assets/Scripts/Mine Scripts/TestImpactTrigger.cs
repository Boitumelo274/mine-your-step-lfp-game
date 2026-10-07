using UnityEngine;

public class TestImpactTrigger : MonoBehaviour
{
    public GridManager gridManager;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    private void Update()
    {
        // Left-Click: Trigger Impact Scan
        if (Input.GetMouseButtonDown(0))
        {
            TileVisualizer tile = RaycastToTile();
            if (tile != null)
            {
                Debug.Log($"[Test Impact] Left-clicked tile '{tile.name}' at {tile.transform.position}. Triggering impact scan!");
                if (gridManager != null)
                {
                    gridManager.HandlePlatformImpact(tile.transform.position);
                }
            }
        }

        // Right-Click: Toggle Flag on Tile
        if (Input.GetMouseButtonDown(1))
        {
            TileVisualizer tile = RaycastToTile();
            if (tile != null)
            {
                Debug.Log($"[Test Impact] Right-clicked tile '{tile.name}'. Toggling Flag!");
                tile.ToggleFlag();
            }
        }
    }

    private TileVisualizer RaycastToTile()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            TileVisualizer tile = hit.collider.GetComponent<TileVisualizer>();
            if (tile != null)
            {
                return tile;
            }
        }
        return null;
    }
}