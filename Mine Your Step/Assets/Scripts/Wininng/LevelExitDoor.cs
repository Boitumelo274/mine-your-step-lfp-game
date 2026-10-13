using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class LevelExitDoor : MonoBehaviour
{
    [Header("Win Condition Settings")]
    [Tooltip("Target jewel name in inventory. If left empty, checks TOTAL jewels in inventory.")]
    [SerializeField] private string jewelName = "Jewel";
    [SerializeField] private int extraJewelsRequired = 5; // Exactly 5 extra jewels placed randomly
    [SerializeField] private string nextSceneName = "NextLevel"; // Name of win screen or next level

    [Header("Door Visuals")]
    [SerializeField] private SpriteRenderer doorSpriteRenderer;
    [SerializeField] private Sprite lockedDoorSprite;
    [SerializeField] private Sprite unlockedDoorSprite;

    private int totalRequiredJewels = 0;
    private bool isDoorOpen = false;

    private void Start()
    {
        // Ensure collider is set to Trigger so the player can walk up to it
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (doorSpriteRenderer == null)
        {
            doorSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Calculate total required jewels: (Number of Mines) + (5 Extra Jewels)
        int mineCount = 15; // Fallback default
        if (GridManager.Instance != null)
        {
            mineCount = GridManager.Instance.targetMines;
        }

        totalRequiredJewels = mineCount + extraJewelsRequired;
        Debug.Log($"[LevelExitDoor] Door initialized. Total jewels required: {totalRequiredJewels} ({mineCount} mines + {extraJewelsRequired} extra)");

        // Set initial locked sprite
        if (doorSpriteRenderer != null && lockedDoorSprite != null)
        {
            doorSpriteRenderer.sprite = lockedDoorSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player touched the door
        if (other.CompareTag("Player") || other.GetComponent<MinerController>() != null)
        {
            CheckWinCondition(other.gameObject);
        }
    }

    private void CheckWinCondition(GameObject playerObj)
    {
        if (isDoorOpen)
        {
            ProceedToNextLevel();
            return;
        }

        PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = playerObj.GetComponentInChildren<PlayerInventory>();
        }

        if (inventory != null)
        {
            // Check specific jewel name if specified, otherwise fall back to inventory.Total
            int playerJewels = !string.IsNullOrEmpty(jewelName) ? inventory.Count(jewelName) : inventory.Total;

            Debug.Log($"[LevelExitDoor] Player has {playerJewels} / {totalRequiredJewels} required jewels.");

            if (playerJewels >= totalRequiredJewels)
            {
                OpenDoor();
                ProceedToNextLevel();
            }
            else
            {
                Debug.Log($"[LevelExitDoor] Door is locked! You need {totalRequiredJewels - playerJewels} more jewels.");
            }
        }
        else
        {
            Debug.LogWarning("[LevelExitDoor] PlayerInventory component not found on player!");
        }
    }

    private void OpenDoor()
    {
        isDoorOpen = true;
        Debug.Log("[LevelExitDoor] Door Unlocked!");

        if (doorSpriteRenderer != null && unlockedDoorSprite != null)
        {
            doorSpriteRenderer.sprite = unlockedDoorSprite;
        }
    }

    private void ProceedToNextLevel()
    {
        if (isDoorOpen)
        {
            Debug.Log("[LevelExitDoor] Victory! Loading next scene...");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}