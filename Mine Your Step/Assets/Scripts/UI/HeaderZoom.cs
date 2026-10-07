using UnityEngine;
using UnityEngine;

public class UIZoomEffect : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 3f;
    public float minScale = 0.95f; 
    public float maxScale = 1.05f; 

    private Vector3 baseScale;

    private void Start()
    {
        // Store the original size of the UI element when the game starts
        baseScale = transform.localScale;
    }

    private void Update()
    {
        
        float wave = Mathf.Sin(Time.time * zoomSpeed);

        // Convert that -1 to 1 wave into a 0 to 1 value to smoothly blend between our min and max scale
        float currentScale = Mathf.Lerp(minScale, maxScale, (wave + 1f) / 2f);

        // Apply the newly calculated scale evenly to X, Y, and Z
        transform.localScale = baseScale * currentScale;
    }
}