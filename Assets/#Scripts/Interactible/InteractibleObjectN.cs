using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InteractibleObjectN : NetworkBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;
     
    private NetworkVariable<bool> _interacted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _interacted.OnValueChanged += OnAnimPlayedChanged;
    }

    private void OnAnimPlayedChanged(bool previousValue, bool newValue)
    {
        foreach (Animator animator in _animators)
        {
            if (newValue) animator.SetTrigger(_firstAnimTrigger);
            else animator.SetTrigger(_secondAnimTrigger);
        }
    }

    public void PlayAnimOnInteract()
    {
        if (!IsSpawned) return;

        if (!string.IsNullOrEmpty(_firstAnimTrigger) && !_interacted.Value)
        {
            _interacted.Value = true;
        }
        else if (!string.IsNullOrEmpty(_secondAnimTrigger) && _interacted.Value)
        {
            _interacted.Value = false;
        }
    }

    public bool CanInteract()
    {
        if (!IsSpawned || _firstAnimTrigger == null) return false;
        if (_interacted.Value && string.IsNullOrEmpty(_secondAnimTrigger)) return false;
        return true;
    }
}
