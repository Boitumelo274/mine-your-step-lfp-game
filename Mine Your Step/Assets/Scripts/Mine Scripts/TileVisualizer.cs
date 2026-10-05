using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileVisualizer : MonoBehaviour
{
    [Header("Tile State")]
    public bool isMine = false;
    public bool isFlagged = false;
    public bool isDisarmed = false;

    [Header("Visual Feedback References")]
    public SpriteRenderer spriteRenderer;
    public Sprite defaultSprite;
    public Sprite cleanCompressionSprite;
    public Sprite hairlineCrackSprite;
    public Sprite spiderCrackSprite;
    public Sprite lavaFissureSprite;
    public Sprite disarmedSprite; // Used for Flag / Disarmed sprite
    public ParticleSystem warningParticles;

    [Header("Deformation Animation")]
    public float dipDistance = 0.2f;
    public float animSpeed = 5.0f;

    private Vector3 originalPosition;
    private bool isDeforming = false;

    private void Awake()
    {
        originalPosition = transform.position;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null && defaultSprite == null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
    }

    /// <summary>
    /// Backward-compatibility method for Checkpoint.cs or existing trigger calls.
    /// </summary>
    public void ExposeVisualClues(int nearbyMineCount = 0)
    {
        ApplyAreaScanFeedback(nearbyMineCount, false);
    }

    /// <summary>
    /// Toggles flag state on this tile (shows disarmedSprite / flag icon).
    /// </summary>
    public void ToggleFlag()
    {
        isFlagged = !isFlagged;

        if (isFlagged)
        {
            if (disarmedSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = disarmedSprite;
            Debug.Log($"[TileVisualizer] '{gameObject.name}' is now FLAGGED.");
        }
        else
        {
            if (defaultSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = defaultSprite;
            Debug.Log($"[TileVisualizer] '{gameObject.name}' flag REMOVED.");
        }
    }

    /// <summary>
    /// Applies visual crack levels or particle warnings based on surrounding threat level.
    /// </summary>
    public void ApplyAreaScanFeedback(int nearbyMineCount, bool animateDip = false)
    {
        // Preserve flag state if tile is flagged or disarmed
        if (isFlagged || isDisarmed)
        {
            if (disarmedSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = disarmedSprite;
            return;
        }

        // Direct Mine hit/reveal
        if (isMine)
        {
            if (lavaFissureSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = lavaFissureSprite;

            if (warningParticles != null && !warningParticles.isPlaying)
                warningParticles.Play();

            if (animateDip && !isDeforming)
                StartCoroutine(AnimateDeformation());

            return;
        }

        // Apply crack intensity based on nearby mine density:
        // 0 mines nearby  -> clean tile
        // 1 mine nearby   -> hairline crack
        // 2+ mines nearby -> spider crack
        if (spriteRenderer != null)
        {
            if (nearbyMineCount >= 2 && spiderCrackSprite != null)
            {
                spriteRenderer.sprite = spiderCrackSprite;
            }
            else if (nearbyMineCount == 1 && hairlineCrackSprite != null)
            {
                spriteRenderer.sprite = hairlineCrackSprite;
            }
            else if (nearbyMineCount == 0 && cleanCompressionSprite != null)
            {
                spriteRenderer.sprite = cleanCompressionSprite;
            }
        }

        // Dip down physically only if directly landed on/clicked
        if (animateDip && !isDeforming)
        {
            StartCoroutine(AnimateDeformation());
        }
    }

    private IEnumerator AnimateDeformation()
    {
        isDeforming = true;

        Vector3 targetPos = originalPosition + Vector3.down * dipDistance;

        // Dip down
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, animSpeed * Time.deltaTime);
            yield return null;
        }

        // Return to original position
        while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, animSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPosition;
        isDeforming = false;
    }
}