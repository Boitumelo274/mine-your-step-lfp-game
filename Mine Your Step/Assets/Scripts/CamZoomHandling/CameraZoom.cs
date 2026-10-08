using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    public enum ZoomMode
    {
        Hold,   // zoomed out only while the button is held down
        Toggle  // press once to zoom out, press again to zoom back in
    }

    [Header("Input")]
    [Tooltip("The 'ZoomOut' action from your Input Actions asset.")]
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private ZoomMode mode = ZoomMode.Hold;

    [Header("Zoom")]
    [Tooltip("How many times bigger the view gets when zoomed out. 2 = sees twice as much height (and width) as normal. Always zooms OUT, whatever your normal camera size is.")]
    [SerializeField, Min(1.05f)] private float zoomOutMultiplier = 2f;
    [Tooltip("Seconds to glide between the two sizes. Higher = slower and smoother.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.4f;

    //[Header("Freeze Player")]
    //[Tooltip("Found automatically (the object with PlayerHealth) if left empty.")]
    //[SerializeField] private GameObject player;
    //[Tooltip("Scripts switched off while the camera is zoomed. If left empty, the player's MinerController is used.")]
    //[SerializeField] private Behaviour[] disableWhileZoomed;

    [Header("References (found automatically if empty)")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private Camera fallbackCamera;
    private float normalSize;
    private float currentSize;
    private float velocity;
    private bool toggledOut;

    //private Rigidbody2D playerBody;
    //private Animator playerAnimator;
    //private PlayerHealth playerHealth;
    //private PlayerRespawn playerRespawn;
    //private MinerMining playerMining;

    /// <summary>True from the moment the zoom button is pressed until the camera is back to normal.</summary>
    public bool IsFrozen { get; private set; }

    private void Awake()
    {
        if (cinemachineCamera == null) cinemachineCamera = GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null) cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera == null) fallbackCamera = Camera.main;

        normalSize = ReadSize();
        currentSize = normalSize;

        //if (player == null)
        //{
        //    PlayerHealth found = FindFirstObjectByType<PlayerHealth>();
        //    if (found != null) player = found.gameObject;
        //}

        //if (player != null)
        //{
        //    playerBody = player.GetComponent<Rigidbody2D>();
        //    playerAnimator = player.GetComponent<Animator>();
        //    playerHealth = player.GetComponent<PlayerHealth>();
        //    playerRespawn = player.GetComponent<PlayerRespawn>();
        //    playerMining = player.GetComponent<MinerMining>();

        //    if (disableWhileZoomed == null || disableWhileZoomed.Length == 0)
        //    {
        //        MinerController controller = player.GetComponent<MinerController>();
        //        if (controller != null) disableWhileZoomed = new Behaviour[] { controller };
        //    }
        //}
        //else
        //{
        //    Debug.LogWarning("[CameraZoom] No player found, so the player won't be frozen while zooming.", this);
        //}
    }

    private void OnEnable()
    {
        if (zoomAction != null) zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        if (zoomAction != null) zoomAction.action.Disable();
    }

    private void Update()
    {
        if (zoomAction == null) return;

        // Zooming isn't allowed while the miner is dead, respawning or in the middle of mining.
        bool allowed = CanZoom();

        bool wantZoomOut = false;
        if (allowed)
        {
            if (mode == ZoomMode.Hold)
            {
                wantZoomOut = zoomAction.action.IsPressed();
            }
            else
            {
                if (zoomAction.action.WasPressedThisFrame()) toggledOut = !toggledOut;
                wantZoomOut = toggledOut;
            }
        }
        else
        {
            toggledOut = false;
        }

        // Freeze the instant the button goes down.
        //if (wantZoomOut && !IsFrozen) SetFrozen(true);

        float target = wantZoomOut ? normalSize * zoomOutMultiplier : normalSize;
        bool arrived = Mathf.Abs(currentSize - target) < 0.001f && Mathf.Abs(velocity) < 0.001f;

        if (!arrived)
        {
            // Unscaled time, so zooming still works if the game is paused.
            currentSize = Mathf.SmoothDamp(currentSize, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            WriteSize(currentSize);
        }

        // Unfreeze only once the button is released AND the camera is back at its original size.
        //if (IsFrozen && !wantZoomOut && Mathf.Abs(currentSize - normalSize) < 0.02f)
        //{
        //    currentSize = normalSize;
        //    velocity = 0f;
        //    WriteSize(currentSize);
        //    SetFrozen(false);
        //}
    }

    private bool CanZoom()
    {
        //if (playerHealth != null && playerHealth.IsDead) return false;
        //if (playerRespawn != null && playerRespawn.IsRespawning) return false;
        //if (playerMining != null && playerMining.IsMining) return false;
        return true;
    }

    private void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (frozen)
        {
            SetControl(false);

            //if (playerBody != null) playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
            //if (playerAnimator != null) playerAnimator.SetFloat("Speed", 0f); // so the run animation doesn't keep playing
        }
        else
        {
            // Don't hand control back if death or a respawn has taken over in the meantime.
            //bool dead = playerHealth != null && playerHealth.IsDead;
            //bool respawning = playerRespawn != null && playerRespawn.IsRespawning;
            //if (!dead && !respawning) SetControl(true);
        }
    }

    private void SetControl(bool enabled)
    {
        //if (disableWhileZoomed == null) return;

        //foreach (Behaviour b in disableWhileZoomed)
        //{
        //    if (b == null) continue;

        //    // Switching the Animator off would freeze the sprite mid-pose, so never do that.
        //    if (b is Animator) continue;

        //    b.enabled = enabled;
        //}
    }

    private float ReadSize()
    {
        if (cinemachineCamera != null) return cinemachineCamera.Lens.OrthographicSize;
        if (fallbackCamera != null) return fallbackCamera.orthographicSize;
        return 5f;
    }

    private void WriteSize(float size)
    {
        if (cinemachineCamera != null)
        {
            LensSettings lens = cinemachineCamera.Lens;
            lens.OrthographicSize = size;
            cinemachineCamera.Lens = lens;
        }
        else if (fallbackCamera != null)
        {
            fallbackCamera.orthographicSize = size;
        }
    }
}