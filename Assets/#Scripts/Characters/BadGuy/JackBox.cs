using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class JackBox : BadGuy
{
    [Space]
    [SerializeField] private Ability _activateAbility = null;
    [SerializeField] private GameObject _activateSpawnTemplate = null;
    [SerializeField] private SoundSpawnData _activateSFX = null;
    [Space]
    [SerializeField] private float _disableDelay = 5.0f;

    public GameObject FireTarget { get; private set; }


    public static Action<JackBox> OnJackBoxActivate;


    protected override void Awake()
    {
        base.Awake();

        if (_activateAbility) _activateAbility.OnFireFinish += OnActivateAbFired;
        if (_activateAbility) _activateAbility.OnBeforeFire += OnBeforeFire;
    }

    private void OnActivateAbFired()
    {
        // Event
        OnJackBoxActivate?.Invoke(this);

        NotifyActiveClientRPC();
    }

    private void OnBeforeFire(List<GameObject> targets)
    {
        if (targets == null || targets.Count == 0) return;

        // Save fire target
        FireTarget = targets[0];
    }

    [ClientRpc]
    public void NotifyActiveClientRPC()
    {
        if (!_activateSpawnTemplate) return;

        // Spawn activate gameobject
        GameObject go = Instantiate(_activateSpawnTemplate, transform.position, transform.rotation);
        go.SetActive(true);

        // Set to cleanup after delay
        Destroy(go, _disableDelay);

        // SFX
        SoundManager.Instance.PlaySound(_activateSFX);

        // Deactivate ourselves
        this.gameObject.SetActive(false);
    }
}