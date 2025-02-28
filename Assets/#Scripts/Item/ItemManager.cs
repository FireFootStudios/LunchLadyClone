using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ItemManager : SingletonBaseNetwork<ItemManager>
{
    [SerializeField] private bool _debugItemPickUps = true;

    private List<ItemN> _registeredItems = new List<ItemN>();
    private List<ItemN> _returnItemsList = new List<ItemN>();


    public static Action<ItemN> OnItemPickedUp;
    public static Action<ItemN> OnItemRegister;

    // Works for all clients
    public static Action<itemType> OnItemPickedUpClient; 
    public static Action<itemType> OnItemRegisterClient; 



    public void RegisterItem(ItemN item)
    {
        if (_registeredItems.Contains(item)) return;

        OnItemRegisteredClientRPC(item.Data.Type);

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

    public List<ItemN> GetItemsByType(itemType type)
    {
        _returnItemsList.Clear();

        _returnItemsList.AddRange(_registeredItems.FindAll(item => item.Data.Type == type));
        return _returnItemsList;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkManager.Singleton.SceneManager.OnLoad += OnNetworkSceneLoad;
    }

    private void OnNetworkSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        if (!IsHost) return;

        _registeredItems.Clear();
    }

    private void OnItemPickUp(ItemN item)
    {
        DebugItemPickedUpClientRpc(item.Data.Type, item.name + " has been picked up!");

        if (!IsHost) return;
        OnItemPickedUp?.Invoke(item);
    }

    [ClientRpc]
    private void DebugItemPickedUpClientRpc(itemType type, string message)
    {
        if (!_debugItemPickUps) return;

        Debug.Log(message);
        OnItemPickedUpClient?.Invoke(type);
    }

    [ClientRpc]
    private void OnItemRegisteredClientRPC(itemType type)
    {
        OnItemRegisterClient?.Invoke(type);
    }
}