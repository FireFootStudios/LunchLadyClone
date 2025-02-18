using UnityEngine;

public sealed class Modifier : Effect
{
    [SerializeField] private MovementModifier _modifier = new MovementModifier();
    [SerializeField] private bool _stopTarget = false;
    [SerializeField, Tooltip("Prevent use of abilities for x duration?")] private float _abilityDisableTime = 0.0f;
    [SerializeField, Tooltip("Cancel ongoing abilities of target?")] private bool _cancelAbilities = true;
    [SerializeField] private bool _ignoreDead = true;

    private void Start()
    {
        //set source on modifier
        _modifier.Source = Ability.gameObject;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target) return;

        if (_ignoreDead)
        {
            Health health = target.GetComponent<Health>();
            if (!health || health.IsDead) return;
        }

        //create and add modifer to target movement (copy from template)
        CharMovement movement = target.GetComponent<CharMovement>();
        if (movement)
        {
            movement.AddOrUpdateModifier(_modifier);
            if (_stopTarget) movement.RB.linearVelocity = Vector3.zero;
        }
        //set ability disable timer
        AbilityManager abilityManager = target.GetComponent<AbilityManager>();
        if (abilityManager)
        {
            if (_abilityDisableTime > abilityManager.DisableTimer) abilityManager.SetDisableTime(_abilityDisableTime);
            if (_cancelAbilities && abilityManager.CurrentFiring) abilityManager.CurrentFiring.Cancel();
        }
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        //target dead?
        if (_ignoreDead)
        {
            Health health = target.GetComponent<Health>();
            if (!health || health.IsDead) return 0.0f;
        }


        float eff = 1.0f;

        //has a movement comp?
        CharMovement movement = target.GetComponent<CharMovement>();
        AbilityManager abilityManager = target.GetComponent<AbilityManager>();

        if (!movement && !abilityManager) eff = 0.0f;

        return eff;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}