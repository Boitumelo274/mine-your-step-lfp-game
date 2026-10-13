using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(EdgeCollider2D))]
public class BendablePlatform : MonoBehaviour, ICheckpointable
{
    [Header("Shape")]
    [Tooltip("Auto-filled from this object's SpriteRenderer the moment the script is added. You can reassign it afterward to swap textures.")]
    [SerializeField] private Sprite sourceSprite;
    [SerializeField, Min(3)] private int pointCount = 16;
    [Tooltip("Used only if there's no sprite assigned.")]
    [SerializeField] private float fallbackLength = 8f;
    [SerializeField] private float fallbackThickness = 1f;
    [SerializeField] private Color fallbackColor = Color.gray;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Deformability")]
    [Tooltip("If disabled, ground-slam impacts (bulgeUpward = false) do nothing to this platform.")]
    [SerializeField] private bool canDeformDownward = true;
    [Tooltip("If disabled, headbutt impacts (bulgeUpward = true) do nothing to this platform.")]
    [SerializeField] private bool canDeformUpward = true;

    [Header("Bend Behavior")]
    [Tooltip("How far the beam can sag DOWNWARD from a slam impact.")]
    [SerializeField] private float maxBendDepth = 2.5f;
    [Tooltip("How far the beam can fold UPWARD from a headbutt impact.")]
    [SerializeField] private float maxUpwardBendDepth = 2.5f;
    [Tooltip("How many full-strength impacts, landing at the same spot, it takes to reach Max Bend Depth (or Max Upward Bend Depth for a headbutt). The depth added per hit is simply maxDepth / this number — impactForce no longer scales it, so the hit count needed is always consistent no matter how hard or soft the collision was.")]
    [SerializeField, Min(1)] private int hitsToReachMaxDepth = 3;
    [SerializeField] private float bendSpread = 1.5f;
    [SerializeField, Range(0, 4)] private int smoothingIterations = 2;
    [Tooltip("If enabled, the two end points of the beam are anchored at zero offset (like a bridge pinned to two supports) and never deform, no matter how close an impact lands. Disable this to let impacts near the tips bend the edges too.")]
    [SerializeField] private bool pinEdges = false;

    [Header("Settle Physics")]
    [SerializeField] private float springStiffness = 40f;
    [SerializeField] private float damping = 6f;
    [SerializeField] private float settleThreshold = 0.0015f;

    private float platformLength;
    private float thickness;
    private float uMin, uMax, vMin, vMax;

    private Vector2[] localPositions;
    private float[] permanentOffset;
    private float[] elasticOffset;
    private float[] velocity;

    private EdgeCollider2D edgeCollider;
    private EdgeCollider2D underEdgeCollider;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private bool isSettled = true;

    private void OnEnable()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        EnsureUnderCollider();

        ConvertSpriteRendererIfPresent();
        EnsureMeshComponents();
        RebuildAll();
    }

    private void EnsureUnderCollider()
    {
        if (underEdgeCollider != null) return;

        EdgeCollider2D[] existing = GetComponents<EdgeCollider2D>();
        if (existing.Length > 1)
        {
            underEdgeCollider = existing[1];
            return;
        }

        underEdgeCollider = gameObject.AddComponent<EdgeCollider2D>();
    }

    private void OnValidate()
    {
        if (pointCount < 3) pointCount = 3;

#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            EnsureUnderCollider();
            EnsureMeshComponents();
            RebuildAll();
        };
#else
        RebuildAll();
#endif
    }

    private void ConvertSpriteRendererIfPresent()
    {
        SpriteRenderer existing = GetComponent<SpriteRenderer>();
        if (existing == null) return;

        if (sourceSprite == null) sourceSprite = existing.sprite;
        sortingLayerName = existing.sortingLayerName;
        sortingOrder = existing.sortingOrder;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(existing);
            Debug.Log($"[BendablePlatform] Replaced the SpriteRenderer on '{name}' with a bendable mesh using the same sprite.", this);
            return;
        }
