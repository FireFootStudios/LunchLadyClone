using UnityEngine;

public enum KinematicMoveMode { snappy, acceleration, smoothDamp }
public sealed class KinematicMovement : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private KinematicMoveMode _mode = KinematicMoveMode.snappy;
    [SerializeField] private float _acceleration = 40.0f;
    [SerializeField] private float _deceleration = 60.0f;
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private bool _clearRemainingvelOnTargetPosReach = true;


    [Header("Rotation"), SerializeField] private bool _canRotate = false;
    [SerializeField] private float _maxRotationSpeed = 360;
    [SerializeField] private float _rotationAcceleration = 720.0f;
    [SerializeField] private float _minAngleForRotate = 1.0f;
    [SerializeField, Tooltip("If true, rotation will be synced based on the desired movement")] private bool _syncRotations = true;

    [Space, SerializeField] private bool _allowVerticalRot = true;
    [SerializeField] private bool _allowHorizontalRot = true;
    [SerializeField] private Transform _overrideRotateT = null;
    //[SerializeField, Tooltip("Set this to limit the horizontal rotation to a certain object only")] private Transform _horizontalRotT = null;
    //[SerializeField, Tooltip("Set this to limit the vertical rotation to  a certain object only ")] private Transform _verticalRotT = null;

    private bool _isStopped = true;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _targetPos = Vector3.zero;
    private Vector3 _desiredForward = Vector3.zero;

    //Used to sync rotations with positional changes
    private Vector3 _prevTargetPos = Vector3.zero;
    private float _originalTargetDistance = 0.0f;

    private float _remainingTargetDistance = 0.0f;

    private Quaternion _originalRot = Quaternion.identity;
    public float MaxSpeed { get { return _maxSpeed; } set { _maxSpeed = value; } }
    public Vector3 DesiredMovement { get; private set; }
    public Vector3 AdjustedDesiredMovement { get; private set; }
    public Vector3 DesiredForward
    {
        get { return _desiredForward; }
        set
        {
            _desiredForward = value;
            _originalRot = RotateT.rotation;
        }
    }

    public KinematicMoveMode Mode { get { return _mode; } }
    public Vector3 CurrentVelocity { get { return _velocity; } private set { _velocity = value; } }
    public float CurrentRotationSpeed { get; private set; }

    public Transform OverrideRotateT { get { return _overrideRotateT; } }
    public Transform RotateT { get { return _overrideRotateT ? _overrideRotateT : transform; } }

    public bool IsStopped { get { return _isStopped; } set { _isStopped = value; } }
    public bool CanRotate { get { return _canRotate; } set { _canRotate = value; } }


    public void Stop(bool instant = true)
    {
        DesiredMovement = Vector3.zero;
        DesiredForward = Vector3.zero;
        _targetPos = transform.position;
        if (instant) CurrentVelocity = Vector3.zero;
        IsStopped = true;

        _originalTargetDistance = 0.0f;
        _remainingTargetDistance = 0.0f;
    }

    public void SetPosAndRot(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        RotateT.rotation = rotation;

        _targetPos = position;
        _prevTargetPos = position;
        _originalTargetDistance = 0.0f;
        _remainingTargetDistance = 0.0f;
    }

    public void MoveToPos(Vector3 pos, bool canRotate = true, bool setRotationToMovement = true)
    {
        _prevTargetPos = transform.position;
        _targetPos = pos;

        DesiredMovement = _targetPos - _prevTargetPos;
        _originalTargetDistance = DesiredMovement.magnitude;
        _remainingTargetDistance = _originalTargetDistance;


        if (setRotationToMovement)
        {
            DesiredForward = DesiredMovement;
            _originalRot = RotateT.rotation;
        }

        IsStopped = false;
        CanRotate = canRotate;
    }

    public void MoveToPosDelta(Vector3 delta, bool canRotate = true, bool setRotationToMovement = true)
    {
        if (Utils.AreVectorsEqual(delta, Vector3.zero)) return;

        MoveToPos(transform.position + delta, canRotate, setRotationToMovement);
    }

    private void FixedUpdate()
    {
        AdjustDesired();
        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();
    }

    private void AdjustDesired()
    {
        AdjustedDesiredMovement = DesiredMovement.normalized;

        //if (Vector3.Angle(desired, transform.forward) > _minAngleForceRotateOnly) desired = Vector3.zero;
    }

    private void UpdateVelocity()
    {
        //No need to do any velocity calculations
        if (_mode == KinematicMoveMode.smoothDamp) return;

        //snappy movement (no accel/decel)
        if (_mode == KinematicMoveMode.snappy)
        {
            CurrentVelocity = AdjustedDesiredMovement * MaxSpeed;
            return;
        }

        //decelerate
        if (IsStopped || AdjustedDesiredMovement == Vector3.zero)
        {
            //calculate new speed (deceleration applied)
            float newSpeed = Mathf.Clamp(CurrentVelocity.magnitude - _deceleration * Time.deltaTime, 0.0f, float.MaxValue);

            //set new velocity
            CurrentVelocity = CurrentVelocity.normalized * newSpeed;
        }
        //acelerate
        else
        {
            //just add acceleration
            CurrentVelocity += _acceleration * AdjustedDesiredMovement * Time.deltaTime;
        }

        //clamp magnitude to max speed
        CurrentVelocity = Vector3.ClampMagnitude(CurrentVelocity, MaxSpeed);
    }

    private void UpdatePosition()
    {
        if (IsStopped && Utils.AreVectorsEqual(CurrentVelocity, Vector3.zero)) return;

        //adjust position
        //_rb.MovePosition(_rb.transform.position + CurrentVelocity * Time.deltaTime); //DOESNT WORK WHEN PLAYER IS PARENTED TO THIS OBJECT FFS

        if (_mode == KinematicMoveMode.smoothDamp && !_isStopped)
        {
            transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _velocity, _smoothTime, MaxSpeed);
        }
        else
        {
            Vector3 change = CurrentVelocity * Time.deltaTime;

            //Have we reached end (prevent overshooting)
            float changeSqrMag = change.magnitude;
            if (changeSqrMag < _remainingTargetDistance)
            {
                transform.position += change;
                _remainingTargetDistance -= changeSqrMag;
            }
            else if (!IsStopped)
            {
                transform.position = _targetPos;
                Stop(_clearRemainingvelOnTargetPosReach);
            }
        }
    }

    private void UpdateRotation()
    {
        if (!CanRotate || DesiredForward == Vector3.zero) return;

        Vector3 desiredDir = DesiredForward;

        //ignore y if cant rotate around X or cannot fly
        if (!_allowVerticalRot) desiredDir.y = 0.0f;
        if (!_allowHorizontalRot) desiredDir.x = desiredDir.z = 0.0f;

        //could be zero vector
        if (desiredDir == Vector3.zero) return;

        //calculate desired angle to check if rotate partially
        if (Vector3.Angle(RotateT.forward, desiredDir) < _minAngleForRotate)
        {
            RotateT.forward = desiredDir;
            CurrentRotationSpeed = 0.0f;
        }
        else
        {
            desiredDir.Normalize();

            //desired rot
            Quaternion desiredRotation = Quaternion.LookRotation(desiredDir.normalized, Vector3.up);

            if (_syncRotations && _originalTargetDistance > 0.0f)
            {
                //Set rot speed?

                //Current distance to target pos
                float distance = (transform.position - _targetPos).magnitude;

                //How far have we moved towards target pos
                float movePerc = 1.0f - (distance / _originalTargetDistance);

                //Slerp rotation
                RotateT.rotation = Quaternion.Slerp(_originalRot, desiredRotation, movePerc);
            }
            else
            {
                //update rotation speed with acceleration 
                CurrentRotationSpeed = Mathf.Clamp(CurrentRotationSpeed + _rotationAcceleration * Time.deltaTime, 0.0f, _maxRotationSpeed);
                RotateT.rotation = Quaternion.RotateTowards(RotateT.rotation, desiredRotation, CurrentRotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}