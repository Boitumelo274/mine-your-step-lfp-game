using System.Collections;
using UnityEngine;

public class MenuMinerAnimation : MonoBehaviour
{
    [Header("Left Side Waypoints")]
    public Transform emptyObject1; // Left off-screen start/return
    public Transform emptyObject2; // Left on-screen mining spot

    [Header("Right Side Waypoints")]
    public Transform emptyObject3; // Right off-screen start/return
    public Transform emptyObject4; // Right on-screen mining spot

    [Header("Movement Settings")]
    public float runSpeed = 5f;
    public bool spriteFacesRightByDefault = true;

    [Header("Adjustable Timings (Seconds)")]
    public float initialStartDelay = 3f;
    public float mineDownDuration = 3f;
    public float idleDuration = 2f;
    public float delayBetweenSides = 1f;

    [Header("Animation State Names")]
    public string runStateName = "run";
    public string mineDownStateName = "mineDown";
    public string idleStateName = "idle";

    private Animator animator;
    private Vector3 baseScale;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
    }

    private void Start()
    {
        // Place the miner off-screen at the start
        if (emptyObject1 != null)
        {
            transform.position = emptyObject1.position;
        }

        StartCoroutine(PlayMenuSequence());
    }

    private IEnumerator PlayMenuSequence()
    {
        // Wait a few seconds after the main menu loads
        yield return new WaitForSeconds(initialStartDelay);

        // Loops continuously between the left side and right side
        while (true)
        {
            // Perform sequence on the Left Side (emptyObject1 -> emptyObject2 -> emptyObject1)
            yield return StartCoroutine(RunSideRoutine(emptyObject1, emptyObject2));

            yield return new WaitForSeconds(delayBetweenSides);

            // Perform sequence on the Right Side (emptyObject3 -> emptyObject4 -> emptyObject3)
            yield return StartCoroutine(RunSideRoutine(emptyObject3, emptyObject4));

            yield return new WaitForSeconds(delayBetweenSides);
        }
    }

    private IEnumerator RunSideRoutine(Transform startPoint, Transform targetPoint)
    {
        // 1. Teleport to the starting off-screen point
        transform.position = startPoint.position;

        // 2. Face towards the on-screen target and run to it
        FaceTarget(targetPoint.position);
        animator.Play(runStateName);
        yield return StartCoroutine(MoveToPoint(targetPoint.position));

        // 3. Play "mineDown" animation for the set duration
        animator.Play(mineDownStateName);
        yield return new WaitForSeconds(mineDownDuration);

        // 4. Play "idle" animation for the set duration
        animator.Play(idleStateName);
        yield return new WaitForSeconds(idleDuration);

        // 5. Turn around and run back off-screen to the start point
        FaceTarget(startPoint.position);
        animator.Play(runStateName);
        yield return StartCoroutine(MoveToPoint(startPoint.position));
    }

    private IEnumerator MoveToPoint(Vector3 destination)
    {
        while (Vector2.Distance(transform.position, destination) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                destination,
                runSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.position = destination;
    }

    private void FaceTarget(Vector3 target)
    {
        float direction = target.x - transform.position.x;
        if (Mathf.Abs(direction) > 0.01f)
        {
            float scaleX = Mathf.Abs(baseScale.x);
            bool movingRight = direction > 0;

            if (!spriteFacesRightByDefault)
            {
                movingRight = !movingRight;
            }

            transform.localScale = new Vector3(
                movingRight ? scaleX : -scaleX,
                baseScale.y,
                baseScale.z
            );
        }
    }
}