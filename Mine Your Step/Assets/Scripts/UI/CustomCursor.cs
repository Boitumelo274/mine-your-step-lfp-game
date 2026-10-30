using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public Texture2D targetPointer;
    public Vector2 hotspot = new Vector2(16, 16); // Sets click point to the center of a 32x32 image

    private void Start()
    {
        Cursor.SetCursor(targetPointer, hotspot, CursorMode.Auto);
    }
}