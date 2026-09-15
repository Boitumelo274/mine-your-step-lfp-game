using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class BendablePlatform : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField, Min(3)] private int pointCount = 16;
    [SerializeField, Min(0.1f)] private float platformLength = 8f;

    [Header("Bend Behavior")]
    [SerializeField] private float forceToDepthScale = 0.02f;
    [SerializeField] private float maxBendDepth = 2.5f;
    [SerializeField] private float maxUpwardBendDepth = 2.5f; 
    [SerializeField] private float bendSpread = 1.5f;
    [SerializeField, Range(0, 4)] private int smoothingIterations = 2;

    [Header("Settle Physics")]
    [SerializeField] private float springStiffness = 40f;
    [SerializeField] private float damping = 6f;
    [SerializeField] private float settleThreshold = 0.0015f;

    private Vector2[] localPositions;
    private float[] permanentOffset;
    private float[] elasticOffset;
    private float[] velocity;

    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;
    private bool isSettled = true;

    private void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        BuildBeam();
    }

    private void OnValidate()
    {
        if (pointCount < 3) pointCount = 3;
    }

    private void BuildBeam()
    {
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
        if (isSettled) return;

        bool anyMovement = false;

        for (int i = 1; i < pointCount - 1; i++)
        {
            float displacement = elasticOffset[i];
            float force = -springStiffness * displacement - damping * velocity[i];

            velocity[i] += force * Time.fixedDeltaTime;
            elasticOffset[i] += velocity[i] * Time.fixedDeltaTime;

            if (Mathf.Abs(velocity[i]) < settleThreshold && Mathf.Abs(elasticOffset[i]) < settleThreshold)
            {
                velocity[i] = 0f;
                elasticOffset[i] = 0f;
            }
            else
            {
                anyMovement = true;
            }
        }

        UpdateColliderAndVisual();

        if (!anyMovement)
        {
            isSettled = true; // shape is now locked — no more recalculation until the next impact
        }
    }

    // Call this when the player ground-slams onto the platform.
    // worldContactPoint = where they hit; impactForce = how hard (e.g. mass * fall speed).

    public void ApplyImpact(Vector2 worldContactPoint, float impactForce)
    {
        Vector3 localContact = transform.InverseTransformPoint(worldContactPoint);
        float bendAmount = Mathf.Min(impactForce * forceToDepthScale, maxBendDepth);

        // Find the nearest beam point to the contact — used as the elastic "kick" origin.
        int nearestIndex = 0;
        float nearestDist = float.MaxValue;

        for (int i = 1; i < pointCount - 1; i++)
        {
            float distFromContact = Mathf.Abs(localPositions[i].x - localContact.x);

            if (distFromContact < nearestDist)
            {
                nearestDist = distFromContact;
                nearestIndex = i;
            }

            if (distFromContact > bendSpread) continue;

            float falloff = 1f - (distFromContact / bendSpread);
            falloff = falloff * falloff; // ease it — smooth taper, not a linear ramp

            float addedBend = bendAmount * falloff;

            // Additive plastic deformation, clamped — repeated slams deepen the dent further.
            permanentOffset[i] = Mathf.Min(permanentOffset[i] + addedBend, maxBendDepth);
        }

        SmoothPermanentOffset();

        // Elastic kick for a snappy visual settle, centered on the actual impact point.
        velocity[nearestIndex] -= bendAmount * 4f;

        isSettled = false;
        UpdateColliderAndVisual();
    }

    // Averages each point with its neighbors so the permanent dent has no sharp
    // creases where the affected radius ends — a smooth curve instead of a V-shape.

    private void SmoothPermanentOffset()
    {
        for (int pass = 0; pass < smoothingIterations; pass++)
        {
            float[] smoothed = new float[pointCount];
            smoothed[0] = permanentOffset[0];
            smoothed[pointCount - 1] = permanentOffset[pointCount - 1];

            for (int i = 1; i < pointCount - 1; i++)
            {
                smoothed[i] = (permanentOffset[i - 1] + permanentOffset[i] * 2f + permanentOffset[i + 1]) / 4f;
            }

            permanentOffset = smoothed;
        }
    }


    // Flattens the platform back to its original shape — useful for repeatable puzzles.

    public void ResetShape()
    {
        for (int i = 0; i < pointCount; i++)
        {
            permanentOffset[i] = 0f;
            elasticOffset[i] = 0f;
            velocity[i] = 0f;
        }

        isSettled = true;
        UpdateColliderAndVisual();
    }

    private void UpdateColliderAndVisual()
    {
        Vector2[] colliderPoints = new Vector2[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float y = -(permanentOffset[i] + elasticOffset[i]);
            Vector2 point = new Vector2(localPositions[i].x, y);
            colliderPoints[i] = point;
            lineRenderer.SetPosition(i, transform.TransformPoint(point));
        }

        edgeCollider.points = colliderPoints;
    }
}

