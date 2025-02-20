using System;
using Unity.Netcode;
using UnityEngine;

public sealed class ItemN : NetworkBehaviour
{
    [SerializeField] private GameObject _visuals = null;


    private NetworkVariable<bool> _isPickedUp = new NetworkVariable<bool>();


    public Action<ItemN> OnPickedUp;


    public bool IsPickedUp { get { return _isPickedUp.Value; } }


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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerN playerN)) return;
        if (!playerN.IsLocalPlayer) return;
        if (_isPickedUp.Value) return;
        if (!IsSpawned) return;

        PickUpItemServerRpc();
    }

    // Try pick up item through server
    [ServerRpc(RequireOwnership = false)]
    private void PickUpItemServerRpc()
    {
        if (_isPickedUp.Value) return;

        _isPickedUp.Value = true;
        OnPickedUp?.Invoke(this);
    }
}