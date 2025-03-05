using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public sealed class Door : Interactible
{
    [SerializeField] private List<ItemN> _unlockItems = null;
    [SerializeField] private bool _requireItemsInInventory = true;
    [SerializeField] private HitBox _openHitbox = null;
    [Space]
    [SerializeField] private KinematicMovement _movement = null;
    [SerializeField] private Transform _moveTargetT = null;

    private Transform _startT = null;

    private List<PlayerN> _playersInside = new List<PlayerN>();

    private NetworkVariable<bool> _isUnlocked = new NetworkVariable<bool>();



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _isUnlocked.OnValueChanged += OnOpened;
        _startT = this.transform;

        if (!IsHost) return;

        if (_openHitbox) _openHitbox.OnTargetsChange += OnOpenHBTargetsChange;
    }

    private void OnOpenHBTargetsChange()
    {
        TryUnlock();
    }

    private bool TryUnlock()
    {
        if (!IsSpawned || !IsHost || _isUnlocked.Value) return false;

        _playersInside.Clear();
        foreach (GameObject target in _openHitbox.Targets)
        {
            if (!target.TryGetComponent(out PlayerN player)) continue;
            //if (player.Health.IsDead) continue;

            _playersInside.Add(player);
        }

        // Check for all unlock items if unlocked and owned by players if needed
        foreach (ItemN item in _unlockItems)
        {
            // Ignore null entries
            if (item == null) continue;

            // Return false as soon as an item has not been picked up yet
            if (!item.IsPickedUp) return false;

            // Check if item is owned by any of players, if not we return
            if (_requireItemsInInventory)
            {
                bool itemOwned = _playersInside.Exists(player =>
                {
                    if (!player.Inventory) return false;

                    return player.Inventory.HasItem(item);
                });

                if (!itemOwned) return false;
            }
        }

        _isUnlocked.Value = true;
        return true;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!other.TryGetComponent(out PlayerN playerN)) return;
    //    if (!playerN.IsLocalPlayer) return;
    //    if (playerN.Health.IsDead) return;
    //    if (_isOpened.Value) return;
    //    if (!IsSpawned) return;

    //    TryOpenDoorServerRpc();
    //}

    //[ServerRpc(RequireOwnership = false)]
    //private void TryOpenDoorServerRpc()
    //{
    //    // If already open return
    //    if (_isOpened.Value) return;


    //    foreach(ItemN item in _unlockItems)
    //    {
    //        if (item == null || !item.IsPickedUp) continue;

    //        if (_requireItemsInInventory)
    //        {

    //        }
    //    }


    //    //
    //    if (_requireItemsInInventory && playerN.Inventory)
    //    {
    //        foreach (ItemN item in _unlockItems)
    //            playerN.Inventory.HasItemServerRpc()
    //    }


    //    bool itemsUnlocked = _unlockItems.TrueForAll(item => item.IsPickedUp);
    //    if (_unlockItems.Count > 0 && !itemsUnlocked) return; 

    //    _isOpened.Value = true;
    //}


    private void OnOpened(bool previousValue, bool opened)
    {
        // Only host moves
        if (IsHost && opened && _movement && _moveTargetT)
        {
            _movement.MoveToPos(_moveTargetT.position, false);
        }
    }
}