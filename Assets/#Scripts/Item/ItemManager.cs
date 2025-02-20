using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class ItemManager : SingletonBaseNetwork<ItemManager>
{
    [SerializeField] private bool _debugItemPickUps = true;

    private List<ItemN> _registeredItems = new List<ItemN>();



    public static Action<ItemN> OnItemPickedUp;



    public void RegisterItem(ItemN item)
    {
        if (_registeredItems.Contains(item)) return;
        if (!IsHost) return;

        item.OnPickedUp += OnItemPickUp;
        _registeredItems.Add(item);
    }

    public bool IsItemPickedUp(ItemN item)
    {
        if (!_registeredItems.Contains(item)) return false;

        return item.IsPickedUp;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void OnItemPickUp(ItemN item)
    {
        DebugItemPickedUpClientRpc(item.name + " has been picked up!");
        OnItemPickedUp?.Invoke(item);
    }

    [ClientRpc]
    private void DebugItemPickedUpClientRpc(string message)
    {
        if (!_debugItemPickUps) return;

        Debug.Log(message);
    }
}