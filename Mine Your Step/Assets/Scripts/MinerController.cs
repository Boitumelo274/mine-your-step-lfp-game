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

    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movementInput;
    private bool isGrounded;
    private bool facingRight = true;

    // These are calculated automatically by the script now
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
        // Prevents the math from breaking if moveSpeed is accidentally set to 0
        float safeMoveSpeed = Mathf.Max(moveSpeed, 0.1f);

        // Time it takes to reach the peak based on your speed and desired arc distance
        float timeToApex = jumpDistanceToPeak / safeMoveSpeed;

        // Required gravity to perfectly stop the player at the exact jump height
        defaultGravity = (2f * jumpHeight) / Mathf.Pow(timeToApex, 2);

        // Required upward burst velocity to reach that jump height
        jumpVelocity = (2f * jumpHeight) / timeToApex;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        jumpAction.action.performed -= OnJump;
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
        // Apply horizontal movement
        rb.linearVelocity = new Vector2(movementInput.x * moveSpeed, rb.linearVelocity.y);

        // Dynamically shift the gravity scale to create the custom drop speed
        if (rb.linearVelocity.y < 0) // Falling downwards
        {
            rb.gravityScale = (defaultGravity * dropGravityMultiplier) / Mathf.Abs(Physics2D.gravity.y);
        }
        else // Rising upwards or running on the ground
        {
            rb.gravityScale = defaultGravity / Mathf.Abs(Physics2D.gravity.y);
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            // Recalculates right before the jump so you can test different values in real-time
            CalculateJumpPhysics();

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            isGrounded = false;
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
            // Look at every point of contact
            for (int i = 0; i < collision.contactCount; i++)
            {
                // A normal pointing up (y > 0.5) means we touched a floor, not a wall
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    isGrounded = true;
                    return; // Stop checking, we found the floor
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Added Stay so the character doesn't randomly unground while running across flat surfaces
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