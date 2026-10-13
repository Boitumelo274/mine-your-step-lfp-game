using UnityEngine;
using TMPro;

public class DisarmerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI disarmerText;
    [SerializeField] private DisarmerThrower disarmerThrower;

    private int lastAmmo = -1;

    private void Start()
    {
        // Auto-find TextMeshPro component if attached to the same GameObject
        if (disarmerText == null)
        {
            disarmerText = GetComponent<TextMeshProUGUI>();
        }

        // Auto-find DisarmerThrower on the Player if not assigned
        if (disarmerThrower == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                disarmerThrower = player.GetComponent<DisarmerThrower>();
            }
        }
    }

    private void Update()
    {
        if (disarmerThrower != null && disarmerText != null)
        {
            int current = disarmerThrower.CurrentDisarmers;
            int max = disarmerThrower.MaxDisarmers;

            // Update text only when ammo changes to save performance
            if (current != lastAmmo)
            {
                lastAmmo = current;
                disarmerText.text = $"{current}";
                // Change to $"{current}" if you just want to show the number of shots left
            }
        }
    }
}