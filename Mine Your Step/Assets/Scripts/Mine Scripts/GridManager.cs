using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Setup")]
    public Transform hazardIndicatorsParent;
    public int targetMines = 10;

    [Header("Scan & Threat Radii")]
    [Tooltip("How far from the impact point the shockwave scan spreads.")]
    public float impactScanRadius = 40.0f;

    [Tooltip("How close a mine must be to a tile to register as a threat (Tiles are ~7.5u apart; 16u covers ~2 tiles away).")]
    public float mineProximityRadius = 16.0f;

    private List<TileVisualizer> cachedTiles = new List<TileVisualizer>();
    private List<Vector2> activeMinePositions = new List<Vector2>();

    private void Start()
    {
        InitializeGridSystem();
    }

    public void InitializeGridSystem()
    {
        CacheHazardZones();
        GenerateMines();
        SyncMinesWithTiles();
    }

    private void CacheHazardZones()
    {
        cachedTiles.Clear();

        if (hazardIndicatorsParent != null)
        {
            TileVisualizer[] tiles = hazardIndicatorsParent.GetComponentsInChildren<TileVisualizer>();
            cachedTiles.AddRange(tiles);
        }
        else
        {
            cachedTiles.AddRange(FindObjectsByType<TileVisualizer>(FindObjectsSortMode.None));
        }

        Debug.Log($"[GridManager] Cached {cachedTiles.Count} candidate platform tiles.");
    }

    private void GenerateMines()
    {
        activeMinePositions.Clear();
        if (cachedTiles.Count == 0) return;

        List<TileVisualizer> availablePool = new List<TileVisualizer>(cachedTiles);
        int minesToSpawn = Mathf.Min(targetMines, availablePool.Count);

        for (int i = 0; i < minesToSpawn; i++)
        {
            int randomIndex = Random.Range(0, availablePool.Count);
            TileVisualizer selectedTile = availablePool[randomIndex];

            activeMinePositions.Add(selectedTile.transform.position);
            availablePool.RemoveAt(randomIndex);
        }

        Debug.Log($"[GridManager] Active hidden mines generated: {activeMinePositions.Count} out of {cachedTiles.Count} candidate tiles.");
    }

    private void SyncMinesWithTiles()
    {
        int mineCount = 0;
        foreach (TileVisualizer tile in cachedTiles)
        {
            tile.isMine = activeMinePositions.Contains(tile.transform.position);
            if (tile.isMine) mineCount++;
        }

        Debug.Log($"[GridManager] Synced mine statuses across scene tiles ({mineCount} set to active mines).");
    }

    /// <summary>
    /// Scans surrounding tiles and updates visual hazard cues.
    /// </summary>
    public void HandlePlatformImpact(Vector2 impactPosition)
    {
        int scannedTilesCount = 0;

        foreach (TileVisualizer tile in cachedTiles)
        {
            float distToImpact = Vector2.Distance(impactPosition, tile.transform.position);

            if (distToImpact <= impactScanRadius)
            {
                scannedTilesCount++;

                // Count active mines within mineProximityRadius of THIS specific tile
                int minesNearThisTile = 0;
                foreach (Vector2 minePos in activeMinePositions)
                {
                    if (Vector2.Distance(tile.transform.position, minePos) <= mineProximityRadius)
                    {
                        minesNearThisTile++;
                    }
                }

                bool isDirectStruckTile = distToImpact < 1.0f;
                tile.ApplyAreaScanFeedback(minesNearThisTile, isDirectStruckTile);
            }
        }

        Debug.Log($"[GridManager] Impact at {impactPosition}. Scanned {scannedTilesCount} tiles within {impactScanRadius}u radius.");
    }
}