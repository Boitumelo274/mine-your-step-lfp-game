using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class RollingBallCheckpointable2D : MonoBehaviour, ICheckpointable
{
    private Rigidbody2D rb;

    private struct BallState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector2 linearVelocity;
        public float angularVelocity;
        public bool isActive;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Registers the ball with CheckpointManager even if spawned mid-game
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.Register(this);
        }
    }

    public object CaptureState()
    {
        return new BallState
        {
            position = transform.position,
            rotation = transform.rotation,
#if UNITY_6000_0_OR_NEWER
            linearVelocity = rb.linearVelocity,
#else
            linearVelocity = rb.velocity,
#endif
            angularVelocity = rb.angularVelocity,
            isActive = gameObject.activeSelf
        };
    }

    public void RestoreState(object state)
    {
        if (state is BallState savedState)
        {
            // Restore position, rotation, and active status
            transform.position = savedState.position;
            transform.rotation = savedState.rotation;
            gameObject.SetActive(savedState.isActive);

            // Restore physics velocity & clear old momentum
            if (rb != null)
            {
                rb.position = savedState.position;
                rb.rotation = savedState.rotation.eulerAngles.z;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = savedState.linearVelocity;
#else
                rb.velocity = savedState.linearVelocity;
#endif
                rb.angularVelocity = savedState.angularVelocity;
            }
        }
    }
}