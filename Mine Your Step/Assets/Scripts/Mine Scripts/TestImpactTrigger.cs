using UnityEngine;

public class TestImpactTrigger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Collect ALL 2D colliders under the cursor
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            if (hits.Length == 0)
            {
                Debug.LogWarning("Clicked on empty space — no 2D Collider was hit at all.");
                return;
            }

            foreach (RaycastHit2D hit in hits)
            {
                Debug.Log($"Raycast passed through object: {hit.transform.name}");

                TileVisualizer tile = hit.collider.GetComponent<TileVisualizer>();
                if (tile != null)
                {
                    Vector2 tileWorldPos = hit.transform.position;
                    GridManager.OnPlatformStruck?.Invoke(tileWorldPos);

                    Debug.Log($"SUCCESS: Hit Platform '{hit.transform.name}' at World Pos: {tileWorldPos}");
                    return;
                }
            }

            Debug.LogWarning("Clicked an object, but NONE of the hit objects had a TileVisualizer component!");
        }
    }
}