using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(EdgeCollider2D))]
public class BendablePlatform : MonoBehaviour
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

    [Header("Bend Behavior")]
    [SerializeField] private float forceToDepthScale = 0.02f;
    [Tooltip("How far the beam can sag DOWNWARD from a slam impact.")]
    [SerializeField] private float maxBendDepth = 2.5f;
    [Tooltip("How far the beam can fold UPWARD from a headbutt impact.")]
    [SerializeField] private float maxUpwardBendDepth = 2.5f;
    [SerializeField] private float bendSpread = 1.5f;
    [SerializeField, Range(0, 4)] private int smoothingIterations = 2;

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

    // The original collider only ever tracked the TOP surface (the walkable side). That
    // left the underside with zero collision geometry, so a headbutt coming from below had
    // to clip most of the way through the sprite before it could ever register a hit. This
    // second collider traces the bottom surface at its real height, so headbutts land where
    // they visually should.
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

        // Look for one we already created on a previous enable (e.g. domain reload)
        // before adding a new one, so we don't accumulate duplicates.
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
        // OnValidate can fire before OnEnable has fully set things up (e.g. right after
        // the script is added in the Inspector), and Unity disallows some operations
        // (like destroying components) synchronously inside OnValidate. Deferring one
        // frame avoids both problems while still updating the Scene view immediately.
        EditorApplication.delayCall += () =>
        {
            if (this == null) return; // object may have been deleted in the meantime
            EnsureUnderCollider();
            EnsureMeshComponents();
            RebuildAll();
        };
#else
        RebuildAll();
#endif
    }

    /// <summary>
    /// If this GameObject already has a SpriteRenderer (the normal case when dragging
    /// this script onto an existing 2D sprite object), grab its sprite and sorting info,
    /// then remove it — SpriteRenderer and MeshRenderer cannot coexist on one object, and
    /// only a mesh can actually deform per-vertex.
    /// </summary>
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

        int vertCount = pointCount * 2; // bottom row + top row
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[(pointCount - 1) * 6];

        for (int i = 0; i < pointCount; i++)
        {
            float u = pointCount > 1 ? Mathf.Lerp(uMin, uMax, i / (float)(pointCount - 1)) : uMin;
            uvs[i] = new Vector2(u, vMin);                 // bottom row
            uvs[pointCount + i] = new Vector2(u, vMax);    // top row
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

        mesh.vertices = vertices; // placeholder, filled in by UpdateColliderAndVisual
        mesh.uv = uvs;
        mesh.triangles = triangles;

        meshFilter.sharedMesh = mesh;
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

    /// <summary>
    /// Call this when the player ground-slams onto the platform (bulgeUpward = false)
    /// or headbutts it from below (bulgeUpward = true). The two paths are deliberately
    /// mirrored: same falloff, same smoothing, same clamp logic — just flipped in sign
    /// and pointed at maxBendDepth vs. maxUpwardBendDepth respectively.
    /// </summary>
    public void ApplyImpact(Vector2 worldContactPoint, float impactForce, bool bulgeUpward = false)
    {
        if (localPositions == null) return; // not initialized yet (called in edit mode before OnEnable ran)

        Vector3 localContact = transform.InverseTransformPoint(worldContactPoint);

        float maxDepthForDirection = bulgeUpward ? maxUpwardBendDepth : maxBendDepth;
        float rawBend = Mathf.Min(impactForce * forceToDepthScale, maxDepthForDirection);

        // Slam bends positive (down). Headbutt bends negative (up). Everything from here
        // down uses this single signed value, so the two impact types stay symmetric.
        float bendAmount = bulgeUpward ? -rawBend : rawBend;

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
            falloff = falloff * falloff;

            float addedBend = bendAmount * falloff;

            permanentOffset[i] = bulgeUpward
                ? Mathf.Max(permanentOffset[i] + addedBend, -maxUpwardBendDepth)
                : Mathf.Min(permanentOffset[i] + addedBend, maxBendDepth);
        }

        SmoothPermanentOffset();

        // Kick the impact point in the SAME direction it just bent (positive kick for a
        // slam's downward bend, negative kick for a headbutt's upward bend) so it overshoots
        // slightly before the spring settles it back. The previous version subtracted here,
        // which sent the headbutt's transient wobble in the wrong direction on the very
        // first frames after impact.
        velocity[nearestIndex] += bendAmount * 4f;

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
        if (vertices.Length != pointCount * 2) return; // topology not built yet for current pointCount

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
}