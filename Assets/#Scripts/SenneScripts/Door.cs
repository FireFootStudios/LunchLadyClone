using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public sealed class Door : NetworkBehaviour
{
    [SerializeField] private KinematicMovement _movement = null;
    [SerializeField] private List<ItemN> _unlockItems = null;
    [Space]
    [SerializeField] private Transform _moveTargetT = null;

    private NetworkVariable<bool> _isOpened = new NetworkVariable<bool>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _isOpened.OnValueChanged += OnOpened;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerN playerN)) return;
        if (!playerN.IsLocalPlayer) return;
        if (_isOpened.Value) return;
        if (!IsSpawned) return;

        TryOpenDoorServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryOpenDoorServerRpc()
    {
        // If already open return
        if (_isOpened.Value) return;

        bool itemsUnlocked = _unlockItems.TrueForAll(item => item.IsPickedUp);
        if (_unlockItems.Count > 0 && !itemsUnlocked) return; 

        _isOpened.Value = true;
    }


    private void OnOpened(bool previousValue, bool opened)
    {
        // Only host moves
        if (IsHost && opened && _movement && _moveTargetT)
        {
            _movement.MoveToPos(_moveTargetT.position, false);
        }
    }
}