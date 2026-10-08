using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows everything in the PlayerInventory as a list, one line per kind of jewel.
/// Put it on a UI object that has a TextMeshPro text, or assign one below.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TMP_Text listText;
    [Tooltip("Shown above the list. Leave empty for none.")]
    //[SerializeField] private string header = "";
    //[Tooltip("{0} = name, {1} = count")]
    [SerializeField] private string lineFormat = "{0}  x{1}";

    private void OnEnable()
    {
        if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
        if (listText == null) listText = GetComponent<TMP_Text>();

        if (inventory == null)
        {
            Debug.LogWarning("[InventoryUI] No PlayerInventory found.", this);
            return;
        }

        inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (listText == null) return;

        StringBuilder sb = new StringBuilder();
        //if (!string.IsNullOrEmpty(header)) sb.AppendLine(header);

        foreach (KeyValuePair<string, int> pair in inventory.Items)
        {
            //sb.AppendLine(string.Format(lineFormat, pair.Key, pair.Value));
            sb.AppendLine(pair.Value.ToString());
        }

        listText.text = sb.ToString();
    }
}