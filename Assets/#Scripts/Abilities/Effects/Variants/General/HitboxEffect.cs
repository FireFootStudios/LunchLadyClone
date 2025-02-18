using System.Collections.Generic;
using UnityEngine;

public enum HitBoxEffectMode { oncePerEnter, interval/*, oncePerEntity*/ }
public sealed class HitboxEffect : TargettingEffect
{
    [SerializeField] private HitBoxEffectMode _mode = 0;
    [SerializeField] private List<HitBox> _hitboxes = new List<HitBox>();
    [SerializeField, Tooltip("Set negative or 0 for infinite duration")] private float _duration = 1.0f;

    [Header("IntervalMode"), SerializeField] private float _interval = 0.2f;
    //[SerializeField] private bool _invokeEveryFrame = false;
    
    public bool IsActive { get { return ActiveTimer > 0.0f; } }
    public float ActiveTimer {  get; private set; }
    public float IntervalElapsed { get; private set; }
    public float Interval { get { return _interval; } }

    protected override void Awake()
    {
        base.Awake();

        foreach (HitBox hitbox in _hitboxes)
        {
            hitbox.OnTargetEnter += OnHitboxTargetAdd;
            hitbox.OnTargetsChange += OnHitboxTargetsChange;

            //if interval mode, update targets on exit too
            //if (_mode == HitBoxEffectMode.interval) hitbox.OnTargetExit += (GameObject go) => Targets.Remove(go);
        }
    }

    private void FixedUpdate()
    {
        ActiveTimer -= Time.deltaTime;

        UpdateInterval();
    }

    private void UpdateInterval()
    {
        if (!IsActive) return;
        if (_mode != HitBoxEffectMode.interval) return;

        if (_interval > 0.0f)
        {
            //update and check elapsed
            IntervalElapsed += Time.deltaTime;
            if (IntervalElapsed < _interval) return;

            //adjust elapsed and invoke for current targets
            IntervalElapsed -= _interval;
        }

        // Add targets again (some bugs cause targets to not be updated correctly os i just put this here)
        Targets.Clear();
        foreach (HitBox hitbox in _hitboxes)
            Targets.AddRange(hitbox.Targets);
        OnTargetsReady?.Invoke();
    }

    public override bool IsFinished()
    {
        return !IsActive;
    }

    public override void OnCancel()
    {
        ActiveTimer = 0.0f;
        IntervalElapsed = 0.0f;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!(_duration > 0.0f)) ActiveTimer = float.MaxValue;
        else ActiveTimer = _duration;

        //invoke for initial targets in hitbox
        if (Targets.Count > 0)
            OnTargetsReady?.Invoke();
    }

    private void OnHitboxTargetAdd(Collider target)
    {
        if (target == null || !IsActive) return;

        switch (_mode)
        {
            case HitBoxEffectMode.oncePerEnter:

                Targets.Clear();
                Targets.Add(target.gameObject);
                OnTargetsReady?.Invoke();

                break;

            case HitBoxEffectMode.interval:

                //Targets.Add(target);

                break;
        }
    }

    private void OnHitboxTargetsChange()
    {
        Targets.Clear();

        // Add targets
        foreach (HitBox hitbox in _hitboxes)
            Targets.AddRange(hitbox.Targets);
    }
    protected override void Copy(Effect effect)
    {
        //TODO
    }
}