using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    [Header("Mine State")]
    public bool isMine = false;
    public bool isDetonated = false;
    public bool isFlagged = false;
    public bool isDisarmed = false;
    public bool isJewel = false;

    [Header("Sprites & Visuals")]
    public Color disarmedColor = Color.green;
    public Sprite defaultRockSprite;
    public Sprite hairlineCrackSprite; // 1 adjacent mine
    public Sprite spiderCrackSprite;   // 2+ adjacent mines
    public Sprite lavaFissureSprite;   // Detonated actual mine
    public Sprite flagSprite;          // Flagged tile sprite
    public Sprite jewelSprite;         // Sprite shown when converted to a jewel

    [Header("Components")]
    public SpriteRenderer spriteRenderer;
    [SerializeField] private MineableJewel mineableJewelComponent;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (mineableJewelComponent == null)
            mineableJewelComponent = GetComponent<MineableJewel>();

        // Ensure mining component starts disabled on hidden mines
        if (mineableJewelComponent != null)
            mineableJewelComponent.enabled = false;
    }

    // Disarms the mine and converts it into a collectible Mineable Jewel node
    public void DisarmTile()
    {
        if (!isMine || isDetonated || isDisarmed) return;

        isDisarmed = true;
        isJewel = true; // Transforms into a jewel block!

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // Reset tint
            if (jewelSprite != null)
            {
                spriteRenderer.sprite = jewelSprite; // Switch to jewel sprite
            }
        }

        // ENABLE the MineableJewel component so MinerMining can detect and mine it!
        if (mineableJewelComponent != null)
        {
            mineableJewelComponent.enabled = true;
        }

        Debug.Log($"[Disarmer] Mine at {transform.position} disarmed and converted into a Mineable Jewel!");
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
        if (isDetonated || isDisarmed || isJewel) return false;

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
        isJewel = false;

        // Disable MineableJewel component back when resetting/respawning
        if (mineableJewelComponent != null)
        {
            mineableJewelComponent.enabled = false;
        }

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
            Gizmos.color = isJewel ? Color.cyan : (isDisarmed ? Color.green : Color.red);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
        }
    }
}