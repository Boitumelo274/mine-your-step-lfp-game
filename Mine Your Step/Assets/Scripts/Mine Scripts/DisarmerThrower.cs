using UnityEngine;

public class DisarmerThrower : MonoBehaviour
{
    [Header("Prefab & Origin")]
    [SerializeField] private GameObject disarmerPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Shooting Mechanics")]
    [SerializeField] private float shootSpeed = 30f; // High speed for instant projectile feel
    [SerializeField] private KeyCode shootKey = KeyCode.Z;
    [SerializeField] private float cooldownTime = 0.2f;
    [SerializeField] private float disarmerLifetime = 3f;

    private float nextShootTime = 0f;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(shootKey) && Time.time >= nextShootTime)
        {
            ShootDisarmer();
            nextShootTime = Time.time + cooldownTime;
        }
    }

    private void ShootDisarmer()
    {
        if (disarmerPrefab == null)
        {
            Debug.LogError("[DisarmerThrower] Disarmer Prefab missing in Inspector!");
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Calculate spawn point
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;
        spawnPos.z = 0f;

        // 2. Get target vector from mouse position
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector2 shootDirection = (mouseWorldPos - spawnPos).normalized;

        // 3. Rotate projectile toward target direction
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 4. Instantiate projectile
        GameObject projectile = Instantiate(disarmerPrefab, spawnPos, rotation);

        // 5. Ignore player collisions
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            foreach (Collider2D col in playerColliders)
            {
                Physics2D.IgnoreCollision(col, projectileCollider);
            }
        }

        // 6. Set linear projectile physics (Zero gravity & continuous collision)
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; // Completely straight trajectory
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevents clipping through thin platforms
            rb.linearVelocity = shootDirection * shootSpeed; // Unity 6. Use rb.velocity on older Unity versions
        }

        Destroy(projectile, disarmerLifetime);
    }
}