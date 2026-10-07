using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Setup")]
    public Transform hazardIndicatorsParent;
    public int targetMines = 15;

    [Header("Scanner Area Settings")]
    public float scanRangeX = 600f;

    [Header("Neighbor Step Spacing")]
    public float maxStepDistanceX = 16.0f;
    public float maxRowHeightGapY = 1.5f;

    private List<TileVisualizer> allTiles = new List<TileVisualizer>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGridSystem();
    }

    public void InitializeGridSystem()
    {
        CacheHazardZones();
        GenerateMines();
    }

    private void CacheHazardZones()
    {
        allTiles.Clear();
        if (hazardIndicatorsParent == null) return;

        TileVisualizer[] tiles = hazardIndicatorsParent.GetComponentsInChildren<TileVisualizer>();
        allTiles.AddRange(tiles);

        Debug.Log($"[GridManager] Cached {allTiles.Count} total platform tiles across scene.");
    }

    public void GenerateMines()
    {
        if (allTiles.Count == 0) return;

        // Reset existing tiles completely
        foreach (var tile in allTiles)
        {
            tile.isMine = false;
            tile.isDetonated = false;
            tile.isFlagged = false;
            tile.ResetVisual();
        }

        int minesToPlace = Mathf.Min(targetMines, allTiles.Count);
        List<TileVisualizer> availableTiles = new List<TileVisualizer>(allTiles);

        // Exclude the first 3 starting tiles from spawning mines
        if (availableTiles.Count > 3)
        {
            availableTiles.RemoveRange(0, 3);
        }

        int placed = 0;
        while (placed < minesToPlace && availableTiles.Count > 0)
        {
            int index = Random.Range(0, availableTiles.Count);
            availableTiles[index].isMine = true;
            availableTiles.RemoveAt(index);
            placed++;
        }

        Debug.Log($"[GridManager] Generated {placed} hidden mines.");
    }

    public void HandlePlatformImpact(Vector2 impactPosition)
    {
        int scannedCount = 0;

        foreach (var tile in allTiles)
        {
            if (Mathf.Abs(tile.transform.position.x - impactPosition.x) > scanRangeX)
                continue;

            scannedCount++;

            // Real mines stay hidden as standard rocks during scans
            if (tile.isMine)
            {
                continue;
            }

            int count = CountAdjacentMines(tile);

            if (tile.spriteRenderer != null)
            {
                if (count == 1 && tile.hairlineCrackSprite != null)
                {
                    tile.spriteRenderer.sprite = tile.hairlineCrackSprite;
                }
                else if (count >= 2 && tile.spiderCrackSprite != null)
                {
                    tile.spriteRenderer.sprite = tile.spiderCrackSprite;
                }
                else if (count == 0 && tile.defaultRockSprite != null)
                {
                    tile.spriteRenderer.sprite = tile.defaultRockSprite;
                }
            }
        }

        Debug.Log($"[GridManager] Scanned {scannedCount} tiles from position {impactPosition}.");
    }

    private int CountAdjacentMines(TileVisualizer currentTile)
    {
        int count = 0;
        Vector2 currentPos = currentTile.transform.position;

        foreach (var otherTile in allTiles)
        {
            if (otherTile == currentTile) continue;

            Vector2 otherPos = otherTile.transform.position;

            float distX = Mathf.Abs(otherPos.x - currentPos.x);
            float distY = Mathf.Abs(otherPos.y - currentPos.y);

            if (distX > 0.1f && distX <= maxStepDistanceX && distY <= maxRowHeightGapY)
            {
                if (otherTile.isMine)
                {
                    count++;
                }
            }
        }

        return count;
    }
}