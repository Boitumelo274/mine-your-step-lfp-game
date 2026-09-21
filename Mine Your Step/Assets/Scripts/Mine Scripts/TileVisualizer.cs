using System.Collections;
using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    [Header("Grid Position")]
    public int gridX;
    public int gridY;

    [Header("Visual Feedback References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D tileCollider;

    [Header("Stress Cue Sprites")]
    [SerializeField] private Sprite cleanCompressionSprite;  // 0 Hazards
    [SerializeField] private Sprite hairlineCrackSprite;      // 1 Hazard
    [SerializeField] private Sprite spiderCrackSprite;        // 2 Hazards
    [SerializeField] private Sprite lavaFissureSprite;        // 3+ Hazards

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem warningParticles;

    [Header("Deformation Settings")]
    [SerializeField] private float deformationOffset = -0.15f;

    private Vector3 originalPosition;
    private bool isCollapsed = false;

    private void Awake()
    {
        originalPosition = transform.position;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (tileCollider == null) tileCollider = GetComponent<BoxCollider2D>();

        GridManager gridMgr = FindFirstObjectByType<GridManager>();
        if (gridMgr != null)
        {
            // Standardize raw world positions relative to the GridManager's transform position
            Vector3 relativePos = transform.position - gridMgr.transform.position;

            gridX = Mathf.RoundToInt(relativePos.x);
            gridY = Mathf.RoundToInt(relativePos.y);

            gridMgr.RegisterTileVisualizer(gridX, gridY, this);
        }
    }

    public void DeformAndApplyStressCue(int hazardCount)
    {
        if (isCollapsed) return;

        transform.position = originalPosition + new Vector3(0f, deformationOffset, 0f);

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
                if (spiderCrackSprite) spriteRenderer.sprite = spiderCrackSprite;
                spriteRenderer.color = new Color(1.0f, 0.5f, 0.0f);
                break;

            default:
                if (lavaFissureSprite) spriteRenderer.sprite = lavaFissureSprite;
                spriteRenderer.color = Color.red;

                if (warningParticles != null) warningParticles.Play();

                StartCoroutine(CollapseSequence());
                break;
        }
    }

    private IEnumerator CollapseSequence()
    {
        isCollapsed = true;

        yield return new WaitForSeconds(0.25f);

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 dropPos = startPos + new Vector3(0f, -2.0f, 0f);

        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(startPos, dropPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (tileCollider != null)
        {
            tileCollider.enabled = false;
        }

        spriteRenderer.color = new Color(1f, 0f, 0f, 0.2f);
    }
}