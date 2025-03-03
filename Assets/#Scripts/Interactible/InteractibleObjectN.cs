using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InteractibleObjectN : NetworkBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;
     
    private NetworkVariable<bool> _animPlayed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Init...
        _animPlayed.OnValueChanged += OnAnimPlayedChanged;
    }

    private void OnAnimPlayedChanged(bool previousValue, bool newValue)
    {
    }

    public void PlayAnimOnInteract()
    {
        if (IsSpawned) return;

        foreach (Animator animator in _animators)
        {
            if (_firstAnimTrigger != null && !_animPlayed.Value)
            {
                animator.SetTrigger(_firstAnimTrigger);
                if (_secondAnimTrigger != null)
                    _animPlayed.Value = true;
            }
            else if (_secondAnimTrigger != null && _animPlayed.Value)
            {
                animator.SetTrigger(_secondAnimTrigger);
                _animPlayed.Value = false;
            }
        }
    }

    public bool CanInteract()
    {
        if (!IsSpawned || _firstAnimTrigger == null) return false;
        if (_animPlayed.Value && string.IsNullOrEmpty(_secondAnimTrigger)) return false;
        return true;
    }
}
