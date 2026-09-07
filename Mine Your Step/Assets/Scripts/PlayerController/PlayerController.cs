using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerControls playerInput;
    private Rigidbody2D body;

    private float horizontalMovement;
    private float baseGravityScale;

    private bool hasJumpButtonTriggered;
    private bool hasHeadButtTriggered;
    private bool hasHeadButtedThisAirtime;
    private bool isForceDown;
    private bool isHeadButting;
    private bool hasProcessedThisLanding;
    private bool hasProcessedThisHeadButt;

    // MOVEMENT

    [Header("Movement")]
    [SerializeField]
    private float speed = 8f;

    private bool isFacingRight = true;

    // JUMP

    [Header("Jump")]
    [SerializeField]
    private float jumpForce = 16f;

    [SerializeField]
    private float fallGravityMultiplier = 2.5f;

    // GROUND SLAM

    [Header("Ground Slam")]
    [SerializeField]
    private float downForce = 20f;

    // HEAD BUTT

    [Header("Head Butt")]
    [Tooltip("How high (in world units) the headbutt rises before gravity naturally brings it back down.")]
    [SerializeField]
    private float maxHeadButtHeight = 3f;

    [Tooltip("Scales how fast the headbutt launches and decelerates.")]
    [SerializeField]
    private float headButtSpeedMultiplier = 1.5f;

    [Tooltip("How much force is delivered to terrain when the headbutt connects.")]
    [SerializeField]
    private float headButtImpactForce = 20f;

    [SerializeField]
    private float headButtImpactRadius = 1.0f;

    // GROUND CHECKs

    [Header("Ground Check")]
    [SerializeField]
    private bool isGrounded;

    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private LayerMask groundLayer;

    // TERRAIN DEFORMATION

    [Header("Terrain Deformation")]
    [Tooltip("Must match the Layer your deformable platforms are set to.")]
    [SerializeField]
    private LayerMask terrainLayer;

    [Tooltip("Downward speed at impact required to register as a slam instead of a normal landing.")]
    [SerializeField]
    private float slamVelocityThreshold = 15f;

    [SerializeField]
    private float impactRadius = 1.2f;

    [SerializeField]
    private float maxImpactStrength = 40f;

    // AWAKE

    private void Awake()
    {
        playerInput = new PlayerControls();

        body = GetComponent<Rigidbody2D>();

        baseGravityScale = body.gravityScale;
    }

    // UPDATE

    private void Update()
    {
        // MOVEMENT INPUT

        Vector2 moveInput =
            playerInput.Player.Move.ReadValue<Vector2>();

        horizontalMovement = moveInput.x;

        // GROUND CHECK

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            0.05f,
            groundLayer
        ) != null;


        // Reset landing guard when leaving ground
        if (!isGrounded)
        {
            hasProcessedThisLanding = false;
        }

        // JUMP INPUT

        if (playerInput.Player.Jump.triggered && isGrounded)
        {
            hasJumpButtonTriggered = true;
        }


        // =========================
        // VARIABLE JUMP
        // =========================
        // This replaces:
        //
        // Input.GetButtonUp("Jump")
        //
        // from the old Input System.

        if (!playerInput.Player.Jump.IsPressed() &&
            body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                body.linearVelocity.y * 0.5f
            );
        }

        // GROUND SLAM

        if (playerInput.Player.Slam.triggered && !isGrounded)
        {
            isForceDown = true;

            // Cancel head butt if the player starts slamming
            isHeadButting = false;
        }

        // HEAD BUTT

        if (playerInput.Player.HeadButt.triggered &&
            !isGrounded &&
            !hasHeadButtedThisAirtime)
        {
            hasHeadButtTriggered = true;
            hasHeadButtedThisAirtime = true;
            isHeadButting = true;

            // A fresh headbutt can register an impact
            hasProcessedThisHeadButt = false;

            // Cancel ground slam
            isForceDown = false;
        }

        // FLIP PLAYER

        Flip();
    }

    private void FixedUpdate()
    {
  
        // HORIZONTAL MOVEMENT

        if (!isForceDown && !isHeadButting)
        {
            body.linearVelocity = new Vector2(
                horizontalMovement * speed,
                body.linearVelocity.y
            );
        }
        else
        {
            // Ground slam / headbutt controls horizontal movement
            body.linearVelocity = new Vector2(
                0f,
                body.linearVelocity.y
            );
        }

        // JUMP

        if (hasJumpButtonTriggered)
        {
            hasJumpButtonTriggered = false;

            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                jumpForce
            );
        }

        // HEAD BUTT

        if (hasHeadButtTriggered)
        {
            hasHeadButtTriggered = false;

            float speedMultiplier =
                Mathf.Max(0.01f, headButtSpeedMultiplier);

            float baseGravityMagnitude =
                Mathf.Abs(Physics2D.gravity.y) *
                baseGravityScale;

            float effectiveGravityMagnitude =
                baseGravityMagnitude *
                speedMultiplier *
                speedMultiplier;


            body.gravityScale =
                baseGravityScale *
                speedMultiplier *
                speedMultiplier;


            float requiredVelocity =
                Mathf.Sqrt(
                    2f *
                    effectiveGravityMagnitude *
                    Mathf.Max(
                        0.01f,
                        maxHeadButtHeight
                    )
                );


            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                requiredVelocity
            );
        }

        // HEAD BUTT PEAK

        if (isHeadButting &&
            body.linearVelocity.y <= 0f)
        {
            isHeadButting = false;
        }

        // NATURAL FALLING

        if (!isForceDown && !isHeadButting)
        {
            if (body.linearVelocity.y < 0f)
            {
                body.gravityScale =
                    baseGravityScale *
                    fallGravityMultiplier;
            }
            else
            {
                body.gravityScale =
                    baseGravityScale;
            }
        }

        // GROUND SLAM

        if (isForceDown)
        {
            body.gravityScale =
                baseGravityScale;

            body.linearVelocity = new Vector2(
                0f,
                -downForce
            );
        }

        // LANDING

        if (isGrounded)
        {
            isForceDown = false;
            isHeadButting = false;

            hasHeadButtedThisAirtime = false;

            body.gravityScale =
                baseGravityScale;
        }
    }

    // FLIP

    private void Flip()
    {
        if (isFacingRight && horizontalMovement < 0f ||
            !isFacingRight && horizontalMovement > 0f)
        {
            isFacingRight = !isFacingRight;

            Vector3 localScale =
                transform.localScale;

            localScale.x *= -1f;

            transform.localScale = localScale;
        }
    }

    // GROUND CHECK

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            0.05f,
            groundLayer
        ) != null;
    }


    // ENABLE INPUT

    private void OnEnable()
    {
        playerInput.Player.Enable();
    }


    // DISABLE INPUT


    private void OnDisable()
    {
        playerInput.Player.Disable();
    }


    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            groundCheck.position,
            0.05f
        );
    }
}

