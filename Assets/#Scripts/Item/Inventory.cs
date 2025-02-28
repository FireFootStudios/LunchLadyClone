using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class Inventory : NetworkBehaviour
{
    private List<ItemN> _items = new List<ItemN>(); // Host only

    // List that will have an item data for each item owned
    private List<ItemData> _clientItems = new List<ItemData>();


    
    public List<ItemData> ClientItems { get { return _clientItems; } }
    
    
    public Action<ItemData> OnItemAddClient;






    // Host only
    public bool HasItem(ItemN item)
    {
        if (!item) return false;
        if (!IsHost) return false;

        return _items.Contains(item);
    }

    // Host only
    public bool TryAddItem(ItemN item)
    {
        if (!IsHost) return false;
        if (!item) return false;
        if (_items.Contains(item)) return false;


        _items.Add(item);
        OnItemAddedClientRpc(item.Data);

        return true;
    }

    // Called when an item is added to a clients inventory
    [ClientRpc]
    private void OnItemAddedClientRpc(ItemData itemData)
    {
        _clientItems.Add(itemData);
        OnItemAddClient?.Invoke(itemData);
    }

    //[ServerRpc]
    //public bool HasItemServerRpc(ulong networkID)
    //{
    //    ItemN item = _items.Find(item => item.NetworkObjectId == networkID);
    //    if (!item) return false;

    //    return true;
    //}


}