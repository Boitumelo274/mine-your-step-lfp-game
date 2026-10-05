using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    [Header("Mine State")]
    public bool isMine = false;
    public bool isDetonated = false;
    public bool isFlagged = false;
    public bool isDisarmed = false; // NEW

    [Header("Disarm Colors")]
    public Color disarmedColor = Color.green; // Color when disarmed

    [Header("Warning Sprites")]
    public Sprite defaultRockSprite;
    public Sprite hairlineCrackSprite; // 1 adjacent mine
    public Sprite spiderCrackSprite;   // 2+ adjacent mines
    public Sprite lavaFissureSprite;   // Detonated actual mine
    public Sprite flagSprite;          // Flagged tile sprite

    [Header("Components")]
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // Disarms the mine and tints it green
    public void DisarmTile()
    {
        if (!isMine || isDetonated || isDisarmed) return;

        isDisarmed = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = disarmedColor;
        }

        Debug.Log($"[Disarmer] Mine at {transform.position} disarmed successfully!");
    }

    public bool Detonate()
    {
        if (isDetonated || isDisarmed) return false;

        isDetonated = true;
        if (spriteRenderer != null && lavaFissureSprite != null)
        {
            spriteRenderer.sprite = lavaFissureSprite;
        }
        return true;
    }

    public bool ToggleFlag()
    {
        if (isDetonated || isDisarmed) return false;

        isFlagged = !isFlagged;

        if (spriteRenderer != null)
        {
            if (isFlagged && flagSprite != null)
            {
                spriteRenderer.sprite = flagSprite;
            }
            else if (!isFlagged && defaultRockSprite != null)
            {
                spriteRenderer.sprite = defaultRockSprite;
            }
        }

        return isFlagged;
    }

    public void ResetVisual()
    {
        isDisarmed = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // Reset tint color back to normal

            if (!isDetonated && defaultRockSprite != null)
            {
                spriteRenderer.sprite = defaultRockSprite;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (isMine)
        {
            Gizmos.color = isDisarmed ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
        }
    }
}