using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnManager : SingletonBase<SpawnManager>
{
    private List<Spawner> _spawners = new List<Spawner>();

    public static Action OnReset;

    public void Register(Spawner spawner)
    {
        if (_spawners.Contains(spawner)) return;

        _spawners.Add(spawner);
    }

    public void UnRegister(Spawner spawner)
    {
        _spawners.Remove(spawner);
    }

    public static void Reset()
    {
        OnReset?.Invoke();

        Physics.SyncTransforms();
    }
}