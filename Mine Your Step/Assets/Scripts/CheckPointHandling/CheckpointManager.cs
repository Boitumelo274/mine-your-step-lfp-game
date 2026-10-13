using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public event Action OnRestored;

    private readonly List<ICheckpointable> registered = new List<ICheckpointable>();
    private readonly Dictionary<ICheckpointable, object> saved = new Dictionary<ICheckpointable, object>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in all)
        {
            if (mb is ICheckpointable c) registered.Add(c);
        }

        CaptureAll(); // Level start counts as Checkpoint 0
    }

    public void Register(ICheckpointable c)
    {
        if (!registered.Contains(c)) registered.Add(c);
    }

    public void CaptureAll()
    {
        saved.Clear();
        foreach (ICheckpointable c in registered)
        {
            if ((c as UnityEngine.Object) == null) continue;
            saved[c] = c.CaptureState();
        }
    }

    public void RestoreAll()
    {
        foreach (ICheckpointable c in registered)
        {
            if ((c as UnityEngine.Object) == null) continue;
            if (saved.TryGetValue(c, out object state)) c.RestoreState(state);
        }
        OnRestored?.Invoke();
    }
}