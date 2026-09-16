using System.Collections;
using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    public int gridX;
    public int gridY;

    [Header("Visual Feedback References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D tileCollider;

    [Header("Stress Cue Sprites")]
    [SerializeField] private Sprite cleanCompressionSprite;
    [SerializeField] private Sprite hairlineCrackSprite;
    [SerializeField] private Sprite fractureSprite;

    private Vector3 originalPosition;
    private bool isCollapsed = false;

    private void Awake()
    {
        originalPosition = transform.position;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (tileCollider == null) tileCollider = GetComponent<BoxCollider2D>();

        // Register self with GridManager on startup
        GridManager gridMgr = FindFirstObjectByType<GridManager>();
        if (gridMgr != null)
        {
            gridMgr.RegisterTileVisualizer(gridX, gridY, this);
        }
    }

    public void DeformAndApplyStressCue(int hazardCount)
    {
        if (isCollapsed) return;

        // Apply downward deformation displacement
        transform.position = originalPosition + new Vector3(0f, -0.15f, 0f);

        switch (hazardCount)
        {
            case 0:
                if (cleanCompressionSprite) spriteRenderer.sprite = cleanCompressionSprite;
                spriteRenderer.color = Color.white;
                break;

            case 1:
                if (hairlineCrackSprite) spriteRenderer.sprite = hairlineCrackSprite;
                spriteRenderer.color = Color.yellow;
                break;

            case 2:
                if (fractureSprite) spriteRenderer.sprite = fractureSprite;
                spriteRenderer.color = new Color(1f, 0.5f, 0f); // Orange
                break;

            default: // 3+ Hazards (Danger / Collapse Trigger)
                spriteRenderer.color = Color.red;
                StartCoroutine(CollapseSequence());
                break;
        }
    }

    private IEnumerator CollapseSequence()
    {
        isCollapsed = true;

        // Brief delay before collapse so player sees the red warning
        yield return new WaitForSeconds(0.25f);

        // Drop platform visually
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 dropPos = startPos + new Vector3(0f, -2.0f, 0f);

        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(startPos, dropPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Disable collider so player falls through
        if (tileCollider != null)
        {
            tileCollider.enabled = false;
        }

        // Fade out sprite
        spriteRenderer.color = new Color(1f, 0f, 0f, 0.2f);
    }
}