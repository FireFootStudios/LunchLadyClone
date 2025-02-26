using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class NavMeshMovement : MonoBehaviour
{
    #region Fields
    [Header("General")]
    [SerializeField] private float _baseMaxSpeed = 5;
    [SerializeField] private float _baseAcceleration = 100;


    [Space]
    //[SerializeField, Tooltip("Stop adding gravity force when y velocity below this value")] private float _maxYVelForGravity = -30.0f;
    [SerializeField] private float _maxLinearVel = 100.0f;

    [Header("Rotations"), SerializeField] private float _maxRotationSpeed = 1400.0f;
    [SerializeField] private float _rotationAcceleration = 5000.0f;
    [Space]
    [SerializeField] private bool _useSnappyRotations = false;
    [Space]
    [SerializeField, Tooltip("Allow rotating around the local x axis")] private bool _rotateAroundX = false;
    [SerializeField, Tooltip("If desired rot is bigger than this value, movement will be paused untill rotated enough")] private float _anglePauseMoveBase = 360.0f;
    [Space]
    [SerializeField, Tooltip("Grounded only local x axis rotation for when on slopes")] private bool _rotateForSlopes = false;
    [SerializeField] private float _rotateForSlopesSphereCastRadius = 0.5f;
    [SerializeField] private Transform _rotateForSlopesT = null;


    private NavMeshAgent _agent = null;

    private static float _minAngleForRotate = 1.0f;
    private static float _minVelocityForDecelerate = 0.1f;

    private float _velAlignment = 0.0f;

    // Modifiers
    private List<MovementModifier> _modifiers = new List<MovementModifier>();
    private float _maxSpeedModifier = 0.0f;
    private float _accelerationModifier = 0.0f;
    private float _decelerationModifier = 0.0f;
    private float _anglePauseMoveCurrent = 0.0f;
    private float _gravityModifier = 0.0f;
    #endregion

    #region Properties

    public float MaxSpeedAdjusted { get; private set; }
    public float AccelerationAdjusted { get; private set; }
    public float DecelerationdAdjusted { get; private set; }
    public float RotationMultiplier { get; private set; }

    public Rigidbody RB { get; private set; }
    public NavMeshAgent Agent { get { return _agent; } }

    public Vector3 DesiredMovement { get { return _agent.desiredVelocity; } }
    public Vector3 AdjustedDesiredMovement { get; private set; } //corrected current movement direction (desired movement but changed)
    public Vector3 DesiredForward { get; set; }

    // Current move speed and velocity are different from RB.velocity and its magnitude if we can only move horizontally (in those cases its only horizontal speed/vel)
    public float CurrentMoveSpeed { get; private set; }
    public Vector3 CurrentMoveVelocity { get; private set; }

    public float CurrentRotationSpeed { get; private set; }


    public bool IsStopped
    {
        get { return _agent.isStopped; }
        set
        {
            if (_agent.isOnNavMesh) _agent.isStopped = value;
        }
    }
    public bool CanRotate { get; set; }
    public bool DisableGravity { get; set; }
    public float MinAngleForceRotateOnly { get { return _anglePauseMoveBase; } set { _anglePauseMoveBase = value; } }

    public float VelocityPercentage { get { return CurrentMoveSpeed / MaxSpeedAdjusted; } } //return percentage based on base max speed

    public bool IsModified { get { return _modifiers.Count > 0; } }

    #endregion



    #region Public Functions

    public bool MoveToPos(Vector3 pos, bool canRotate = true, bool setRotationToMovement = true)
    {
        //DesiredMovement = pos - transform.position;
        //if (setRotationToMovement) DesiredForward = DesiredMovement;

        bool succes = _agent.SetDestination(pos);
        if (!succes) return false;

        IsStopped = false;
        CanRotate = canRotate;

        return true;
    }

    public bool DestinationReached()
    {
        if (_agent.pathPending) return false;
        
        return _agent.remainingDistance < _agent.stoppingDistance;
    }

    public void AddOrUpdateModifier(MovementModifier templateMod, bool forceCopy = true, bool forceAdd = false)
    {
        if (!(templateMod.duration > 0.0f)) return;

        //Check if modifier exists and update it instead of creating new one (needs to be same source)
        MovementModifier moveMod = forceAdd ? null : _modifiers.Find(mod => mod.Source == templateMod.Source);

        if (moveMod == null)
        {
            moveMod = forceCopy ? templateMod.Copy() : templateMod;
            _modifiers.Add(moveMod);
        }

        //Reset elapsed in case it was reused
        moveMod.elapsed = 0.0f;

        //sort on priority if more than 1 element
        if (_modifiers.Count > 1)
        {
            _modifiers.Sort((a, b) =>
            {
                return a.priority.CompareTo(b.priority);
            });
        }

        ReCalculateModifiers();
    }

    // Removes all mods which have source equal to param
    public void RemoveMod(GameObject source)
    {
        for (int i = 0; i < _modifiers.Count; i++)
        {
            MovementModifier moveMod = _modifiers[i];
            if (moveMod.Source != source) continue;

            _modifiers.RemoveAt(i);
            i--;
        }

        ReCalculateModifiers();
    }

    public void RemoveMod(MovementModifier mod)
    {
        for (int i = 0; i < _modifiers.Count; i++)
        {
            MovementModifier moveMod = _modifiers[i];
            if (moveMod != mod) continue;

            _modifiers.RemoveAt(i);
            i--;
        }

        ReCalculateModifiers();
    }

    public void ClearModifiers()
    {
        _modifiers.Clear();
        ReCalculateModifiers();
    }

    public void Stop()
    {
        IsStopped = true;
        CurrentMoveSpeed = 0.0f;
        CurrentRotationSpeed = 0.0f;
        RB.linearVelocity = Vector3.zero;
        CurrentMoveVelocity = Vector3.zero;
    }
    #endregion

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        _agent.updateRotation = false;
        CanRotate = true;


        // Get RB and initialize
        RB = GetComponent<Rigidbody>();
        RB.maxLinearVelocity = _maxLinearVel;

        // Gravity is handled by this script
        RB.useGravity = false;
    }

    private void Start()
    {
        // Inital mod calculations (very important)
        ReCalculateModifiers();
    }

    #region Updates

    private void Update()
    {
        UpdateModifiers();
    }

    private void FixedUpdate()
    {
        // Cache velocity and speed before making any changes first
        CurrentMoveVelocity = _agent.velocity;
        CurrentMoveSpeed = CurrentMoveVelocity.magnitude;

        // ORDER IS IMPORTANT
        //CalculateAdjustedDesiredMovement(); //start by evaluating our desired movement and adjusting it if necessary/desired

        //Calculate and cache alignment of desired movement to our current velocity
        _velAlignment = Vector3.Dot(AdjustedDesiredMovement.normalized, CurrentMoveVelocity.normalized);

        CalculateDataAdjustements();
        UpdateRotation();
    }

    //private void CalculateAdjustedDesiredMovement()
    //{
    //    //return if zero vec
    //    if (DesiredMovement == Vector3.zero)
    //    {
    //        AdjustedDesiredMovement = DesiredMovement;
    //        return;
    //    }

    //    //Normalize
    //    Vector3 adjusted = DesiredMovement.normalized;

    //    //if above min angle for force rotate only, set adjusted to 0 (because this means we cannot move untill rotated)
    //    if (Vector3.Angle(adjusted, transform.forward) > _anglePauseMoveCurrent) adjusted = Vector3.zero;
    //    //else if limit movement to rotation and we cant fly or we can rotate around x axis
    //    else if (LimitMoveToRot/* && (!_canFly || _rotateAroundX)*/)
    //    {
    //        //set adjusted to forward
    //        adjusted = transform.forward;

    //        //prevents from not reaching target pos when limit movement to rotation (cuz we will never rotate up/down thus never move up/down)
    //        if (!_rotateAroundX) adjusted.y = DesiredMovement.y;
    //    }

    //    //if we cannot fly take out y part
    //    if (!CurrentData.canMoveVertical)
    //    {
    //        adjusted.y = 0.0f;
    //        adjusted.Normalize();
    //    }

    //    //adjust to slopes
    //    if (_adjustToSlopes && IsGrounded && adjusted != Vector3.zero /*&& Vector3.Dot(adjusted, GroundedSurfaceNormal) > 0.0f*/)
    //    {
    //        //alignment
    //        adjusted = Vector3.ProjectOnPlane(adjusted, GroundedSurfaceNormal);

    //        //normally when grounded no gravity is added, we do this here because otherwise the slope movement can feel bumby
    //        RB.AddForce(GroundedSurfaceNormal * Physics.gravity.y, ForceMode.Acceleration);
    //    }

    //    //Finally normalize a final time and cache
    //    adjusted.Normalize();
    //    AdjustedDesiredMovement = adjusted;

    //    //Debug.DrawRay(transform.position, AdjustedDesiredMovement.normalized * 10.0f);
    //}

    private void CalculateDataAdjustements()
    {
        // Reset cached modifiers from data and apply initial modifier
        MaxSpeedAdjusted = _baseMaxSpeed * _maxSpeedModifier;
        AccelerationAdjusted = _baseAcceleration * _accelerationModifier;

        // SOFT INPUT
        //if (_allowSoftInput)
        //{
        //    float softInputMult = Vector3.ClampMagnitude(DesiredMovement, 1.0f).magnitude;
        //    MaxSpeedAdjusted *= softInputMult;
        //    AccelerationAdjusted *= softInputMult;
        //}

        // ACCELERATION -> Clamp acceleration to maxspeed / deltatime so we do not add more speed than allowed (otherwise too large acceleration values break movement)
        AccelerationAdjusted = Mathf.Clamp(AccelerationAdjusted, 0.0f, MaxSpeedAdjusted / Time.deltaTime);

        // Set to agent
        _agent.speed = MaxSpeedAdjusted;
        _agent.acceleration = AccelerationAdjusted;
    }

    //private void UpdateGravity()
    //{
    //    if (/*IsGrounded || */DisableGravity) return;
    //    if (RB.linearVelocity.y < _maxYVelForGravity) return;

    //    RB.AddForce(Vector3.up * CurrentData.gravity * _gravityModifier, ForceMode.Acceleration);
    //}

    private void UpdateRotation()
    {
        if (!CanRotate) return;

        Vector3 desiredDir = DesiredForward;

        //ignore y if cant rotate around X or cannot fly
        //if (!_rotateAroundX || !CurrentData.canMoveVertical && !IsGrounded) desiredDir.y = 0.0f;

        //slop rotation -> use raycast straight down so that if we rotate we dont get jittery movement
        //if (_rotateForSlopes && Physics.Raycast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), Vector3.down, out RaycastHit hitInfo, 1.5f, ~_ignoreForGrounded, QueryTriggerInteraction.Ignore))
        //if (IsGrounded && CurrentMoveSpeed > 0.1f && _rotateForSlopes && Physics.SphereCast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), _rotateForSlopesSphereCastRadius, Vector3.down, out RaycastHit hitInfo,
        //    1.1f + _rotateForSlopesSphereCastRadius, ~_ignoreForGrounded, QueryTriggerInteraction.Ignore))
        //{
        //    desiredDir = Vector3.ProjectOnPlane(desiredDir.normalized, hitInfo.normal);
        //}
        //else if (!_rotateAroundX || !CurrentData.canMoveVertical) desiredDir.y = 0.0f;


        // If moving, use agent velocity as desired forward (this overrides desired forward set by default for now)
        if (_agent.desiredVelocity.magnitude > 0.0f) desiredDir = _agent.desiredVelocity;
        else desiredDir = DesiredForward.normalized;

        // Could be zero vector
        if (desiredDir == Vector3.zero) return;

        // Calculate desired angle to check if rotate partially
        if (_useSnappyRotations || Vector3.Angle(transform.forward, desiredDir) < _minAngleForRotate)
        {
            transform.forward = desiredDir;
            CurrentRotationSpeed = 0.0f;
        }
        else
        {
            desiredDir.Normalize();

            // Update rotation speed with acceleration 
            CurrentRotationSpeed = Mathf.Clamp(CurrentRotationSpeed + _rotationAcceleration * RotationMultiplier * Time.deltaTime, 
                0.0f, _maxRotationSpeed * RotationMultiplier);

            // Rotate
            Quaternion desiredRotation = Quaternion.LookRotation(desiredDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, CurrentRotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateModifiers()
    {
        int nrMods = _modifiers.Count;
        for (int i = 0; i < _modifiers.Count; i++)
        {
            MovementModifier mod = _modifiers[i];

            //update mod activeness
            mod.elapsed += Time.deltaTime;
            if (mod.elapsed >= mod.duration /*&& !mod.keepAlive*/)
            {
                _modifiers.Remove(mod);
                i--;
            }
        }

        //If any were removed, recalculate modifiers
        if (nrMods != _modifiers.Count)
            ReCalculateModifiers();
    }

    private void ReCalculateModifiers()
    {
        // Reset
        _maxSpeedModifier = 1.0f;
        _accelerationModifier = 1.0f;
        _decelerationModifier = 1.0f;
        _gravityModifier = 1.0f;
        RotationMultiplier = 1.0f;
        _anglePauseMoveCurrent = _anglePauseMoveBase;

        // Loop over active mods (sorted on priority)
        for (int i = 0; i < _modifiers.Count; i++)
        {
            MovementModifier mod = _modifiers[i];

            // Apply mod
            _maxSpeedModifier *= mod.maxSpeedMultiplier;
            _accelerationModifier *= mod.accelerationMultiplier;
            _decelerationModifier *= mod.decelerationMultiplier;
            RotationMultiplier *= mod.rotationMultiplier;
            _gravityModifier *= mod.gravityMultiplier;

            if (mod.overrideAnglePauseMove) _anglePauseMoveCurrent = mod.anglePauseMove;
        }

        bool speedZero = _maxSpeedModifier < 0.01f;
        if (speedZero) _agent.isStopped = true;
    }

    #endregion
}