#endif
        Destroy(existing);
    }

    private void EnsureMeshComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    private void RebuildAll()
    {
        if (edgeCollider == null || meshFilter == null || meshRenderer == null) return;

        ReadSourceVisual();
        BuildBeam();
        BuildMeshTopology();
        UpdateColliderAndVisual();
    }

    private void ReadSourceVisual()
    {
        Material material;

        if (sourceSprite != null)
        {
            platformLength = sourceSprite.bounds.size.x;
            thickness = sourceSprite.bounds.size.y;

            Rect rect = sourceSprite.rect;
            Texture2D texture = sourceSprite.texture;

            uMin = rect.x / texture.width;
            uMax = (rect.x + rect.width) / texture.width;
            vMin = rect.y / texture.height;
            vMax = (rect.y + rect.height) / texture.height;

            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = texture;
        }
        else
        {
            platformLength = fallbackLength;
            thickness = fallbackThickness;
            uMin = uMax = vMin = vMax = 0f;

            material = new Material(Shader.Find("Sprites/Default"));
            material.color = fallbackColor;
        }

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.sharedMaterial = material;
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
    }

    private void BuildMeshTopology()
    {
        if (mesh == null) mesh = new Mesh { name = "BendablePlatformMesh" };
        else mesh.Clear();

        int vertCount = pointCount * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[(pointCount - 1) * 6];

        for (int i = 0; i < pointCount; i++)
        {
            float u = pointCount > 1 ? Mathf.Lerp(uMin, uMax, i / (float)(pointCount - 1)) : uMin;
            uvs[i] = new Vector2(u, vMin);
            uvs[pointCount + i] = new Vector2(u, vMax);
        }

        int tri = 0;
        for (int i = 0; i < pointCount - 1; i++)
        {
            int bottomA = i;
            int bottomB = i + 1;
            int topA = pointCount + i;
            int topB = pointCount + i + 1;

            triangles[tri++] = bottomA;
            triangles[tri++] = topA;
            triangles[tri++] = bottomB;

            triangles[tri++] = bottomB;
            triangles[tri++] = topA;
            triangles[tri++] = topB;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        meshFilter.sharedMesh = mesh;
    }

    private void FixedUpdate()
    {
        if (isSettled) return;

        bool anyMovement = false;

        int startIndex = pinEdges ? 1 : 0;
        int endIndexExclusive = pinEdges ? pointCount - 1 : pointCount;

        for (int i = startIndex; i < endIndexExclusive; i++)
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
            isSettled = true;
        }
    }

    public void ApplyImpact(Vector2 worldContactPoint, float impactForce, bool bulgeUpward = false)
    {
        if (localPositions == null) return;

        if (bulgeUpward && !canDeformUpward) return;
        if (!bulgeUpward && !canDeformDownward) return;

        Vector3 localContact = transform.InverseTransformPoint(worldContactPoint);

        float maxDepthForDirection = bulgeUpward ? maxUpwardBendDepth : maxBendDepth;
        float perHitDepth = maxDepthForDirection / hitsToReachMaxDepth;

        float bendAmount = bulgeUpward ? -perHitDepth : perHitDepth;

        int nearestIndex = 0;
        float nearestDist = float.MaxValue;

        int startIndex = pinEdges ? 1 : 0;
        int endIndexExclusive = pinEdges ? pointCount - 1 : pointCount;

        int minAffected = -1;
        int maxAffected = -1;

        for (int i = startIndex; i < endIndexExclusive; i++)
        {
            float distFromContact = Mathf.Abs(localPositions[i].x - localContact.x);

            if (distFromContact < nearestDist)
            {
                nearestDist = distFromContact;
                nearestIndex = i;
            }

            if (distFromContact > bendSpread) continue;

            float falloff = 1f - (distFromContact / bendSpread);
            falloff = falloff * falloff;

            float addedBend = bendAmount * falloff;

            permanentOffset[i] = bulgeUpward
                ? Mathf.Max(permanentOffset[i] + addedBend, -maxUpwardBendDepth)
                : Mathf.Min(permanentOffset[i] + addedBend, maxBendDepth);

            if (minAffected == -1) minAffected = i;
            maxAffected = i;
        }

        if (minAffected != -1)
        {
            SmoothPermanentOffset(minAffected, maxAffected);
        }

        velocity[nearestIndex] += bendAmount * 4f;

        isSettled = false;
        UpdateColliderAndVisual();
    }

    private void SmoothPermanentOffset(int rangeStart, int rangeEnd)
    {
        rangeStart = Mathf.Clamp(rangeStart, 0, pointCount - 1);
        rangeEnd = Mathf.Clamp(rangeEnd, 0, pointCount - 1);

        int length = rangeEnd - rangeStart + 1;

        for (int pass = 0; pass < smoothingIterations; pass++)
        {
            float[] smoothed = new float[length];

            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                int localIndex = i - rangeStart;

                if (i == 0)
                {
                    smoothed[localIndex] = pinEdges
                        ? permanentOffset[0]
                        : (permanentOffset[0] * 2f + permanentOffset[1]) / 3f;
                }
                else if (i == pointCount - 1)
                {
                    smoothed[localIndex] = pinEdges
                        ? permanentOffset[pointCount - 1]
                        : (permanentOffset[pointCount - 1] * 2f + permanentOffset[pointCount - 2]) / 3f;
                }
                else
                {
                    smoothed[localIndex] = (permanentOffset[i - 1] + permanentOffset[i] * 2f + permanentOffset[i + 1]) / 4f;
                }
            }

            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                permanentOffset[i] = smoothed[i - rangeStart];
            }
        }
    }

    public void ResetShape()
    {
        if (permanentOffset == null) return;

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
        if (mesh == null || localPositions == null) return;

        float halfThickness = thickness / 2f;

        Vector3[] vertices = mesh.vertices;
        if (vertices.Length != pointCount * 2) return;

        Vector2[] topPoints = new Vector2[pointCount];
        Vector2[] bottomPoints = new Vector2[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float sag = permanentOffset[i] + elasticOffset[i];

            float bottomY = -halfThickness - sag;
            float topY = halfThickness - sag;

            vertices[i] = new Vector3(localPositions[i].x, bottomY, 0f);
            vertices[pointCount + i] = new Vector3(localPositions[i].x, topY, 0f);

            topPoints[i] = new Vector2(localPositions[i].x, topY);
            bottomPoints[i] = new Vector2(localPositions[i].x, bottomY);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        edgeCollider.points = topPoints;

        if (underEdgeCollider != null)
        {
            underEdgeCollider.points = bottomPoints;
        }
    }

    [ContextMenu("Preview/Test Bend Down (Slam)")]
    private void TestBendDown() => PreviewImpact(bulgeUpward: false);

    [ContextMenu("Preview/Test Bend Up (Headbutt)")]
    private void TestBendUp() => PreviewImpact(bulgeUpward: true);

    [ContextMenu("Preview/Reset Shape")]
    private void TestReset() => ResetShape();

    private void PreviewImpact(bool bulgeUpward)
    {
        ApplyImpact(transform.position, 150f, bulgeUpward);

        if (elasticOffset != null)
        {
            for (int i = 0; i < pointCount; i++)
            {
                elasticOffset[i] = 0f;
                velocity[i] = 0f;
            }
        }
        isSettled = true;
        UpdateColliderAndVisual();
    }

    // --- ICheckpointable Implementation ---

    public object CaptureState()
    {
        return permanentOffset != null ? (float[])permanentOffset.Clone() : new float[0];
    }

    public void RestoreState(object state)
    {
        if (state is float[] saved)
        {
            if (saved.Length != pointCount) return;

            for (int i = 0; i < pointCount; i++)
            {
                permanentOffset[i] = saved[i];
                elasticOffset[i] = 0f;
                velocity[i] = 0f;
            }
            isSettled = true;
            UpdateColliderAndVisual();
        }
    }
}