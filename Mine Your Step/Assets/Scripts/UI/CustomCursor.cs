using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    public Texture2D targetCursor;
    public Vector2 hotspot = new Vector2(16f, 16f);

    private void OnEnable()
    {
        // Apply the custom target cursor when entering MainScene
        Cursor.SetCursor(targetCursor, hotspot, CursorMode.Auto);
    }

    private void OnDisable()
    {
        // Reset back to the normal OS arrow when leaving MainScene
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}