using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AbilityManager : MonoBehaviour
{
    [SerializeField] private List<Ability> _abilities = new List<Ability>();
    [SerializeField] private float _inputBuffer = 0.2f;

    public Action<Ability> OnFire;

    public List<Ability> Abilities { get { return _abilities; } }
    public Ability LastUsed { get; private set; }
    public Ability CurrentFiring { get; private set; }
    public float GeneralAbilityCDTimer { get; private set; }

    public float DisableTimer { get; private set; }
    public float DisableTimeOriginal { get; private set; }

    public void Resett()
    {
        foreach (Ability ability in _abilities)
        {
            ability.Cancel();
            ability.Resett();
        }
        if (CurrentFiring) CurrentFiring.Cancel();

        DisableTimer = 0.0f;
        DisableTimeOriginal = 0.0f;
        GeneralAbilityCDTimer = 0.0f;

        LastUsed = null;
        CurrentFiring = null;
    }

    public void Cancel()
    {
        if (CurrentFiring)
        {
            CurrentFiring.Cancel();
            CurrentFiring = null;
        }
    }

    public void SetDisableTime(float duration)
    {
        DisableTimer = DisableTimeOriginal = duration;
    }

    public bool CanUseAbility(Ability ability, bool checkRotation = true)
    {
        return ability && DisableTimer < 0.001f && ability.CanFire(GeneralAbilityCDTimer, checkRotation);
    }

    public bool TryUseAbility(Ability ability)
    {
        StopAllCoroutines();
        return TryUse(ability);
    }

    public void TryUseAbilityInputBuffer(Ability ability)
    {
        if (!ability) return;

        StopAllCoroutines();
        StartCoroutine(TryUseAbilityCo(ability));
    }

    //attempts to use an ability with input buffer
    private IEnumerator TryUseAbilityCo(Ability ability)
    {
        float elapsed = 0.0f;
        while (!CanUseAbility(ability) && elapsed < _inputBuffer)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        TryUse(ability);

        yield return null;
    }

    private bool TryUse(Ability ability)
    {
        if (ability && DisableTimer < 0.001f && ability.TryFire(GeneralAbilityCDTimer))
        {
            if (GeneralAbilityCDTimer > 0.0f) GeneralAbilityCDTimer += ability.GeneralCooldown;
            else GeneralAbilityCDTimer = ability.GeneralCooldown;

            LastUsed = ability;
            CurrentFiring = ability;

            OnFire?.Invoke(ability);
            return true;
        }

        return false;
    }

    private void Update()
    {
        //update general ability cooldown timer
        GeneralAbilityCDTimer -= Time.deltaTime;

        //update disable timer
        DisableTimer -= Time.deltaTime;

        if (CurrentFiring && !CurrentFiring.IsFiring) CurrentFiring = null;
    }
}