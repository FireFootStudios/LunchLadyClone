using UnityEngine;

public sealed class Damage : Effect
{
    [SerializeField] private float _amount = 1.0f;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target) return;

        Health health = target.GetComponent<Health>();
        if (!health || health.IsDead) return;
        if (!health.IsSpawned) return;

        health.Add(-_amount, Ability.Source);

        if (health.IsOwner) health.Add(-_amount, Ability.Source);
        else health.AddClientRpc(-_amount);
        //else if(health.IsServer) health.add 
        //    health.AddServerRpc(health.OwnerClientId, -_amount);
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        Health health = target.GetComponent<Health>();
        if (!health || health.IsDead) return 0.0f;
        if (!health.IsSpawned) return 0.0f;


        return 1.0f;
    }

    protected override void Copy(Effect template)
    {
        Damage dmgTemplate = template as Damage;
        if (!dmgTemplate) return;

        // TODO
        //_amount = dmgTemplate._amount;
    }
}
