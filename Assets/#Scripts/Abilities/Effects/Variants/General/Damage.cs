using UnityEngine;

public sealed class Damage : Effect
{
    [SerializeField] private float _amount = 1.0f;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target) return;

        Health health = target.GetComponent<Health>();
        if (!health || health.IsDead) return;

        health.Add_Server(-_amount, Ability.Source);
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        Health health = target.GetComponent<Health>();
        if (!health || health.IsDead) return 0.0f;

        return 1.0f;
    }

    protected override void Copy(Effect template)
    {
        Damage dmgTemplate = template as Damage;
        if (!dmgTemplate) return;

        //TODO
        //_amount = dmgTemplate._amount;
    }
}
