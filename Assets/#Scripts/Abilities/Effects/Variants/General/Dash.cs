using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class Dash : TargettingEffect/*, ISaveable*/
{
    [Header("General"), SerializeField] private float _distance = 2.0f;
    [SerializeField] private float _duration = 0.5f;
    [Space]
    [SerializeField, Tooltip("Override dash direction (by default forward is used)")] private Transform _directionT = null;
    [SerializeField, Tooltip("Limit to horizontal input")] private bool _ignoreYInput = true;
    [SerializeField] private bool _inputOverridesDashDir = true;

    [SerializeField, Tooltip("Can only use once in air?")] private bool _castOnceAir = true;
    [SerializeField] private bool _disableGravity = true;

    [Space]
    [SerializeField] private MovementModifier _moveModWhileDashing = null;
    
    [Space]
    [SerializeField] private CollEvents _cancelRegainVelOnColl = null;

    [SerializeField, Range(0.0f, 1.0f)] private float _velMultOnUseHorizontal = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float _velMultOnUseVertical = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float _velMultOnEndHorizontal = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float _velMultOnEndVertical = 0.5f;

    [SerializeField] private bool _regainVelAfterUse = false;
    [SerializeField, Tooltip ("Will scale the regained velocity based on its alignment with the initial dash direction (so if dash the opposite way we regain no velocity)")] private bool _scaleRegainVelWithDashDir = false;
    [SerializeField, Tooltip("Align regain vel with dash dir (so prev vel magnitude is used together with dash dir)")] private bool _alignRegainVelWithDashDir = false;
    [SerializeField, Tooltip("Will limit the prev setting to horizontal vel only")] private bool _alignHorizontalOnly = false;
    [SerializeField] private bool _discardDashVelOnEnd = true;
    [SerializeField] private float _discardDashVelAccuracy = 0.9f;

    [Space]
    [SerializeField] private List<TargettingEffect> _resetOnReady = new List<TargettingEffect>();

    //[Header("Targetting"), SerializeField] private ChildEvents _collisionEvents = null;
    //[SerializeField] private Tag _targetTag = Tag.Player;
    //[SerializeField] private bool _adjustDistanceToTarget = true;
    //[SerializeField] private float _adjustDistanceOffset = 1.0f;
    //[SerializeField] private bool _canControlDash = false;
    //[SerializeField] private bool _rotateToTarget = false;

    [Header("Visuals"), SerializeField] private float _fovChangeOnDash = -10.0f;
    [SerializeField] private Camera _cameraFOVChange = null;

    private float _defaultFOV = 0.0f;
    private const string _EndMethodStr = "End";
    private Vector3 _velBeforeDash = Vector3.zero;
    private Vector3 _addedVelocityDash = Vector3.zero;

    //Check if collided during dash
    private bool _collided = false;

    public CharMovement Movement { get; private set; }
    public bool CanCast { get; private set; }
    public bool IsDashing { get; private set; }


    protected override void Awake()
    {
        base.Awake();

        //Init movement
        Movement = GetComponentInParent<CharMovement>();
        Movement.OnGrounded += () => { CanCast = true; };

        //set moveMod source (this ability)
        _moveModWhileDashing.Source = this.gameObject;

        //each time these effects fire, enable dashing
        foreach (TargettingEffect tEffect in _resetOnReady)
        {
            tEffect.OnTargetsReady += () => CanCast = true;
        }

        //(this as ISaveable).InitSaveSync();

        if (_cancelRegainVelOnColl) _cancelRegainVelOnColl.OnEnter += (Collider coll) => { _collided = true; };

        //targetting
        //if (_collisionEvents) _collisionEvents.OnCollisionEnterEvent += OnCollision;
    }

    private void OnDestroy()
    {
        //(this as ISaveable).CleanupSaveSync();
    }

    private void FixedUpdate()
    {
        //if (IsDashing && _canControlDash)
        //{
        //    Movement.RB.velocity = transform.forward * Movement.RB.velocity.magnitude;
        //}
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        //stop coroutines first (might still be running)
        CancelInvoke();
        IsDashing = true;
        _collided = false;

        //Calculate dash direction
        Vector3 dir = _directionT ? _directionT.forward : transform.forward;
        if (_inputOverridesDashDir && !Utils.AreVectorsEqual(Movement.DesiredMovement, Vector3.zero)) dir = Movement.DesiredMovement;
        if (_ignoreYInput) dir.y = 0.0f;
        dir.Normalize();

        //if (target && _rotateToTarget)
        //{
        //    dir = (target.transform.position - transform.position).normalized;
        //    Movement.transform.forward = dir;
        //}

        //disable use of dash if in air and enabled
        if (_castOnceAir && !Movement.IsGrounded) CanCast = false;

        //disable gravity
        if (_disableGravity) Movement.DisableGravity = true;

        //VELOCITYYYY
        ProcessVelOnUse(dir);

        //calculate force from distance, duration and direction
        float distance = _distance;
        //if (_adjustDistanceToTarget && target)
        //{
        //    distance = (target.transform.position - transform.position).magnitude;
        //    distance += _adjustDistanceOffset;
        //}
        Vector3 force = dir * (distance / _duration);

        //add force
        Movement.RB.AddForce(force, ForceMode.VelocityChange);
        _addedVelocityDash = force;

        //fov change
        //if (_cameraFOVChange)
        //{
        //    DOTween.Kill(_cameraFOVChange);
        //    float halfDur = _duration / 2.0f;
        //    _cameraFOVChange.DOFieldOfView(_defaultFOV + _fovChangeOnDash, halfDur);
        //    _cameraFOVChange.DOFieldOfView(_defaultFOV, halfDur).SetDelay(halfDur);
        //}

        //add move mod
        Movement.AddOrUpdateModifier(_moveModWhileDashing);

        //start end coroutine
        Invoke(_EndMethodStr, _duration);
    }

    //private void OnCollision(Collision collision)
    //{
    //    if (!IsDashing) return;

    //    if (!collision.gameObject.CompareTag(TagManager.Instance.GetTagValue(_targetTag))) return;

    //    Targets.Clear();
    //    Targets.Add(collision.gameObject);
    //    OnTargetsReady?.Invoke();

        
    //    CancelInvoke();
    //    End();
    //}

    public override bool CanApply()
    {
        return CanCast && !IsDashing;
    }

    private void End()
    {
        IsDashing = false;

        //reset disable gravity
        if (_disableGravity) Movement.DisableGravity = false;

        //Reset velocity to prev state
        if (_regainVelAfterUse/* && !_collided*/) Movement.RB.linearVelocity = _velBeforeDash;


        //Discord the added dash force, this is tricky cuz the player might have already lost it due to the physics system or otther variables like a new force added to them
        //if (_discardDashVelOnEnd)
        //{
        //    float currentVelMag = Movement.RB.velocity.magnitude;
        //    float beforeDashVelMag = _velBeforeDash.magnitude;

        //    //If current velocity is bigger than vel before dash
        //    if (currentVelMag > beforeDashVelMag)
        //    {
        //        //Clamp velocity after dash to magnitude of 
        //        float newMag = Mathf.Clamp(currentVelMag - _addedVelocityDash.magnitude, 0.0f, float.MaxValue);
        //        Movement.RB.velocity = Vector3.ClampMagnitude(Movement.RB.velocity, newMag);
        //    }


        //    ////Check alignment current velocity and added dash velocity
        //    //float alignment = (Vector3.Dot(_addedVelocityDash.normalized, Movement.RB.velocity.normalized) + 1.0f) / 2.0f; // [0, 1]
        //    //if (_collided || alignment > _discardDashVelAccuracy)
        //    //{
        //    //    //Scale added on current velocity so we do not remove more than we have
        //    //    //Movement.RB.velocity -= Vector3.ClampMagnitude(_addedVelocityDash, Movement.RB.velocity.magnitude);

        //    //    //Clamp velocity after dash to magnitude of 
        //    //    float newMag = Mathf.Clamp(Movement.RB.velocity.magnitude - _addedVelocityDash.magnitude, 0.0f, float.MaxValue);
        //    //    Movement.RB.velocity = Vector3.ClampMagnitude(Movement.RB.velocity, newMag);
        //    //}
        //}

        //Scale vel
        Vector3 scaledVel = Utils.ScaledVector2Axis(Movement.RB.linearVelocity, _velMultOnEndHorizontal, _velMultOnEndVertical);
        Movement.RB.linearVelocity = scaledVel;

        //remove mod
        //Movement.RemoveMod(_moveModWhileDashing.Source);

        //if (_cameraFOVChange) _cameraFOVChange.fieldOfView = _defaultFOV;
    }

    public override void OnCancel()
    {
        CancelInvoke();
        End();
    }

    public override bool IsFinished()
    {
        return !IsDashing;
    }

    private void ProcessVelOnUse(Vector3 dashDir)
    {
        _velBeforeDash = Movement.RB.linearVelocity;

        //Scale after caching prev vel
        Vector3 scaledVel = Utils.ScaledVector2Axis(Movement.RB.linearVelocity, _velMultOnUseHorizontal, _velMultOnUseVertical);
        Movement.RB.linearVelocity = scaledVel;

        if (!_regainVelAfterUse) return;

        //align with dash dir?
        if (_alignRegainVelWithDashDir)
        {
            Vector3 vel = _velBeforeDash;

            //Dont include y?
            if (_alignHorizontalOnly) vel.y = 0.0f;

            _velBeforeDash = dashDir * vel.magnitude;

            //Keep y
            if (_alignHorizontalOnly) _velBeforeDash.y = Movement.RB.linearVelocity.y;
        }
        //else scale with alignment of current vel and dash dir
        else if (_scaleRegainVelWithDashDir)
        {
            float alignment = (Vector3.Dot(_velBeforeDash.normalized, dashDir.normalized) + 1.0f) / 2.0f; // [0, 1]
            _velBeforeDash *= alignment;
        }
    }

    //void ISaveable.OnSettingsChanged(SettingsData settingsData)
    //{
    //    DOTween.Kill(_cameraFOVChange);

    //    //cache default FOV
    //    _defaultFOV = settingsData.fieldOfView;
    //}

    public override void OnCleanUp()
    {
        CanCast = true;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}