using System.Collections;
using UnityEngine;

public sealed class TimeModifier : Effect
{
    [SerializeField] private float _timeMultiplier = 0.5f;
    [SerializeField] private float _modDuration = 2.0f;
    [SerializeField] private bool _holdAbiliy = false;
    [SerializeField] private bool _resetOnCancel = false;
    [Space]
    [SerializeField] private float _smoothSpeedIn = 0.0f;
    [SerializeField] private float _smoothSpeedOut = 0.0f;

    private TimeScaleManager _timeScaleManager = null;


    public bool InSlowMo { get; private set; }

    public float TimeMultipier { get { return _timeMultiplier; } set { _timeMultiplier = value; } }
    public float Duration { get { return _modDuration; } set { _modDuration = value; } }


    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        Resett();
        StartCoroutine(SlowMoCo());
    }

    public IEnumerator SlowMoCo()
    {
        InSlowMo = true;

        _timeScaleManager.AddTimeScaleMod(_timeMultiplier, this, _modDuration, _smoothSpeedIn, _smoothSpeedOut);
        yield return new WaitForSecondsRealtime(_modDuration);

        Resett();
        yield return null;
    }

    public override bool IsFinished()
    {
        return !_holdAbiliy || !InSlowMo;
    }

    public override void OnCancel()
    {
        if (_resetOnCancel) Resett();
    }

    public override void OnCleanUp()
    {
        Resett();
    }

    protected override void Awake()
    {
        base.Awake();

        _timeScaleManager = TimeScaleManager.Instance;
    }

    private void Resett()
    {
        InSlowMo = false;
        if (_timeScaleManager) _timeScaleManager.RemoveTimeScaleMod(this);

        StopAllCoroutines();
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}