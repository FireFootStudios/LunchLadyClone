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
    [SerializeField] private List<GameObject> _enableOnInteractGos = new List<GameObject>();
    [SerializeField] private List<GameObject> _disableOnInteractGos = new List<GameObject>();
    [Space]
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;

    private NetworkVariable<bool> _interacted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _interacted.OnValueChanged += OnInteractedChange;
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
    }

    public async Task Interact(PlayerN player)
    {
        if (!IsSpawned) return;
        if (!CanInteract()) return;

        // Start skill check if any
        if (_skillCheck)
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

        // Skillcheck
        if (_skillCheck && !_skillCheck.CanSkillCheck()) return false;
        return true;
    }
}