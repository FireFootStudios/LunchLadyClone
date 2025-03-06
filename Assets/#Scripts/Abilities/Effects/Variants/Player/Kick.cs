using System;
using System.Collections;
using UnityEngine;

public sealed class Kick : TargettingEffect
{
    [SerializeField] private PlayerN _player = null;
    [SerializeField, Tooltip("Movement mod applied to ourselves on kicking")] private MovementModifier _moveModOnKick = null;
    [Space]
    [SerializeField] private LayerMask _kickableLayerMask = 0;
    [SerializeField] private bool _hitTriggers = false;
    [SerializeField] private float _applyEffectsDelay = 0.05f;
    [SerializeField, Tooltip("Before delay")] private bool _clearVelOnKickStart = true;

    [Space]
    [SerializeField] private float _range = 2.0f;
    [SerializeField] private float _baseKickForce = 15.0f;
    [SerializeField] private float _multiplierDead = 0.0f;
    [SerializeField] private float _attemptKickBuffer = 0.05f;

    [SerializeField] private bool _ignoreDead = true;
    [SerializeField] private bool _alwaysCast = true;


    public float Range { get { return _range; } }
    public float BaseKickForce { get { return _baseKickForce; } }

    public bool AttemptingKick { get; private set; }
    public bool PerformingKick { get; private set; }

    public RaycastHit LastKickedHit {  get; private set; }



    public Action<RaycastHit, GameObject, float> OnHit; // => hitInfo + target + kickinfo + kickforce
    public Action<bool> OnKick; // => hit?
    public Action OnKickHitOrMiss;
    //public Action OnKickStart; // Frame of execution, did we hit or nah
    public Action OnFail;


    protected override void Awake()
    {
        base.Awake();


        _moveModOnKick.Source = this.gameObject;
    }

    private void FixedUpdate()
    {
        if (!AttemptingKick || PerformingKick) return;

        if (Ability.ElapsedFiring > _attemptKickBuffer)
        {
            AttemptingKick = false;

            // At this point if we didnt hit, the kick missed
            OnKick?.Invoke(false);
            OnKickHitOrMiss?.Invoke();
            OnFail?.Invoke();

            return;
        }

        //Attempt kick as long as we reach here
        AttemptKick();
    }
    
    public float CalculateKickForce(bool isDead)
    {
        float kickForce = _baseKickForce;

        // Kick info multiplier
        //if (targetKickInfo)
        //{
        //    kickForce *= targetKickInfo.Multiplier;
        //    if (isDead) kickForce *= _multiplierDead;
        //}

        return kickForce;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        AttemptingKick = true;

        AttemptKick();
    }

    public bool DoKickCast(out RaycastHit hitInfo, out GameObject hitGo, out bool isDead)
    {
        hitGo = null;
        isDead = false;

        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;

        // Camera pos + forward
        if (_player && _player.PlayerCameras)
        {
            origin = _player.PlayerCameras.transform.position;
            dir = _player.PlayerCameras.transform.forward;
        }

        // Raycast
        if (Physics.Raycast(origin, dir, out hitInfo, Range, _kickableLayerMask, _hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore))
        {
            hitGo = hitInfo.collider.gameObject;

            // Check dead
            isDead = hitGo.TryGetComponent(out Health targetHealth) && targetHealth.IsDead;
            if (_ignoreDead && isDead) return false;

            return true;
        }
        return false;
    }    

    public override bool CanApply()
    {
        if (_alwaysCast) return true;

        return DoKickCast(out _, out _, out _);
    }

    protected override float Effectiveness(GameObject target)
    {
        return DoKickCast(out _, out _, out bool isDead) ? (isDead && _ignoreDead ? 0.0f : 1.0f) : 0.0f;
    }

    public override bool IsFinished()
    {
        return !AttemptingKick && !PerformingKick;
    }

    public override void OnCancel()
    {
        AttemptingKick = false;
        PerformingKick = false;
        StopAllCoroutines();
    }

    private void AttemptKick()
    {
        if (!AttemptingKick && !PerformingKick) return;

        // Add move mod to ourselves
        _player.Movement.AddOrUpdateModifier(_moveModOnKick, false);

        // Clear velocity before 
        if (_clearVelOnKickStart) _player.Movement.RB.linearVelocity = Vector3.zero;

        if (DoKickCast(out RaycastHit hitInfo, out GameObject hitGo, out bool isDead))
        {
            AttemptingKick = false;
            StopAllCoroutines();
            StartCoroutine(ApplyKick(hitInfo, hitGo, isDead));
        }
    }

    private IEnumerator ApplyKick(RaycastHit hitInfo, GameObject hitGo, bool isDead)
    {
        PerformingKick = true;
        OnKickHitOrMiss?.Invoke();

        if (_applyEffectsDelay > 0.0f)
        {
            float delay = _applyEffectsDelay - Ability.ElapsedFiring;
            if (delay > 0.0f) yield return new WaitForSeconds(delay);
        }

        // Add force
        float kickForce = CalculateKickForce(isDead);

        Vector3 forceDir = transform.forward;
        if (_player && _player.PlayerCameras)
            forceDir = _player.PlayerCameras.transform.forward;

        _player.Movement.RB.AddForce(-forceDir * kickForce, ForceMode.VelocityChange);

        // Reset Air time so that we will not lose velocity if we happen to get grounded right after adding the velocity (bec vel will be reduced fully/partially)
        _player.Movement.AirTime = 0.0f;

        // Store last kicked + hitInfo
        LastKickedHit = hitInfo;

        // Add to targets + invoke targetting effect
        // Add initial hit gameobject instead as effects might need it, effects should lookf for a EffectReferrer comp if they want the main object themselves
        Targets.Clear();
        Targets.Add(hitInfo.collider.gameObject);
        OnTargetsReady?.Invoke();

        OnKick?.Invoke(true);
        OnHit?.Invoke(hitInfo, hitGo, kickForce);

        PerformingKick = false;
        yield return null;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}