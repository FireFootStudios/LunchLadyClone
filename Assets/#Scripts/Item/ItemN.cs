using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public enum itemType { generic, key, paper }
public sealed class ItemN : NetworkBehaviour
{
    [SerializeField] private GameObject _visuals = null;
    [SerializeField] private ItemData _data = null;
    //[SerializeField] private string _uniqueID = null;
    [SerializeField] private bool _pickupOnTrigger = false;
    [SerializeField, Tooltip("If picked up, is this item owned by just 1 inventory?")] private bool _uniquelyOwned = false;

    private NetworkVariable<bool> _isPickedUp = new NetworkVariable<bool>();


    public Action<ItemN> OnPickedUp;


    public ItemData Data { get { return _data; } }
    public bool IsPickedUp { get { return _isPickedUp.Value; } }

    
    public Inventory Inventory { get; private set; } // Optional inventory we belong to, only server reads this!
    public PlayerN PickUpSource { get; private set; } // Player who picked us up, only server reads this
    public Vector3 PickUpPos { get; private set; }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _isPickedUp.OnValueChanged += OnPickedUpChange;

        // Tell the item manager we exits
        ItemManager.Instance.RegisterItem(this);
    }

    private void OnPickedUpChange(bool previousValue, bool pickedUp)
    {
        if (_visuals) _visuals.SetActive(!pickedUp);

        //if (!previousValue && pickedUp)
        //    OnPickedUp?.Invoke(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_pickupOnTrigger) return;
        if (!other.TryGetComponent(out PlayerN playerN)) return;
        if (!playerN.IsLocalPlayer) return;
        if (playerN.Health.IsDead) return;
        if (_isPickedUp.Value) return;
        if (!IsSpawned) return;

        PickUpItemServerRpc(playerN.OwnerClientId);
    }

    // Try pick up item through server
    [ServerRpc(RequireOwnership = false)]
    public void PickUpItemServerRpc(ulong clientID)
    {
        if (_isPickedUp.Value) return;
        if (Inventory) return;

        // Find player who picked up
        PickUpSource = GameManager.Instance.SceneData.Players.Find(p => p.OwnerClientId == clientID);
        if (!PickUpSource) return;

        // Should be added to someones inventory
        if (_uniquelyOwned && PickUpSource.Inventory)
        {
            PickUpSource.Inventory.TryAddItem(this);
            Inventory = PickUpSource.Inventory;
        }

        PickUpPos = PickUpSource.transform.position;

        _isPickedUp.Value = true;
        OnPickedUp?.Invoke(this);
    }
}


//[System.Serializable]
//public sealed class ItemData : INetworkSerializable
//{
//    [SerializeField] private itemType _itemType = 0;
//    [SerializeField] private string _itemName = default;

//    public itemType Type { get { return _itemType; } }
//    public string ItemName { get { return _itemName.ToString(); } }

//    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//    {
//        // For enums, you can serialize them directly or cast to int if needed.
//        serializer.SerializeValue(ref _itemType);
//        serializer.SerializeValue(ref _itemName);
//    }
//}

[System.Serializable]
public sealed class ItemData : INetworkSerializable
{
    [SerializeField]
    private itemType _itemType = 0;

    // Store as a normal string for inspector convenience.
    [SerializeField]
    private string _itemName = "default";

    public itemType Type { get { return _itemType; } }
    public string Name { get { return _itemName.ToString(); } }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _itemType);

        // Use a temporary FixedString64Bytes for network transmission.
        FixedString64Bytes temp = new FixedString64Bytes(_itemName);
        serializer.SerializeValue(ref temp);
        if (serializer.IsReader)
        {
            _itemName = temp.ToString();
        }
    }
}