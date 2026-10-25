using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class LevelExitElevator : MonoBehaviour
{
    [Header("Open Conditions")]
    [Tooltip("OFF = the elevator is always allowed to open (testing). ON = the player needs enough jewels, the same rule as the old door.")]
    [SerializeField] private bool requireJewels = false;
    [Tooltip("Name of the jewel to count, matching the Jewel Name on your jewel blocks. Leave empty to count ALL jewels in the inventory.")]
    [SerializeField] private string jewelName = "Jewel";
    [Tooltip("Jewels needed = number of mines in the grid + this many extra.")]
    [SerializeField] private int extraJewelsRequired = 5;

    [Header("Destination")]
    [Tooltip("Empty object placed where the elevator should stop. The elevator moves to its position.")]
    [SerializeField] private Transform destination;

    [Header("Timing")]
    [Tooltip("Seconds between the player getting in and the doors starting to close.")]
    [SerializeField, Min(0f)] private float doorCloseDelay = 0.5f;
    [Tooltip("Seconds the closed doors wait before the elevator starts moving.")]
    [SerializeField, Min(0f)] private float waitBeforeMoving = 0.5f;
    [Tooltip("How long the ride takes, in seconds. It eases in and out.")]
    [SerializeField, Min(0.1f)] private float travelDuration = 5f;
    [Tooltip("Seconds the elevator waits after arriving, before the doors open.")]
    [SerializeField, Min(0f)] private float waitBeforeOpening = 0.4f;
    [Tooltip("While the doors close, the player slides to the middle of the elevator.")]
    [SerializeField] private bool centerPlayerInside = true;

    [Header("Door Visuals")]
    [SerializeField] private SpriteRenderer doorSpriteRenderer;
    [Tooltip("Shown while the elevator is closed: before it's allowed to open, and during the ride.")]
    [SerializeField] private Sprite closedDoorSprite;
    [Tooltip("Shown when the player can get in, and again when the elevator arrives.")]
    [SerializeField] private Sprite openDoorSprite;

    [Header("Drawing Order")]
    [Tooltip("On = the door is drawn BEHIND the player while open (player stands inside it) and IN FRONT of the player while closed for the ride (player is hidden inside).")]
    [SerializeField] private bool controlSorting = true;
    [Tooltip("The player's sprite. Found automatically if left empty.")]
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("When the ride ends")]
    [Tooltip("Runs when the elevator arrives at the top. Use it for sounds, switching on the next level's objects, etc.")]
    [SerializeField] private UnityEvent onArrived;

    private enum State { Waiting, Riding, Done }
    private State state = State.Waiting;

    private Collider2D trigger;
    private PlayerInventory inventory;
    private int totalRequiredJewels;
    private bool lockedMessageShown;

    private void Start()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true; // so the player can walk into it

        if (doorSpriteRenderer == null) doorSpriteRenderer = GetComponent<SpriteRenderer>();

        if (destination == null)
            Debug.LogWarning("[LevelExitElevator] No Destination set, so the elevator can't move anywhere.", this);

        if (playerSprite == null)
        {
            MinerController miner = FindFirstObjectByType<MinerController>();
            if (miner != null) playerSprite = miner.GetComponentInChildren<SpriteRenderer>();
        }

        if (requireJewels)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[LevelExitElevator] No PlayerInventory found, so the jewel requirement can never be met.", this);
            }
            else
            {
                inventory.OnChanged += RefreshDoorLook; // unlock (or re-lock) the moment the jewel count changes
            }

            int mineCount = 15; // fallback if there is no grid
            if (GridManager.Instance != null) mineCount = GridManager.Instance.targetMines;
            totalRequiredJewels = mineCount + extraJewelsRequired;
            Debug.Log($"[LevelExitElevator] Jewels required to open: {totalRequiredJewels} ({mineCount} mines + {extraJewelsRequired} extra)");
        }

        // Doors start open if the elevator is allowed to open, closed otherwise.
        // A closed-but-locked elevator stays BEHIND the player, so they can walk past it.
        RefreshDoorLook();
    }

    // ---------------------------------------------------------------------------------------
    // CONDITIONS
    // ---------------------------------------------------------------------------------------
    private bool ConditionsMet()
    {
        if (!requireJewels) return true; // testing mode: always allowed to open

        if (inventory == null) return false;

        int playerJewels = !string.IsNullOrEmpty(jewelName) ? inventory.Count(jewelName) : inventory.Total;
        return playerJewels >= totalRequiredJewels;
    }

    // Keeps the doors' look in sync with the conditions: open when the player has enough jewels,
    // closed (and behind the player) when they don't. Does nothing during a ride.
    private void RefreshDoorLook()
    {
        if (state != State.Waiting) return;

        if (ConditionsMet()) ShowOpen();
        else ShowClosed(false);
    }

    // ---------------------------------------------------------------------------------------

    // Stay (not Enter) so we only react once the player's centre is truly inside the elevator.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (state != State.Waiting || destination == null) return;

        MinerController miner = other.GetComponentInParent<MinerController>();
        if (miner == null) return;

        if (!CenterInsideTrigger(other)) return;

        if (!ConditionsMet())
        {
            if (!lockedMessageShown)
            {
                lockedMessageShown = true;
                int have = inventory != null
                    ? (!string.IsNullOrEmpty(jewelName) ? inventory.Count(jewelName) : inventory.Total)
                    : 0;
                Debug.Log($"[LevelExitElevator] Locked! You need {totalRequiredJewels - have} more jewels.");
            }
            return;
        }

        state = State.Riding;
        StartCoroutine(RideRoutine(miner));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<MinerController>() != null) lockedMessageShown = false;
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= RefreshDoorLook;
    }

    private bool CenterInsideTrigger(Collider2D other)
    {
        Vector2 c = other.bounds.center;
        Bounds b = trigger.bounds;
        return c.x >= b.min.x && c.x <= b.max.x && c.y >= b.min.y && c.y <= b.max.y;
    }

    private IEnumerator RideRoutine(MinerController miner)
    {
        Transform player = miner.transform;
        Rigidbody2D rb = miner.GetComponent<Rigidbody2D>();
        Animator anim = miner.GetComponent<Animator>();
        PlayerHealth health = miner.GetComponent<PlayerHealth>();

        // 1. Freeze the player. Kinematic so physics can't push them around while we carry them.
        RigidbodyType2D originalBodyType = rb != null ? rb.bodyType : RigidbodyType2D.Dynamic;
        miner.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (anim != null) anim.SetFloat("Speed", 0f);

        // 2. Short pause (player slides to the middle), then the doors close.
        float centerX = trigger.bounds.center.x;
        float t = 0f;
        while (t < doorCloseDelay)
        {
            t += Time.deltaTime;
            if (centerPlayerInside)
            {
                float newX = Mathf.MoveTowards(player.position.x, centerX, 3f * Time.deltaTime);
                player.position = new Vector3(newX, player.position.y, player.position.z);
            }
            yield return null;
        }

        ShowClosed(true); // doors close in front of the player, who is now inside
        yield return new WaitForSeconds(waitBeforeMoving);

        // 3. The ride. The player moves by exactly the same amount as the elevator each frame.
        Vector3 start = transform.position;
        Vector3 end = destination.position;
        t = 0f;
        while (t < travelDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / travelDuration));
            Vector3 newPos = Vector3.Lerp(start, end, k);

            Vector3 delta = newPos - transform.position;
            transform.position = newPos;
            player.position += delta;

            yield return null;
        }

        // 4. Arrived: doors open, player gets control back.
        yield return new WaitForSeconds(waitBeforeOpening);
        ShowOpen();

        if (rb != null) rb.bodyType = originalBodyType;
        bool dead = health != null && health.IsDead;
        if (!dead) miner.enabled = true;

        state = State.Done;
        onArrived?.Invoke();
    }

    private void ShowOpen()
    {
        SetSprite(openDoorSprite);
        ApplySorting(false); // behind the player: they stand inside the open elevator
    }

    private void ShowClosed(bool coverPlayer)
    {
        SetSprite(closedDoorSprite);
        ApplySorting(coverPlayer); // in front of the player when they're riding inside
    }

    // Puts the door one step behind or in front of the player in the drawing order.
    private void ApplySorting(bool doorInFront)
    {
        if (!controlSorting || doorSpriteRenderer == null || playerSprite == null) return;

        doorSpriteRenderer.sortingLayerID = playerSprite.sortingLayerID;
        doorSpriteRenderer.sortingOrder = playerSprite.sortingOrder + (doorInFront ? 1 : -1);
    }

    private void SetSprite(Sprite sprite)
    {
        if (doorSpriteRenderer != null && sprite != null) doorSpriteRenderer.sprite = sprite;
    }

    private void OnDrawGizmosSelected()
    {
        if (destination == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.4f);
    }
}