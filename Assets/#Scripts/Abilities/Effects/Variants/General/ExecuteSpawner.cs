using UnityEngine;

public sealed class ExecuteSpawner : Effect
{
    [SerializeField] private ExecuterData _data = null;
    [SerializeField] private GameObject _template = null;

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
    }
    protected override void Copy(Effect effect)
    {
        //TODO
    }
}