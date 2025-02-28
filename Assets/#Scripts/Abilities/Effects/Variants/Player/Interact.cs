using UnityEngine;

public sealed class Interact : Effect
{
    [SerializeField] private PlayerN _playerN = null;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target.TryGetComponent(out InteractibleObjectN interactible)) return;
        if (_playerN == null || !_playerN.IsSpawned) return;

        interactible.PlayAnimOnInteract();
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        if (!target.TryGetComponent(out InteractibleObjectN item)) return 0.0f;

        return 1.0f;
    }

    protected override void Copy(Effect effect)
    {
        // TODO
    }
}
