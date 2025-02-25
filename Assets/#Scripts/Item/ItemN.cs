using System;
using Unity.Netcode;
using UnityEngine;


public enum itemID { generic, key, paper }
public sealed class ItemN : NetworkBehaviour
{
    [SerializeField] private GameObject _visuals = null;
    [SerializeField] private itemID _itemID = 0;

    private NetworkVariable<bool> _isPickedUp = new NetworkVariable<bool>();


    public Action<ItemN> OnPickedUp;


    public itemID ID { get { return _itemID; } }
    public bool IsPickedUp { get { return _isPickedUp.Value; } }

    
    public PlayerN PickUpSource { get; private set; }
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
        if (!other.TryGetComponent(out PlayerN playerN)) return;
        if (!playerN.IsLocalPlayer) return;
        if (playerN.Health.IsDead) return;
        if (_isPickedUp.Value) return;
        if (!IsSpawned) return;

        PickUpItemServerRpc(playerN.OwnerClientId);
    }

    // Try pick up item through server
    [ServerRpc(RequireOwnership = false)]
    private void PickUpItemServerRpc(ulong clientID)
    {
        if (_isPickedUp.Value) return;

        // Find player who picked up
        PickUpSource = GameManager.Instance.SceneData.Players.Find(p => p.OwnerClientId == clientID);
        if (!PickUpSource) return;

        PickUpPos = PickUpSource.transform.position;

        _isPickedUp.Value = true;
        OnPickedUp?.Invoke(this);
    }
}