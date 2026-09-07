using UnityEngine;

/// <summary>
/// Collision-only deformable ground. Keeps your SpriteRenderer completely
/// untouched - this script does NOT use MeshFilter/MeshRenderer at all, so
/// there is no conflict and nothing to set up beyond adding this script.
///
/// It holds a row of vertex heights along the ground and reshapes a
/// PolygonCollider2D from them:
/// - Ground impacts / slams push the surface DOWN into a concave dent.
/// - Headbutts from below push the surface UP into a rippled fold.
///
/// NOT spring-based - no bounce or oscillation. Each impact nudges the
/// surface toward a new target height once and it eases there smoothly
/// (via Mathf.MoveTowards, frame-rate independent) and stays. Dents only
/// ever get deeper; folds only ever get taller.
///
/// NOTE: Because there is no mesh here, the collision shape deforms but
/// your sprite will NOT visually dent or fold - it will keep its original
/// look while the invisible collider underneath changes shape. If you
/// later want the visuals to deform too, that needs a mesh (or a custom
/// deformation shader on the sprite) - a separate step from this script.
///
/// Setup:
/// - Put this on the SAME GameObject as your existing SpriteRenderer.
/// - Requires only PolygonCollider2D (auto-added) - no MeshFilter/
///   MeshRenderer, so it will NOT conflict with the SpriteRenderer.
/// - Set the GameObject's layer to whatever PlayerController's
///   `terrainLayer` LayerMask includes.
/// - Set `width` to match your sprite's actual world-space width, and
///   `baseHeight` to match its world-space height, so the collider lines
///   up with what's drawn. Leave the Transform scale at whatever you
///   already use for the sprite - width/baseHeight are independent of it,
///   just make sure they match the sprite's real on-screen size.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class DeformableTerrain : MonoBehaviour
{
    [Header("Shape")]
    [Tooltip("Total world-space width of the ground strip. Should match your sprite's actual displayed width.")]
    [SerializeField] private float width = 20f;

    [Tooltip("Starting/default surface height. Should match your sprite's actual displayed height.")]
    [SerializeField] private float baseHeight = 4f;

    [Tooltip("How many vertices make up the deformable surface. Higher = smoother, more detailed dents/folds, but more expensive.")]
    [SerializeField] private int resolution = 40;

    [Tooltip("The surface can never be dented below this height.")]
    [SerializeField] private float minSurfaceHeight = 0.5f;

    [Tooltip("The surface can never be folded/bumped above this height.")]
    [SerializeField] private float maxSurfaceHeight = 6.5f;

    [Header("Settling (eases to target once, no bounce/overshoot)")]
    [Tooltip("Units per second the collider surface moves toward its new target height. Higher = snappier response, lower = smoother/slower settle.")]
    [SerializeField] private float settleSpeed = 10f;

    [Header("Impact / Slam (concave dents)")]
    [SerializeField] private AnimationCurve impactFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Headbutt (folds)")]
    [Tooltip("How tightly packed the ripples in a fold are.")]
    [SerializeField] private float foldFrequency = 3f;

    [Tooltip("Strength of the ripple relative to the central push. 0 = smooth bump, higher = more crinkled fold.")]
    [SerializeField] private float foldRippleAmount = 0.4f;

    // The actual deformable data - one height value per vertex along the strip.
    private float[] currentHeights;
    private float[] targetHeights;

    private PolygonCollider2D polyCollider;

    private float Spacing => width / resolution;
    private int VertexCount => resolution + 1;

    private void Awake()
    {
        polyCollider = GetComponent<PolygonCollider2D>();

        currentHeights = new float[VertexCount];
        targetHeights = new float[VertexCount];
        for (int i = 0; i < VertexCount; i++)
        {
            currentHeights[i] = baseHeight;
            targetHeights[i] = baseHeight;
        }

        RebuildCollider();
    }

    private void Update()
    {
        bool anyChanged = false;

        for (int i = 0; i < VertexCount; i++)
        {
            if (Mathf.Approximately(currentHeights[i], targetHeights[i]))
            {
                continue;
            }

            currentHeights[i] = Mathf.MoveTowards(
                currentHeights[i],
                targetHeights[i],
                settleSpeed * Time.deltaTime
            );

            anyChanged = true;
        }

        if (anyChanged)
        {
            RebuildCollider();
        }
    }

    // =========================
    // PUBLIC API - call these from PlayerController on collision
    // =========================

    /// <summary>Normal landing impact - a shallow concave dent.</summary>
    public void Impact(float worldX, float strength, float radius)
    {
        ApplyDent(worldX, strength, radius);
    }

    /// <summary>Ground slam - a deeper, wider concave crater than a normal impact.</summary>
    public void RegisterSlam(float worldX, float strength, float radius)
    {
        ApplyDent(worldX, strength * 1.5f, radius * 1.3f);
    }

    /// <summary>Headbutt from below - pushes the surface up into a rippled fold.</summary>
    public void Fold(float worldX, float strength, float radius)
    {
        ApplyFold(worldX, strength, radius);
    }

    // =========================
    // DEFORMATION LOGIC
    // =========================

    private void ApplyDent(float worldX, float strength, float radius)
    {
        float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
        int centerIndex = IndexFromLocalX(localX);
        int spread = Mathf.CeilToInt(radius / Spacing);

        for (int i = centerIndex - spread; i <= centerIndex + spread; i++)
        {
            if (i < 0 || i >= VertexCount)
            {
                continue;
            }

            float vertLocalX = -width / 2f + i * Spacing;
            float dist = Mathf.Abs(vertLocalX - localX);

            if (dist > radius)
            {
                continue;
            }

            float falloff = impactFalloff.Evaluate(1f - dist / radius);
            float candidate = targetHeights[i] - strength * falloff;
            candidate = Mathf.Clamp(candidate, minSurfaceHeight, maxSurfaceHeight);

            // Dents only ever get deeper - never raise the surface back up.
            targetHeights[i] = Mathf.Min(targetHeights[i], candidate);
        }
    }

    private void ApplyFold(float worldX, float strength, float radius)
    {
        float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
        int centerIndex = IndexFromLocalX(localX);
        int spread = Mathf.CeilToInt(radius / Spacing);

        for (int i = centerIndex - spread; i <= centerIndex + spread; i++)
        {
            if (i < 0 || i >= VertexCount)
            {
                continue;
            }

            float vertLocalX = -width / 2f + i * Spacing;
            float dist = Mathf.Abs(vertLocalX - localX);

            if (dist > radius)
            {
                continue;
            }

            float falloff = impactFalloff.Evaluate(1f - dist / radius);

            // A ripple layered on top of the central push gives a crinkled
            // "fold" silhouette instead of one smooth dome.
            float ripple = 1f + Mathf.Cos(dist * foldFrequency) * foldRippleAmount;
            float bump = strength * falloff * ripple;

            float candidate = targetHeights[i] + bump;
            candidate = Mathf.Clamp(candidate, minSurfaceHeight, maxSurfaceHeight);

            // Folds only ever push the surface up further - never sink back down.
            targetHeights[i] = Mathf.Max(targetHeights[i], candidate);
        }
    }

    private int IndexFromLocalX(float localX)
    {
        float t = Mathf.InverseLerp(-width / 2f, width / 2f, localX);
        return Mathf.RoundToInt(t * resolution);
    }

    // =========================
    // COLLIDER ONLY - no mesh, no renderer
    // =========================

    private void RebuildCollider()
    {
        int vertCount = VertexCount;
        Vector2[] points = new Vector2[vertCount + 2];

        points[0] = new Vector2(-width / 2f, 0f);
        points[1] = new Vector2(width / 2f, 0f);

        // Walk back across the top, right to left, to close the polygon.
        for (int i = 0; i < vertCount; i++)
        {
            int sourceIndex = vertCount - 1 - i;
            float x = -width / 2f + sourceIndex * Spacing;
            points[2 + i] = new Vector2(x, currentHeights[sourceIndex]);
        }

        polyCollider.pathCount = 1;
        polyCollider.SetPath(0, points);
    }
}