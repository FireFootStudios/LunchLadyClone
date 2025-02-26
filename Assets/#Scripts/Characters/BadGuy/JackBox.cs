using System;
using UnityEngine;

public sealed class JackBox : BadGuy
{
    [Space]
    [SerializeField] private Ability _activateAbility = null;
    [SerializeField] private GameObject _activateSpawnTemplate = null;
    [SerializeField] private SoundSpawnData _activateSFX = null;


    public static Action<JackBox> OnJackBoxActivate;


    protected override void Awake()
    {
        base.Awake();

        if (_activateAbility) _activateAbility.OnFire += OnActivateAbFire;
    }

    private void OnActivateAbFire()
    {
        if (!_activateSpawnTemplate) return;

        // Spawn activate gameobject
        Instantiate(_activateSpawnTemplate);

        // Event
        OnJackBoxActivate?.Invoke(this);

        // SFX
        SoundManager.Instance.PlaySound(_activateSFX);

        // Deactivate ourselves
        this.gameObject.SetActive(false);
    }
}