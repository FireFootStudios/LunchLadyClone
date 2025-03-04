using UnityEngine;

public sealed class Interact : Effect
{
    [SerializeField] private PlayerN _playerN = null;

    private bool _interacting = false;

    protected override async void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target.TryGetComponent(out Interactible interactible)) return;
        if (_playerN == null || !_playerN.IsSpawned) return;

        _interacting = true;
        await interactible.Interact(_playerN);

        _interacting = false;
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        if (!target.TryGetComponent(out Interactible interactible)) return 0.0f;

        if (!interactible.CanInteract()) return 0.0f;

        return 1.0f;
    }

    public override bool CanApply()
    {
        return !_interacting;
    }

    public override bool IsFinished()
    {
        return !_interacting;
    }

    public override void OnCancel()
    {
        _interacting = false;
    }

    protected override void Copy(Effect effect)
    {
        // TODO
    }
}