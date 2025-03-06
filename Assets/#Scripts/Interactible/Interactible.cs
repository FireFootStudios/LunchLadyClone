using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Interactible : NetworkBehaviour
{
    [SerializeField] private bool _pingPong = false;
    [Space]
    [SerializeField] private SkillCheck _skillCheck = null;
    [Space]
    [SerializeField] private Health _health = null;
    [SerializeField] private bool _interactRequireDeath = true;
    [SerializeField] private bool _interactOnDeath = true;
    [Space]
    [SerializeField] private List<GameObject> _enableOnInteractGos = new List<GameObject>();
    [SerializeField] private List<GameObject> _disableOnInteractGos = new List<GameObject>();
    [Space]
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;
    [Space]
    [SerializeField] private SoundSpawnData _interactSFX = null;
    [SerializeField] private SoundSpawnData _reInteractSFX = null;


    private NetworkVariable<bool> _interacted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _interacted.OnValueChanged += OnInteractedChange;
     
        if (_health) _health.OnDeath += OnDeath;
    }

    private void OnInteractedChange(bool previousValue, bool interacted)
    {
        // Handle animations
        foreach (Animator animator in _animators)
        {
            if (interacted) animator.SetTrigger(_firstAnimTrigger);
            else animator.SetTrigger(_secondAnimTrigger);
        }

        foreach (GameObject go in _enableOnInteractGos)
            go?.gameObject.SetActive(interacted);

        foreach (GameObject go in _disableOnInteractGos)
            go?.gameObject.SetActive(!interacted);

        if (interacted) SoundManager.Instance.PlaySound(_interactSFX);
        else SoundManager.Instance.PlaySound(_reInteractSFX);
    }

    public async Task Interact(PlayerN player)
    {
        if (!IsSpawned) return;
        if (!CanInteract()) return;

        // Start skill check if any
        if (_skillCheck && player)
        {
            // Try start the skill check (return if fail)
            bool skillCheckSucces = await _skillCheck.DoSkillCheck(player);
            if (!skillCheckSucces) return;
        }

        InteractServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRPC()
    {
        if (!IsHost) return;

        // If not interacted yet, interact
        if (!_interacted.Value) _interacted.Value = true;
        else if (_interacted.Value && _pingPong) _interacted.Value = false;
    }

    public bool CanInteract()
    {
        if (!IsSpawned) return false;
        if (_interacted.Value && !_pingPong) return false;
        if (_health && _interactRequireDeath && !_health.IsDead) return false;

        // Skillcheck
        if (_skillCheck && !_skillCheck.CanSkillCheck()) return false;
        return true;
    }

    private void OnDeath()
    {
        if (_interactOnDeath)
        {
            Interact(null);
        }
    }
}