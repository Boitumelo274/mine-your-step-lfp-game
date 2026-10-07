using System.Collections;
using UnityEngine;

public class PulsingHeading : MonoBehaviour
{
    [Header("Timing")]
    public float spawnDelay = 2f; // How long to wait before showing

    [Header("Zoom Effect")]
    public float pulseSpeed = 3f; // How fast it zooms in and out
    public float pulseSize = 0.1f; // How much larger/smaller it gets

    private Vector3 originalScale;
    private bool isReady = false;

    private void Start()
    {
        // Remember the original size, then shrink it to 0 so it is invisible
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        StartCoroutine(SpawnDelayRoutine());
    }

    private IEnumerator SpawnDelayRoutine()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(spawnDelay);
        isReady = true;
    }

    private void Update()
    {
        if (isReady)
        {
            // Creates a smooth wave that loops between -1 and 1
            float wave = Mathf.Sin(Time.time * pulseSpeed);

            // Apply the wave to the scale to create the zooming effect
            float offset = wave * pulseSize;
            transform.localScale = originalScale + new Vector3(offset, offset, offset);
        }
    }
}