using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class ItemManager : SingletonBaseNetwork<ItemManager>
{
    [SerializeField] private bool _debugItemPickUps = true;

    private List<ItemN> _registeredItems = new List<ItemN>();
    private List<ItemN> _returnItemsList = new List<ItemN>();


    public static Action<ItemN> OnItemPickedUp;
    public static Action<ItemN> OnItemRegister;

    // Works for all clients
    public static Action<itemID> OnItemPickedUpClient; 
    public static Action<itemID> OnItemRegisterClient; 



    public void RegisterItem(ItemN item)
    {
        if (_registeredItems.Contains(item)) return;


        OnItemRegisteredClientRPC(item.ID);

        if (!IsHost) return;

        item.OnPickedUp += OnItemPickUp;
        _registeredItems.Add(item);
        OnItemRegister?.Invoke(item);
    }

    public bool IsItemPickedUp(ItemN item)
    {
        if (!_registeredItems.Contains(item)) return false;

        return item.IsPickedUp;
    }

    public List<ItemN> GetItemsByType(itemID id)
    {
        _returnItemsList.Clear();

        _returnItemsList.AddRange(_registeredItems.FindAll(item => item.ID == id));
        return _returnItemsList;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void OnItemPickUp(ItemN item)
    {
        DebugItemPickedUpClientRpc(item.ID, item.name + " has been picked up!");

        if (!IsHost) return;
        OnItemPickedUp?.Invoke(item);
    }

    [ClientRpc]
    private void DebugItemPickedUpClientRpc(itemID id, string message)
    {
        if (!_debugItemPickUps) return;

        Debug.Log(message);
        OnItemPickedUpClient?.Invoke(id);
    }

    [ClientRpc]
    private void OnItemRegisteredClientRPC(itemID id)
    {
        OnItemRegisterClient?.Invoke(id);
    }
}