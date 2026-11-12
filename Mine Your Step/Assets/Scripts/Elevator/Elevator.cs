using System.Collections;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform topPoint;
    public Transform bottomPoint;

    [Header("Settings")]
    public float speed = 3f;
    public float waitTime = 1f; // How long it pauses at each end

    private Transform currentTarget;
    private bool isWaiting = false;

    private void Start()
    {
        currentTarget = topPoint; // Starts by moving to the top
    }

    private void Update()
    {
        if (isWaiting) return;

        // Move the platform exactly toward the target point
        transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, speed * Time.deltaTime);

        // Check if the platform has arrived
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.05f)
        {
            StartCoroutine(WaitAndSwitchDirection());
        }
    }

    private IEnumerator WaitAndSwitchDirection()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        // Swap the target to go the other way
        if (currentTarget == topPoint)
        {
            currentTarget = bottomPoint;
        }
        else
        {
            currentTarget = topPoint;
        }

        isWaiting = false;
    }

    // This stops the Miner from sliding off when the elevator moves
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<MinerController>() != null)
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<MinerController>() != null)
        {
            collision.transform.SetParent(null);
        }
    }
}