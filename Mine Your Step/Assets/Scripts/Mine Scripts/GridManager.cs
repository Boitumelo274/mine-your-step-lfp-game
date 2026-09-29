using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Minesweeper Configuration")]
    [Tooltip("Target number of hidden mines to spawn inside the red hazard zones.")]
    [SerializeField] private int targetMines = 2;

    [Tooltip("Distance radius to detect adjacent hidden mines (Set to ~12 to cover 1-2 platform steps).")]
    [SerializeField] private float detectionRadius = 12.0f;

    [Header("Hazard Zone Filtering")]
    [Tooltip("Drag Worldbuilding here so it loops through all Indicator blocks.")]
    [SerializeField] private Transform hazardIndicatorsParent;

    // Static event triggered by platform impacts
    public static Action<Vector2> OnPlatformStruck;

    private Dictionary<Vector2Int, TileVisualizer> grid = new Dictionary<Vector2Int, TileVisualizer>();
    private HashSet<Vector2Int> hazardGrid = new HashSet<Vector2Int>();
    private List<Vector2> indicatorWorldPositions = new List<Vector2>();

    private void Awake()
    {
        CacheHazardZones();
        GenerateMines();
    }

    private void OnEnable()
    {
        OnPlatformStruck += HandlePlatformImpact;
    }

    private void OnDisable()
    {
        OnPlatformStruck -= HandlePlatformImpact;
    }

    private void CacheHazardZones()
    {
        indicatorWorldPositions.Clear();

        if (hazardIndicatorsParent != null)
        {
            foreach (Transform child in hazardIndicatorsParent)
            {
                if (child.name.StartsWith("Indicator"))
                {
                    indicatorWorldPositions.Add(child.position);
                }
            }
        }
        else
        {
#pragma warning disable 0618
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
#pragma warning restore 0618

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("Indicator"))
                {
                    indicatorWorldPositions.Add(obj.transform.position);
                }
            }
        }

        Debug.Log($"GridManager: Cached {indicatorWorldPositions.Count} red hazard indicator zones.");
    }

    public void GenerateMines()
    {
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        hazardGrid.Clear();

        if (indicatorWorldPositions.Count == 0)
        {
            Debug.LogWarning("GridManager: No red indicator zones found to place mines!");
            return;
        }

        // Shuffle indicator positions and pick exact target number of hidden mines
        List<Vector2> availablePositions = new List<Vector2>(indicatorWorldPositions);
        int minesToSpawn = Mathf.Min(targetMines, availablePositions.Count);

        for (int i = 0; i < minesToSpawn; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
            Vector2 chosenPos = availablePositions[randomIndex];
            Vector2Int gridKey = new Vector2Int(Mathf.RoundToInt(chosenPos.x), Mathf.RoundToInt(chosenPos.y));

            hazardGrid.Add(gridKey);
            availablePositions.RemoveAt(randomIndex);
        }

        Debug.Log($"GridManager: Active hidden mines spawned: {hazardGrid.Count} (Inside {indicatorWorldPositions.Count} red hazard zones).");
    }

    public void RegisterTileVisualizer(int x, int y, TileVisualizer tile)
    {
        Vector2Int key = new Vector2Int(x, y);
        grid[key] = tile;
    }

    private void HandlePlatformImpact(Vector2 worldPosition)
    {
        TileVisualizer struckTile = null;

        float closestDistance = float.MaxValue;
        foreach (var entry in grid.Values)
        {
            if (entry != null)
            {
                float dist = Vector2.Distance(entry.transform.position, worldPosition);
                if (dist < 4.0f && dist < closestDistance)
                {
                    closestDistance = dist;
                    struckTile = entry;
                }
            }
        }

        if (struckTile != null)
        {
            bool isDirectMineHit = IsTileAMine(struckTile.transform.position);
            int adjacentMinesCount = ScanAdjacentHazards(struckTile.transform.position);

            struckTile.DeformAndApplyStressCue(adjacentMinesCount, isDirectMineHit);

            Debug.Log($"GridManager: Rock '{struckTile.name}' landed on. Direct Mine: {isDirectMineHit}, Nearby Mines: {adjacentMinesCount}");
        }
    }

    private bool IsTileAMine(Vector3 tileWorldPos)
    {
        Vector2 tilePos2D = new Vector2(tileWorldPos.x, tileWorldPos.y);
        foreach (Vector2Int hazardKey in hazardGrid)
        {
            Vector2 hazardPos = new Vector2(hazardKey.x, hazardKey.y);
            if (Vector2.Distance(tilePos2D, hazardPos) <= 3.5f)
            {
                return true;
            }
        }
        return false;
    }

    private int ScanAdjacentHazards(Vector3 tileWorldPos)
    {
        int count = 0;
        Vector2 tilePos2D = new Vector2(tileWorldPos.x, tileWorldPos.y);

        foreach (Vector2Int hazardKey in hazardGrid)
        {
            Vector2 hazardPos = new Vector2(hazardKey.x, hazardKey.y);
            float distance = Vector2.Distance(tilePos2D, hazardPos);

            if (distance > 3.5f && distance <= detectionRadius)
            {
                count++;
            }
        }

        return count;
    }
}