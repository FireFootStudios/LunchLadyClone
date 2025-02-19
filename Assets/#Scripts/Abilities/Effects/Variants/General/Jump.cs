using UnityEngine;

public sealed class Jump : Effect
{
    [Header("General"), SerializeField, Tooltip("Interpreted as local vec")] private Vector3 _jumpForceMin = new Vector3(0.0f, 5.0f, 0.0f);
    [Header("General"), SerializeField, Tooltip("Interpreted as local vec")] private Vector3 _jumpForceMax = new Vector3(0.0f, 5.0f, 0.0f);

    [SerializeField, Tooltip("Time after becoming ungrounded where player can still jump")] private float _coyoteTime = 0.25f;
    [SerializeField] private bool _needsGrounded = true;
    [SerializeField] private bool _resetYVel = true;
    [SerializeField, Tooltip("Movement mod applied to ourselves on jump")] private MovementModifier _moveModOnJump = null;

    //[SerializeField] private Vector2 _verticalScaleVec = Vector2.one;
    //[SerializeField] private Vector2 _horizontalScaleVec = Vector2.one;
    [SerializeField, Tooltip("Speed reference bounds for scaling jump force")] private Vector2 _scaleSpeedRefBounds = Vector2.one;

    private float _timeUngrounded = 0.0f;
    private bool _hasJumpedSinceUnGrounded = false;

    public FreeMovement Movement { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Movement = GetComponentInParent<FreeMovement>();

        _moveModOnJump.Source = this.gameObject;

        if (_needsGrounded)
        {
            Movement.OnStopGrounded += () => _timeUngrounded = 0.0f;
            Movement.OnGrounded += () => _hasJumpedSinceUnGrounded = false;
        }
    }

    private void Update()
    {
        _timeUngrounded += Time.deltaTime;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        //always set true once apply
        _hasJumpedSinceUnGrounded = true;

        Rigidbody rb = Movement.RB;

        //reset y vel?
        if (_resetYVel) rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0.0f, rb.linearVelocity.z);

        //calculater percentage for multiplier of current speed between min/max ref speed
        //float scalePerc = Mathf.InverseLerp(_scaleSpeedRefBounds.x, _scaleSpeedRefBounds.y, Movement.CurrentMoveSpeed);
        //Vector3 force = Vector3.Lerp(_jumpForceMin, _jumpForceMax, scalePerc);

        //calculater percentage for multiplier of current speed between min/max ref speed
        float scalePerc = Mathf.InverseLerp(_scaleSpeedRefBounds.x, _scaleSpeedRefBounds.y, Movement.DesiredMovement.magnitude);
        Vector3 force = Vector3.Lerp(_jumpForceMin, _jumpForceMax, scalePerc);

        //transform force to local space
        force = originT.transform.transform.TransformDirection(force);

        // Add the force
        //rb.AddForce(force, ForceMode.VelocityChange);

        // I am not using the add force method here as there is a small delay before the velocity is actually applied to the vector,
        // this is a problem because we might also be wanting to step in the same frame, which should not be possible above a certain y velocity,
        // this then results in a unwanted boost by having a jump + step force in the same frame(or close), without them knowing about each other
        rb.linearVelocity += force;

        //add move mod to ourselves
        Movement.AddOrUpdateModifier(_moveModOnJump, false);
    }

    public override bool CanApply()
    {
        return (!_needsGrounded ||
            (Movement.IsGrounded || (!_hasJumpedSinceUnGrounded && _timeUngrounded < _coyoteTime)));
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}