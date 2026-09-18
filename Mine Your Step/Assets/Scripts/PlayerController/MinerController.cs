using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MinerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Kinematic Jump Arc")]
    [Tooltip("Exactly how high the jump should go in Unity units.")]
    public float jumpHeight = 3.5f;
    [Tooltip("The horizontal distance covered before reaching the peak (defines the angular arc).")]
    public float jumpDistanceToPeak = 3f;
    [Tooltip("How much faster the character plummets after reaching the peak (Drop Speed).")]
    public float dropGravityMultiplier = 2.5f;

    [Header("Ground Slam")]
    [Tooltip("How fast the character is forced downward while slamming.")]
    public float downForce = 20f;

    [Header("Head Butt")]
    [Tooltip("How high (in world units) the headbutt rises before gravity naturally brings it back down.")]
    public float maxHeadButtHeight = 3f;
    [Tooltip("Scales how fast the headbutt launches and decelerates.")]
    public float headButtSpeedMultiplier = 1.5f;

    [Header("Platform Impact")]
    [Tooltip("Scales how much a ground-slam impact bends a BendablePlatform.")]
    [SerializeField] private float slamForceMultiplier = 1f;
    [Tooltip("Fixed force applied to a BendablePlatform when headbutting it — independent of how fast the miner was moving. Increase for a deeper upward fold.")]
    [SerializeField] private float headButtImpactForce = 150f;
    [Tooltip("After a headbutt connects with a platform, zero out the vertical velocity so the miner immediately starts falling instead of drifting upward into the deformed platform.")]
    [SerializeField] private bool killVerticalVelocityOnHeadButtImpact = true;

    [Header("Slope Grip")]
    [Tooltip("How strongly the player resists sliding when grounded with no input (higher = snappier correction).")]
    [SerializeField] private float slideCorrectionStrength = 60f;

    private float lockedXPosition;
    private bool hasLockedPosition;

    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference slamAction;
    [Tooltip("Bound to a One Modifier composite in the Input Actions asset: Modifier = Shift (left/right), Binding = Space. Fires only when Space is pressed while Shift is already held.")]
    public InputActionReference headbuttAction;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movementInput;
    private bool isGrounded;
    private bool facingRight = true;

    private bool isForceDown;
    private bool isHeadButting;
    private bool hasHeadButtedThisAirtime;
    private bool hasHeadButtTriggered;

    private float defaultGravity;
    private float jumpVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        CalculateJumpPhysics();
    }

    private void CalculateJumpPhysics()
    {
        float safeMoveSpeed = Mathf.Max(moveSpeed, 0.1f);
        float timeToApex = jumpDistanceToPeak / safeMoveSpeed;

        defaultGravity = (2f * jumpHeight) / Mathf.Pow(timeToApex, 2);
        jumpVelocity = (2f * jumpHeight) / timeToApex;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        jumpAction.action.performed += OnJump;

        slamAction.action.Enable();
        slamAction.action.performed += OnSlam;

        headbuttAction.action.Enable();
        headbuttAction.action.performed += OnHeadbuttInput;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        jumpAction.action.performed -= OnJump;

        slamAction.action.Disable();
        slamAction.action.performed -= OnSlam;

        headbuttAction.action.Disable();
        headbuttAction.action.performed -= OnHeadbuttInput;
    }

    private void Update()
    {
        movementInput = moveAction.action.ReadValue<Vector2>();

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(movementInput.x));
            animator.SetBool("IsGrounded", isGrounded);
        }

        Flip();
    }

    private void FixedUpdate()
    {
        if (!isForceDown && !isHeadButting)
        {
            bool hasInput = Mathf.Abs(movementInput.x) > 0.01f;

            if (isGrounded && !hasInput)
            {
                // Standing still on ground: lock X and actively cancel any slope-induced drift.
                if (!hasLockedPosition)
                {
                    lockedXPosition = rb.position.x;
                    hasLockedPosition = true;
                }

                float drift = rb.position.x - lockedXPosition;
                float correctionVelocity = -drift * slideCorrectionStrength;

                rb.linearVelocity = new Vector2(correctionVelocity, rb.linearVelocity.y);
            }
            else
            {
                // Actively moving or airborne — normal control, release the lock.
                hasLockedPosition = false;
                rb.linearVelocity = new Vector2(movementInput.x * moveSpeed, rb.linearVelocity.y);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            hasLockedPosition = false;
        }

        // HEAD BUTT LAUNCH

        if (hasHeadButtTriggered)
        {
            hasHeadButtTriggered = false;

            float speedMultiplier = Mathf.Max(0.01f, headButtSpeedMultiplier);
            float effectiveGravity = defaultGravity * speedMultiplier * speedMultiplier;

            rb.gravityScale = effectiveGravity / Mathf.Abs(Physics2D.gravity.y);

            float requiredVelocity = Mathf.Sqrt(2f * effectiveGravity * Mathf.Max(0.01f, maxHeadButtHeight));

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, requiredVelocity);
        }

        // HEAD BUTT PEAK

        if (isHeadButting && rb.linearVelocity.y <= 0f)
        {
            isHeadButting = false;
        }

        // GRAVITY

        if (!isForceDown && !isHeadButting)
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = (defaultGravity * dropGravityMultiplier) / Mathf.Abs(Physics2D.gravity.y);
            }
            else
            {
                rb.gravityScale = defaultGravity / Mathf.Abs(Physics2D.gravity.y);
            }
        }

        // GROUND SLAM

        if (isForceDown)
        {
            rb.gravityScale = defaultGravity / Mathf.Abs(Physics2D.gravity.y);
            rb.linearVelocity = new Vector2(0f, -downForce);
        }

        // LANDING

        if (isGrounded)
        {
            isForceDown = false;
            isHeadButting = false;
            hasHeadButtedThisAirtime = false;
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // Headbutt is now its own action (Shift+Space), so this only ever does a normal jump.
        if (isGrounded)
        {
            CalculateJumpPhysics();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            isGrounded = false;
        }
    }

    private void OnHeadbuttInput(InputAction.CallbackContext context)
    {
        if (isGrounded || hasHeadButtedThisAirtime) return;

        hasHeadButtTriggered = true;
        hasHeadButtedThisAirtime = true;
        isHeadButting = true;
        isForceDown = false;
    }

    private void OnSlam(InputAction.CallbackContext context)
    {
        if (!isGrounded)
        {
            isForceDown = true;
            isHeadButting = false;
        }
    }

    private void Flip()
    {
        if ((facingRight && movementInput.x < 0f) || (!facingRight && movementInput.x > 0f))
        {
            facingRight = !facingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void EvaluateCollision(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Capture BEFORE EvaluateCollision/next FixedUpdate can clear these,
        // so we know exactly what kind of impact THIS collision was.
        bool wasSlamming = isForceDown;
        bool wasHeadButting = isHeadButting;

        EvaluateCollision(collision);

        if (!collision.gameObject.CompareTag("Ground")) return;

        BendablePlatform bendable = collision.collider.GetComponent<BendablePlatform>();
        if (bendable == null) return;

        if (wasSlamming)
        {
            ContactPoint2D contact = collision.GetContact(0);
            float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);
            float impactForce = impactSpeed * rb.mass * slamForceMultiplier;

            // Downward slam -> bends the platform DOWN (default, bulgeUpward: false).
            bendable.ApplyImpact(contact.point, impactForce);
        }
        else if (wasHeadButting)
        {
            ContactPoint2D contact = collision.GetContact(0);

            // Unlike the slam, this is a fixed, hand-tuned force rather than something
            // derived from impact velocity/mass — so the fold depth stays consistent
            // and predictable no matter how the headbutt arc was configured.
            float impactForce = headButtImpactForce;

            // Upward headbutt -> bends the platform UP into a fold (convex up / concave
            // from below), the opposite direction of a slam.
            bendable.ApplyImpact(contact.point, impactForce, bulgeUpward: true);

            // The headbutt has resolved on impact — stop the launch early and let gravity
            // take over, instead of waiting for the natural apex check in FixedUpdate.
            isHeadButting = false;

            if (killVerticalVelocityOnHeadButtImpact)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}