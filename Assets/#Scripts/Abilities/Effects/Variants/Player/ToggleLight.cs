using UnityEngine;

public sealed class ToggleLight : Effect
{
    [SerializeField] private PlayerLight _light = null;
    [SerializeField] private TargetInfo _targetInfo = null;
    [SerializeField] private float _losDistMultLightOff = 0.2f;
    [SerializeField] private float _losDistMultLightOn = 1.0f;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_light) return;

        _light.SetLit(!_light.IsLit);

        if (_targetInfo)
        {
            _targetInfo.LosDistanceMult = _light.IsLit ? _losDistMultLightOn : _losDistMultLightOff;
        }
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