using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Goes on the miner. Keeps a count of every kind of jewel collected, by name.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    private readonly SortedDictionary<string, int> items = new SortedDictionary<string, int>();

    /// <summary>Fires whenever something is added or removed.</summary>
    public event Action OnChanged;

    public IReadOnlyDictionary<string, int> Items => items;

    public int Total
    {
        get
        {
            int sum = 0;
            foreach (KeyValuePair<string, int> pair in items) sum += pair.Value;
            return sum;
        }
    }

    public int Count(string itemName)
    {
        return items.TryGetValue(itemName, out int n) ? n : 0;
    }

    public void Add(string itemName, int amount = 1)
    {
        if (amount <= 0) return;

        items[itemName] = Count(itemName) + amount;
        OnChanged?.Invoke();
    }

    /// <returns>true if there was enough to remove.</returns>
    public bool Remove(string itemName, int amount = 1)
    {
        int have = Count(itemName);
        if (amount <= 0 || have < amount) return false;

        if (have == amount) items.Remove(itemName);
        else items[itemName] = have - amount;

        OnChanged?.Invoke();
        return true;
    }
}