using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, ICheckpointable
{
    private readonly SortedDictionary<string, int> items = new SortedDictionary<string, int>();

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

    public bool Remove(string itemName, int amount = 1)
    {
        int have = Count(itemName);
        if (amount <= 0 || have < amount) return false;

        if (have == amount) items.Remove(itemName);
        else items[itemName] = have - amount;

        OnChanged?.Invoke();
        return true;
    }

    // --- ICheckpointable ---
    public object CaptureState() => new Dictionary<string, int>(items);

    public void RestoreState(object state)
    {
        items.Clear();
        if (state is Dictionary<string, int> savedItems)
        {
            foreach (var pair in savedItems) items[pair.Key] = pair.Value;
        }
        OnChanged?.Invoke(); // Refresh UI
    }
}