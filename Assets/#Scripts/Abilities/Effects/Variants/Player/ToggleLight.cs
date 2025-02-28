using UnityEngine;

public sealed class ToggleLight : Effect
{
    [SerializeField] private PlayerLight _light = null;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_light) return;

        _light.SetLit(!_light.IsLit);
    }

    protected override float Effectiveness(GameObject target)
    {
        if (!target) return 0.0f;

        return 1.0f;
    }

    protected override void Copy(Effect effect)
    {
        // TODO
    }
}