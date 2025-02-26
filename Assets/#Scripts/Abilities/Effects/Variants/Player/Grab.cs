using UnityEngine;

public sealed class Grab : Effect
{
    [SerializeField] private PlayerN _playerN = null;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target.TryGetComponent(out ItemN item)) return;
        if (_playerN == null || !_playerN.IsSpawned) return;

        item.PickUpItemServerRpc(_playerN.OwnerClientId);
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        if (!target.TryGetComponent(out ItemN item) || item.IsPickedUp) return 0.0f;

        return 1.0f;
    }

    protected override void Copy(Effect effect)
    {
        // TODO
    }
}