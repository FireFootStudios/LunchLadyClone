using System;
using UnityEngine;
using Unity.Netcode;

public sealed class Jester : BadGuy
{
    [SerializeField] private SoundSpawnData _killPlayerSFX = null;
    [SerializeField] private Ability _killPlayerAbility = null;

    protected override void Awake()
    {
        base.Awake();

        if (_killPlayerAbility) _killPlayerAbility.OnFire += OnKillAbilityFire;
    }

    private void OnKillAbilityFire()
    {
        OnKillAbilityFireClientRpc();
    }

    [ClientRpc]
    private void OnKillAbilityFireClientRpc()
    {
        SoundManager.Instance.PlaySound(_killPlayerSFX);
    }
}