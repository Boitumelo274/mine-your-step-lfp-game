using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 20;

    // Static event triggered by platform impacts
    public static Action<Vector2> OnPlatformStruck;

    // Internal lookup dictionary storing registered tile visualizers
    private Dictionary<Vector2Int, TileVisualizer> grid = new Dictionary<Vector2Int, TileVisualizer>();

    // Internal mine matrix (True = Mine/Hazard present)
    private bool[,] mineMatrix;

    private void Awake()
    {
        mineMatrix = new bool[width, height];

        // --- TEST HAZARDS ---
        // Populating adjacent coordinates around (0,0) to trigger color feedback
        mineMatrix[1, 0] = true; // Hazard 1 -> Triggers Yellow tint
        mineMatrix[0, 1] = true; // Hazard 2 -> Triggers Orange tint
        mineMatrix[1, 1] = true; // Hazard 3 -> Triggers Red tint + particles
    }

    private void OnEnable()
    {
        OnPlatformStruck += HandlePlatformImpact;
    }

    private void OnDisable()
    {
        OnPlatformStruck -= HandlePlatformImpact;
    }

    /// <summary>
    /// Registers a TileVisualizer component into the grid lookup dictionary.
    /// </summary>
    public void RegisterTileVisualizer(int x, int y, TileVisualizer tile)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!grid.ContainsKey(key))
        {
            grid.Add(key, tile);
        }
        else
        {
            grid[key] = tile;
        }
    }

    /// <summary>
    /// Handles landing impact broadcasts by finding the matching platform tile and scanning hazards.
    /// </summary>
    private void HandlePlatformImpact(Vector2 worldPosition)
    {
        TileVisualizer struckTile = null;

        // 1. Direct match: Check if a registered tile exists close to the struck world position
        foreach (var entry in grid.Values)
        {
            if (entry != null && Vector2.Distance(entry.transform.position, worldPosition) < 1.0f)
            {
                struckTile = entry;
                break;
            }
        }

        // 2. Fallback: Default to the first registered tile for testing if distance check misses
        if (struckTile == null && grid.Count > 0)
        {
            foreach (var entry in grid.Values)
            {
                if (entry != null)
                {
                    struckTile = entry;
                    break;
                }
            }
        }

        // 3. Process the hazard scan and trigger visual color cues
        if (struckTile != null)
        {
            int hazardCount = ScanAdjacentHazards(struckTile.gridX, struckTile.gridY);
            struckTile.DeformAndApplyStressCue(hazardCount);
            Debug.Log($"GridManager: Tile at Grid ({struckTile.gridX}, {struckTile.gridY}) updated with {hazardCount} adjacent hazards.");
        }
        else
        {
            Debug.LogWarning("GridManager received impact signal, but no matching registered TileVisualizer was found in scene!");
        }
    }

    /// <summary>
    /// Performs an 8-neighbor matrix scan around the target coordinate to count adjacent hazards.
    /// </summary>
    private int ScanAdjacentHazards(int gridX, int gridY)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the tile itself

                int checkX = gridX + dx;
                int checkY = gridY + dy;

                if (IsValidIndex(checkX, checkY))
                {
                    if (mineMatrix[checkX, checkY])
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private bool IsValidIndex(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}