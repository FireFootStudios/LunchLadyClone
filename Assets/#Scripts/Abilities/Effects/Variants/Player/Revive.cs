using UnityEngine;

public sealed class Revive : Effect
{
    [SerializeField] private float _reviveDuration = 5.0f;


    private float _reviveElapsed = 0.0f;
    private PlayerN _revivingPlayer = null;


    public float ProgressPerc { get { return _reviveElapsed / _reviveDuration; } }


    private void Update()
    {
        UpdateRevive();
    }

    private void UpdateRevive()
    {
        if (!Ability.IsFiring) return;

        // Check if player is still inside targetsystem
        if (!_revivingPlayer || (Ability.TargetSystem && !Ability.TargetSystem.HasSpecificTarget(_revivingPlayer.gameObject)))
        {
            _revivingPlayer = null;
            Ability.Cancel();
            return;
        }

        _reviveElapsed += Time.deltaTime;
        if (_reviveElapsed < _reviveDuration) return;

        // Revive
        _revivingPlayer.Health.ReviveClientServerRpc();
        Debug.Log("Revive Completed");
        
        _revivingPlayer = null;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target.TryGetComponent(out PlayerN player)) return;

        _revivingPlayer = player;
        _reviveElapsed = 0.0f;
    }

    public override void OnCancel()
    {
        _revivingPlayer = null;
    }

    public override bool IsFinished()
    {
        return _revivingPlayer == null;
    }

    public override bool CanApply()
    {
        return _revivingPlayer == null;
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        if (!target.TryGetComponent(out PlayerN player)) return 0.0f;
        
        float eff = player.Health.IsDead ? 1.0f : 0.0f;
        return eff;
    }

    protected override void Copy(Effect effect)
    {
        // TODO

    }
}