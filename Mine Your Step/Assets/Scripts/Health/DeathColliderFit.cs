using UnityEngine;

/// <summary>
/// Add to the miner (next to MinerController). It stays idle until MinerController.TriggerDeath()
/// calls Begin(). After that, every frame it resizes the capsule collider to match whatever the
/// death animation currently looks like, so the body that is drawn is the body that touches the floor.
/// </summary>
[RequireComponent(typeof(CapsuleCollider2D))]
public class DeathColliderFit : MonoBehaviour
{
    [Tooltip("The SpriteRenderer that draws the miner. Found automatically if left empty.")]
    [SerializeField] private SpriteRenderer sprite;

    private CapsuleCollider2D capsule;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        enabled = false; // does nothing until the miner dies
    }

    public void Begin()
    {
        enabled = true;
    }

    // LateUpdate runs after the Animator has posed the sprite this frame.
    private void LateUpdate()
    {
        if (sprite == null || capsule == null) return;

        Bounds b = sprite.bounds; // world-space box around the current sprite frame
        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));

        capsule.direction = b.size.x >= b.size.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(b.size.x / sx, b.size.y / sy);
        capsule.offset = transform.InverseTransformPoint(b.center);
    }
}