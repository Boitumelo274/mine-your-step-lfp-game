using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))]
public class DestructibleTerrain : MonoBehaviour
{
    [Header("Destruction")]
    [SerializeField] private float destructionRadius = 1f;

    [Header("Testing")]
    [SerializeField] private KeyCode destroyKey = KeyCode.F;

    private Tilemap tilemap;
    private CompositeCollider2D compositeCollider; // may be null if not used

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        compositeCollider = GetComponent<CompositeCollider2D>();
    }

    private void Update()
    {
        // Keep for manual testing while you're iterating
        if (Input.GetKeyDown(destroyKey))
        {
            DestroyAtMouse();
        }
    }

    private void DestroyAtMouse()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        DestroyTerrain(mouseWorldPosition);
    }

    /// <summary>
    /// Call this from the Player's landing/collision logic,
    /// passing the contact point where it hit the ground.
    /// </summary>
    public void DestroyTerrain(Vector2 worldPosition)
    {
        Vector3Int centerCell = tilemap.WorldToCell(worldPosition);
        int radius = Mathf.CeilToInt(destructionRadius);
        bool anyTileRemoved = false;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cellPosition = centerCell + new Vector3Int(x, y, 0);
                Vector3 cellWorldPosition = tilemap.GetCellCenterWorld(cellPosition);

                float distance = Vector2.Distance(worldPosition, cellWorldPosition);

                if (distance <= destructionRadius && tilemap.HasTile(cellPosition))
                {
                    tilemap.SetTile(cellPosition, null);
                    anyTileRemoved = true;
                }
            }
        }

        if (anyTileRemoved)
        {
            // Composite Collider2D needs an explicit refresh after tiles change,
            // otherwise the old collision shape lingers.
            if (compositeCollider != null)
            {
                compositeCollider.GenerateGeometry();
            }
        }
    }

    public bool IsSolid(Vector2 worldPosition)
    {
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        return tilemap.HasTile(cellPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, destructionRadius);
    }
}