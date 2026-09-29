using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows health as hearts. Drag your 5 heart Images into the list, left to right.
/// Heart i is full when i < current health. Losing health empties hearts from the right.
/// </summary>
public class HeartsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Heart images, left to right. Their count doesn't need to match max health, extra hearts just show as empty.")]
    [SerializeField] private Image[] hearts;

    [Header("Look")]
    [SerializeField] private Sprite fullHeart;
    [Tooltip("Optional. If left empty, lost hearts are hidden or greyed out instead (see below).")]
    [SerializeField] private Sprite emptyHeart;
    [Tooltip("Used only when there's no empty sprite. On = hide lost hearts. Off = tint them grey.")]
    [SerializeField] private bool hideLostHearts = false;
    [SerializeField] private Color lostTint = new Color(0.25f, 0.25f, 0.25f, 0.6f);

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[HeartsUI] No PlayerHealth found. Assign one in the Inspector.", this);
            return;
        }

        playerHealth.OnHealthChanged += Refresh;
        Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= Refresh;
    }

    private void Refresh(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            bool full = i < current;

            if (emptyHeart != null)
            {
                hearts[i].enabled = true;
                hearts[i].sprite = full ? fullHeart : emptyHeart;
                hearts[i].color = Color.white;
            }
            else if (hideLostHearts)
            {
                hearts[i].enabled = full;
                if (fullHeart != null) hearts[i].sprite = fullHeart;
                hearts[i].color = Color.white;
            }
            else
            {
                hearts[i].enabled = true;
                if (fullHeart != null) hearts[i].sprite = fullHeart;
                hearts[i].color = full ? Color.white : lostTint;
            }
        }
    }
}