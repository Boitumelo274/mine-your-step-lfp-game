using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class BendablePlatform : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private int pointCount = 16;
    [SerializeField] private float platformLength = 8f;

    [Header("Bend Behavior")]
    [SerializeField] private float forceToDepthScale = 0.02f;   // how much impact force converts to bend depth
    [SerializeField] private float maxBendDepth = 2.5f;          // clamp so it can't fold through the floor
    [SerializeField] private float bendSpread = 1.5f;            // how far the dent spreads sideways (world units)
    [SerializeField] private float springStiffness = 40f;
    [SerializeField] private float damping = 6f;

    private Vector2[] localPositions;   // rest X positions along the beam
    private float[] permanentOffset;    // plastic (stays bent) — what makes the "fold"
    private float[] elasticOffset;      // temporary wobble on top of the permanent bend
    private float[] velocity;

    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        localPositions = new Vector2[pointCount];
        permanentOffset = new float[pointCount];
        elasticOffset = new float[pointCount];
        velocity = new float[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            float x = Mathf.Lerp(-platformLength / 2f, platformLength / 2f, t);
            localPositions[i] = new Vector2(x, 0f);
        }

        lineRenderer.positionCount = pointCount;
        UpdateColliderAndVisual();
    }

    private void FixedUpdate()
    {
        bool anyMovement = false;

        // Endpoints (0 and last) stay anchored — everything else can flex.
        for (int i = 1; i < pointCount - 1; i++)
        {
            float displacement = elasticOffset[i];
            float force = -springStiffness * displacement - damping * velocity[i];

            velocity[i] += force * Time.fixedDeltaTime;
            elasticOffset[i] += velocity[i] * Time.fixedDeltaTime;

            if (Mathf.Abs(velocity[i]) > 0.001f || Mathf.Abs(elasticOffset[i]) > 0.001f)
                anyMovement = true;
        }

        if (anyMovement)
        {
            UpdateColliderAndVisual();
        }
    }

    /// <summary>
    /// Call this when the player ground-slams onto the platform.
    /// worldContactPoint = where they hit; impactForce = how hard (e.g. mass * fall speed).
    /// </summary>
    public void ApplyImpact(Vector2 worldContactPoint, float impactForce)
    {
        Vector3 localContact = transform.InverseTransformPoint(worldContactPoint);
        float bendAmount = Mathf.Min(impactForce * forceToDepthScale, maxBendDepth);

        for (int i = 1; i < pointCount - 1; i++)
        {
            float distFromContact = Mathf.Abs(localPositions[i].x - localContact.x);
            if (distFromContact > bendSpread) continue;

            // Smooth falloff so the dent tapers, not a sharp V-shape.
            float falloff = 1f - (distFromContact / bendSpread);
            falloff = falloff * falloff; // ease it

            float addedBend = bendAmount * falloff;

            // Permanent (plastic) deformation — this is the "fold" that stays.
            permanentOffset[i] = Mathf.Max(permanentOffset[i], addedBend);
            permanentOffset[i] = Mathf.Min(permanentOffset[i], maxBendDepth);

            // Elastic kick for a snappy visual settle into place.
            velocity[i] -= addedBend * 4f;
        }

        UpdateColliderAndVisual();
    }

    private void UpdateColliderAndVisual()
    {
        Vector2[] colliderPoints = new Vector2[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float y = -(permanentOffset[i] + elasticOffset[i]); // negative = bends downward
            Vector2 point = new Vector2(localPositions[i].x, y);
            colliderPoints[i] = point;
            lineRenderer.SetPosition(i, transform.TransformPoint(point));
        }

        edgeCollider.points = colliderPoints;
    }
}