using UnityEngine;

public class DisarmerThrower : MonoBehaviour
{
    [Header("Prefab & Origin")]
    [SerializeField] private GameObject disarmerPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Shooting Mechanics")]
    [SerializeField] private float shootSpeed = 30f;
    [SerializeField] private KeyCode shootKey = KeyCode.Z;
    [SerializeField] private float cooldownTime = 0.2f;
    [SerializeField] private float disarmerLifetime = 3f;

    [Header("Ammo Settings")]
    [Tooltip("If true, automatically sets max ammo to GridManager's target mines + extra bonus.")]
    [SerializeField] private bool autoSetAmmoFromMines = true;
    [SerializeField] private int extraAmmoBonus = 1; // 1 more than the number of mines
    [SerializeField] private int maxDisarmers = 16;
    [SerializeField] private int currentDisarmers;

    // Public properties if you want to hook up a UI text counter later
    public int CurrentDisarmers => currentDisarmers;
    public int MaxDisarmers => maxDisarmers;

    private float nextShootTime = 0f;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        InitializeAmmo();
    }

    public void InitializeAmmo()
    {
        if (autoSetAmmoFromMines && GridManager.Instance != null)
        {
            maxDisarmers = GridManager.Instance.targetMines + extraAmmoBonus;
        }

        currentDisarmers = maxDisarmers;
        Debug.Log($"[DisarmerThrower] Ammo initialized: {currentDisarmers}/{maxDisarmers}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(shootKey) && Time.time >= nextShootTime)
        {
            if (currentDisarmers > 0)
            {
                ShootDisarmer();
                nextShootTime = Time.time + cooldownTime;
            }
            else
            {
                Debug.LogWarning("[DisarmerThrower] Out of disarmers! You must restart or reach a checkpoint to refill.");
            }
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

        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;
        spawnPos.z = 0f;

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector2 shootDirection = (mouseWorldPos - spawnPos).normalized;

        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        GameObject projectile = Instantiate(disarmerPrefab, spawnPos, rotation);

        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            foreach (Collider2D col in playerColliders)
            {
                Physics2D.IgnoreCollision(col, projectileCollider);
            }
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = shootDirection * shootSpeed;
        }

        // Consume 1 ammo per shot
        currentDisarmers--;
        Debug.Log($"[DisarmerThrower] Shot fired! Disarmers left: {currentDisarmers}/{maxDisarmers}");

        Destroy(projectile, disarmerLifetime);
    }

    // Called when the player dies / respawns to refill ammo
    public void ResetAmmo()
    {
        InitializeAmmo();
    }
}