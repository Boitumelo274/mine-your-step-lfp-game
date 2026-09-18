using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    [Header("Grid Position")]
    public int gridX;
    public int gridY;

    [Header("Stress Cue Sprites")]
    [SerializeField] private Sprite cleanCompressionSprite;  // 0 Hazards
    [SerializeField] private Sprite hairlineCrackSprite;     // 1 Hazard
    [SerializeField] private Sprite spiderCrackSprite;       // 2 Hazards
    [SerializeField] private Sprite lavaFissureSprite;       // 3+ Hazards

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem warningParticles;

    [Header("Deformation Settings")]
    [SerializeField] private float deformationOffset = -0.15f;

    private SpriteRenderer spriteRenderer;
    private bool isDeformed = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Auto-register this tile with the GridManager
        GridManager manager = FindObjectOfType<GridManager>();
        if (manager != null)
        {
            manager.RegisterTileVisualizer(gridX, gridY, this);
        }
    }

    public void DeformAndApplyStressCue(int hazardCount)
    {
        // 1. Apply soft-body displacement (deform tile downward slightly)
        if (!isDeformed)
        {
            transform.position += new Vector3(0, deformationOffset, 0);
            isDeformed = true;
        }

        // 2. Swap sprites and apply dynamic hazard color tinting
        switch (hazardCount)
        {
            case 0:
                if (cleanCompressionSprite) spriteRenderer.sprite = cleanCompressionSprite;
                spriteRenderer.color = Color.white; // Clean soil (no color tint)
                break;

            case 1:
                if (hairlineCrackSprite) spriteRenderer.sprite = hairlineCrackSprite;
                spriteRenderer.color = Color.yellow; // Yellow tint for 1 hazard
                break;

            case 2:
                if (spiderCrackSprite) spriteRenderer.sprite = spiderCrackSprite;
                spriteRenderer.color = new Color(1.0f, 0.5f, 0.0f); // Bright orange tint for 2 hazards
                break;

            default: // 3 or more hazards
                if (lavaFissureSprite) spriteRenderer.sprite = lavaFissureSprite;
                spriteRenderer.color = Color.red; // Red tint for 3+ hazards
                if (warningParticles != null) warningParticles.Play();
                break;
        }
    }

    public void TriggerLavaCollapse()
    {
        if (lavaFissureSprite) spriteRenderer.sprite = lavaFissureSprite;
        spriteRenderer.color = Color.red;
        if (warningParticles != null) warningParticles.Play();
    }
}