using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class FreeMovement : NetworkBehaviour
{
    #region Fields
    [Header("General"), SerializeField] private List<MoveData> _moveData = new List<MoveData>();//1st of each move type in list is considered default
    [SerializeField, Tooltip("Threshold for when to decelerate while moving (1 being always and -1 never), compared with input alignment to velocity"), Range(-1.0f, 0.99f)] private float _decelerateThreshold = 0.75f;
    [SerializeField, Tooltip("Multiplier when input is available and deceleration is applied")] private float _decelInputMultiplier = 0.25f;
    [SerializeField, Tooltip("Whether to allow vector inputs with a length smaller than 1 affect movement calculations")] private bool _allowSoftInput = false;

    [Space]
    [SerializeField, Tooltip("Required for stepping (automatically looked for on this object)")] private Collider _mainCollider = null;
    [SerializeField, Tooltip("Stop adding gravity force when y velocity below this value")] private float _maxYVelForGravity = -30.0f;
    [SerializeField] private float _maxLinearVel = 100.0f;

    [Header("Grounded"), SerializeField] private float _groundedAngle = 45.0f;
    [SerializeField, Tooltip("Aligns desired movement with slope angle")] private bool _adjustToSlopes = true;
    [SerializeField, Tooltip("Layer mask which prevents walking on certain surfaces")] private LayerMask _ignoreForGrounded = 0;
    [Space]
    [SerializeField, Tooltip("Min/Max vel mult on grounded")] private Vector2 _onGroudedVelMultBounds = new Vector2(1.0f, 0.0f);
    [SerializeField, Tooltip("Bounds for scaling between vel mult bounds based on vel at impact")] private Vector2 _onGroundedVelocityScaleBounds = new Vector2(0.05f, 3.0f);

    [Header("Stepping"), SerializeField] private bool _disableStepping = false;
    [SerializeField] private bool _disableSteppingAir = false;
    [SerializeField, Tooltip("Time after ungrounded we can still step")] private float _airStepTime = 0.2f;
    [SerializeField] private float _maxYVelForStep = 1.0f;
    [SerializeField, Tooltip("Maxixum height we can try to step on")] private float _stepHeight = 0.4f;
    [SerializeField, Tooltip("Step force bounds, lerped between based on the size of the step")] private Vector2 _stepForceBounds = new Vector2(0.0f, 2.0f);
    [SerializeField, Tooltip("How far in front of us should we look for stepping (starts from outside collider size)")] private float _stepDistanceOffset = 0.5f;
    [SerializeField] private float _stepAngleCorrection = 0.2f;
    [SerializeField] private float _stepCooldown = 0.2f;
    [SerializeField, Tooltip("Decides what can be stepped on")] private LayerMask _stepLayerMask = 0;
    [SerializeField, Tooltip("Timer after starting to step which we can continue doing so while technically still in air, we also stay 'grounded' for this duration")] private float _stepDuration = 0.25f;
    [SerializeField] private ForceMode _stepForceMode = ForceMode.Acceleration;

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

    // Current mapped move ids to each move type (can be changed as desired, defaults will be set)
    private Dictionary<MoveType, MoveID> _mappedMoveData = new Dictionary<MoveType, MoveID>(); 
    private MoveData _currentData = null;

    private List<Collider> _groundedObjects = new List<Collider>();

    private bool _isGroundedPrev = false;
    private bool _isGrounded = false;

    private static float _minAngleForRotate = 1.0f;
    private static float _minVelocityForDecelerate = 0.1f;

    private float _stepTimer = 0.0f;
    private float _stepCooldownTimer = 0.0f;

    private float _velAlignment = 0.0f;

    private float _maxSpeedBufferCurrent = 0.0f;
    private float _maxSpeedBufferElapsed = 0.0f;

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

    public MoveData CurrentData
    {
        get
        {
            if (_currentData == null) UpdateCurrentMoveData();
            return _currentData;
        }
        private set { _currentData = value; }
    }

    public List<MoveData> MoveData
    {
        get { return _moveData; }
        set
        {
            _moveData = value;

            //will set new move data for same mapping
            UpdateCurrentMoveData();
        }
    }

    public Collider MainCollider { get { return _mainCollider; } private set { _mainCollider = value; } }
    public Rigidbody RB { get; private set; }

    public Vector3 DesiredMovement { get; set; }
    public Vector3 AdjustedDesiredMovement { get; private set; } //corrected current movement direction (desired movement but changed)
    public Vector3 DesiredForward { get; set; }

    //Current move speed and velocity are different from RB.velocity and its magnitude if we can only move horizontally (in those cases its only horizontal speed/vel)
    public float CurrentMoveSpeed { get; private set; }
    public Vector3 CurrentMoveVelocity { get; private set; }

    public float CurrentRotationSpeed { get; private set; }
    public float AirTime { get; set; }
    public bool IsStepping
    {
        get { return _stepTimer > 0.0f; }
        private set
        {
            _stepTimer = value ? _stepDuration : 0.0f;
        }
    }

    public bool IsStopped { get; set; }
    public bool CanRotate { get; set; }
    public bool DisableGravity { get; set; }
    public bool IgnoreDecelerate { get; set; }
    public bool LimitMoveToRot { get; set; }//this will make sure we always move in forward dir (for more realistic movement)
    public float MinAngleForceRotateOnly { get { return _anglePauseMoveBase; } set { _anglePauseMoveBase = value; } }

    public Vector3 GroundedSurfaceNormal { get; private set; } // Only valid when grounded
    public bool IsGrounded { get { return _isGrounded; } }
    public float GroundedAngle { get { return _groundedAngle; } }
    public Collision GroundedCollision { get; private set; } // Last grounded collision
    public List<Collider> GroundedObjects { get { return _groundedObjects; } }

    public float VelocityPercentage { get { return CurrentMoveSpeed / MaxSpeedAdjusted; } } //return percentage based on base max speed

    public bool IsModified { get { return _modifiers.Count > 0; } }
    public Surface CurrentSurface { get; private set; }

    #endregion


    public Action OnGrounded;
    public Action OnStopGrounded;


    #region Public Functions

    public void ChangeMoveDataForType(MoveType type, MoveID ID)
    {
        if (!_mappedMoveData.ContainsKey(type)) return;

        //try and find target moveData
        MoveData moveData = _moveData.Find(data => data.MoveID == ID);
        if (moveData == null) return;

        //map data
        _mappedMoveData[type] = ID;

        //make sure to update current move data as it might take effect immediatly
        UpdateCurrentMoveData();
    }

    public MoveID GetMappedMoveID(MoveType type)
    {
        if (!_mappedMoveData.ContainsKey(type)) return 0;

        return _mappedMoveData[type];
    }

    public void MoveToPos(Vector3 pos, bool canRotate = true, bool setRotationToMovement = true)
    {
        DesiredMovement = pos - transform.position;
        if (setRotationToMovement) DesiredForward = DesiredMovement;

        IsStopped = false;
        CanRotate = canRotate;
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

    [ClientRpc(RequireOwnership = false)]
    public void AddOrUpdateModifierClientRPC(MovementModifier modifier)
    {
        AddOrUpdateModifier(modifier);
    }

    [ClientRpc(RequireOwnership = false)]
    public void AddForceClientRPC(Vector3 force, ForceMode forceMode)
    {
        RB.AddForce(force, forceMode);
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

    public void ClearGrounded()
    {
        if (_groundedObjects.Count > 0)
        {
            _groundedObjects.Clear();
            CheckGrounded(null);
        }
    }

    public void Stop()
    {
        IsStopped = true;
        CurrentMoveSpeed = 0.0f;
        CurrentRotationSpeed = 0.0f;
        RB.linearVelocity = Vector3.zero;
        CurrentMoveVelocity = Vector3.zero;

        _maxSpeedBufferElapsed = 0.0f;
        _maxSpeedBufferCurrent = 0.0f;
    }
    #endregion

    private void Awake()
    {
        if (!MainCollider) MainCollider = GetComponent<Collider>();

        //Get RB and initialize
        RB = GetComponent<Rigidbody>();
        RB.maxLinearVelocity = _maxLinearVel;

        //Gravity is handled by this script
        RB.useGravity = false;

        InitMoveDataMapping();

        //update move data on grounded change
        OnGrounded += UpdateCurrentMoveData;
        OnStopGrounded += UpdateCurrentMoveData;
    }

    private void Start()
    {
        //Inital mod calculations (very important)
        ReCalculateModifiers();
    }

    private void OnEnable()
    {
        ClearGrounded();
        UpdateCurrentMoveData();
    }

    private void InitMoveDataMapping()
    {
        //Clear any intial data
        _mappedMoveData.Clear();

        //init move data dictionary
        foreach (MoveData moveData in _moveData)
        {
            //1st of each move type in list is considered default, so continue if already there
            if (_mappedMoveData.ContainsKey(moveData.MoveType)) continue;

            _mappedMoveData.Add(moveData.MoveType, moveData.MoveID);
        }

        //make sure current data is set
        UpdateCurrentMoveData();
    }

    private void UpdateCurrentMoveData()
    {
        if (MoveData.Count == 0) return;

        if (IsGrounded)
        {
            if (_mappedMoveData.ContainsKey(MoveType.ground)) _currentData = _moveData.Find(x => x.MoveID == _mappedMoveData[MoveType.ground]);
            if (_currentData == null) Debug.Log("No move data found for ground movement: " + this.gameObject);
        }
        else
        {
            if (_mappedMoveData.ContainsKey(MoveType.air)) _currentData = _moveData.Find(x => x.MoveID == _mappedMoveData[MoveType.air]);
            if (_currentData == null) Debug.Log("No move data found for ground movement: " + this.gameObject);
        }

        //define other movement types here (like swimming when in water volume))

    }

    #region Updates

    private void Update()
    {
        UpdateGrounded();
        UpdateModifiers();
    }

    private void FixedUpdate()
    {
        //cache velocity and speed before making any changes first (will prob get changed during this update)
        if (CurrentData != null) CurrentMoveVelocity = CurrentData.canMoveVertical || IsGrounded ? RB.linearVelocity : Utils.HorizontalVector(RB.linearVelocity);
        CurrentMoveSpeed = CurrentMoveVelocity.magnitude;

        //ORDER IS IMPORTANT
        CalculateAdjustedDesiredMovement(); //start by evaluating our desired movement and adjusting it if necessary/desired

        //Calculate and cache alignment of desired movement to our current velocity
        _velAlignment = Vector3.Dot(AdjustedDesiredMovement.normalized, CurrentMoveVelocity.normalized);

        CalculateDataAdjustements();
        UpdateDeceleration(); //will possibly change current vel/speed (so do first thing after adjusted desired is calculated)
        UpdateGravity();
        UpdateRotation();
        UpdateMovement();
        UpdateStepping();
    }

    private void CalculateAdjustedDesiredMovement()
    {
        if (CurrentData == null) return;

        //return if zero vec
        if (DesiredMovement == Vector3.zero)
        {
            AdjustedDesiredMovement = DesiredMovement;
            return;
        }

        //Normalize
        Vector3 adjusted = DesiredMovement.normalized;

        //if above min angle for force rotate only, set adjusted to 0 (because this means we cannot move untill rotated)
        if (Vector3.Angle(adjusted, transform.forward) > _anglePauseMoveCurrent) adjusted = Vector3.zero;
        //else if limit movement to rotation and we cant fly or we can rotate around x axis
        else if (LimitMoveToRot/* && (!_canFly || _rotateAroundX)*/)
        {
            //set adjusted to forward
            adjusted = transform.forward;

            //prevents from not reaching target pos when limit movement to rotation (cuz we will never rotate up/down thus never move up/down)
            if (!_rotateAroundX) adjusted.y = DesiredMovement.y;
        }

        //if we cannot fly take out y part
        if (!CurrentData.canMoveVertical)
        {
            adjusted.y = 0.0f;
            adjusted.Normalize();
        }

        //adjust to slopes
        if (_adjustToSlopes && IsGrounded && adjusted != Vector3.zero /*&& Vector3.Dot(adjusted, GroundedSurfaceNormal) > 0.0f*/)
        {
            //alignment
            adjusted = Vector3.ProjectOnPlane(adjusted, GroundedSurfaceNormal);

            //normally when grounded no gravity is added, we do this here because otherwise the slope movement can feel bumby
            RB.AddForce(GroundedSurfaceNormal * Physics.gravity.y, ForceMode.Acceleration);
        }

        //Finally normalize a final time and cache
        adjusted.Normalize();
        AdjustedDesiredMovement = adjusted;

        //Debug.DrawRay(transform.position, AdjustedDesiredMovement.normalized * 10.0f);
    }

    private void CalculateDataAdjustements()
    {
        if (CurrentData == null) return;

        //Reset cached modifiers from data and apply initial modifier
        MaxSpeedAdjusted = _currentData.maxSpeed * _maxSpeedModifier;
        AccelerationAdjusted = _currentData.acceleration * _accelerationModifier;
        DecelerationdAdjusted = _currentData.deceleration * _decelerationModifier;

        //SPEED BOUNDS
        if (CurrentData.useSpeedBounds)
        {
            //calculate percentage of how much desired movement is aligned with our forward [0, 1]
            float moveDirPerc = (Vector3.Dot(AdjustedDesiredMovement, transform.forward) + 1.0f) / 2.0f;
            float boundedPerc = Mathf.Lerp(CurrentData.speedBounds.x, CurrentData.speedBounds.y, moveDirPerc);

            MaxSpeedAdjusted *= boundedPerc;
            //AccelerationAdjusted *= boundedPerc;
            //DecelerationdAdjusted *= boundedPerc;
        }

        //MAX SPEED BUFFFER
        float target;
        if (_maxSpeedBufferCurrent < _currentData.maxSpeedBuffer && _velAlignment > CurrentData.maxSpeedBufferAccuracy)
        {
            _maxSpeedBufferElapsed += Time.deltaTime;

            //Start increasing buffer
            if (_maxSpeedBufferElapsed > CurrentData.maxSpeedBufferMinTime)
            {
                target = CurrentData.maxSpeedBuffer;
            }
            else target = 0;
        }
        else
        {
            target = 0;
            _maxSpeedBufferElapsed = 0.0f;
            if (CurrentMoveSpeed < 0.1f) _maxSpeedBufferCurrent = 0.0f;
        }
        float bufferSpeed = target > _maxSpeedBufferCurrent ? _currentData.maxSpeedBufferAcceleration : -_currentData.maxSpeedBufferDeceleration;
        _maxSpeedBufferCurrent = Mathf.Clamp(_maxSpeedBufferCurrent + bufferSpeed * Time.deltaTime, 0.0f, float.MaxValue);

        MaxSpeedAdjusted += _maxSpeedBufferCurrent;


        //if (_velAlignment > CurrentData.maxSpeedBufferAccuracy)
        //{
        //    _maxSpeedBufferElapsed += Time.deltaTime;

        //    //Start increasing buffer
        //    if (_maxSpeedBufferElapsed > CurrentData.maxSpeedBufferMinTime)
        //    {
        //        //_maxSpeedBufferCurrent = Mathf.Lerp(_maxSpeedBufferCurrent, CurrentData.maxSpeedBuffer, Time.deltaTime * CurrentData.maxSpeedBufferAcceleration);
        //        _maxSpeedBufferCurrent += Time.deltaTime * CurrentData.maxSpeedBufferAcceleration;
        //    }
        //}
        //else
        //{
        //    _maxSpeedBufferElapsed = 0.0f;
        //    //_maxSpeedBufferCurrent = 0.0f;
        //}
        //_maxSpeedBufferCurrent = Mathf.Clamp(_maxSpeedBufferCurrent, 0.0f, CurrentData.maxSpeedBuffer);



        //SOFT INPUT
        if (_allowSoftInput)
        {
            float softInputMult = Vector3.ClampMagnitude(DesiredMovement, 1.0f).magnitude;
            MaxSpeedAdjusted *= softInputMult;
            AccelerationAdjusted *= softInputMult;
        }

        //DECELERATION MULTIPLIER -> Deceleration multiplier while input, if we are above max speed we do not want to change deceleration
        if (AdjustedDesiredMovement != Vector3.zero && CurrentMoveSpeed < MaxSpeedAdjusted)
        {
            //multiply with vel alignment [-1,1] put into [1,0] range (more disalignment means more decel)
            float alignmentScale = 1.0f - ((_velAlignment + 1.0f) / 2.0f); // 1 -> full disalignment
            DecelerationdAdjusted *= _decelInputMultiplier * alignmentScale;
        }

        //ACCELERATION -> Clamp acceleration to maxspeed / deltatime so we do not add more speed than allowed (otherwise too large acceleration values break movement)
        AccelerationAdjusted = Mathf.Clamp(AccelerationAdjusted, 0.0f, MaxSpeedAdjusted / Time.deltaTime);
    }

    private void UpdateDeceleration()
    {
        if (CurrentData == null) return;

        //Return if ignore, decelerationAdjusted is 0 or move speed is 0
        if (IgnoreDecelerate || !(DecelerationdAdjusted > 0.0f) || !(CurrentMoveSpeed > 0.0f)) return;

        //If we have input AND under maxSpeedAdjusted AND velocity alignment is above threshold, return
        if (AdjustedDesiredMovement != Vector3.zero && CurrentMoveSpeed < MaxSpeedAdjusted && _velAlignment > _decelerateThreshold) return;

        if (CurrentMoveSpeed > _minVelocityForDecelerate)
        {
            //calculate new speed and velocity
            CurrentMoveSpeed = Mathf.Clamp(CurrentMoveSpeed - DecelerationdAdjusted * Time.deltaTime, 0.0f, float.MaxValue);
            CurrentMoveVelocity = CurrentMoveVelocity.normalized * CurrentMoveSpeed;
        }
        else
        {
            //set to zero
            CurrentMoveVelocity = Vector3.zero;
            CurrentMoveSpeed = 0.0f;
        }

        //sync RB vel
        SyncRBVelocity();
    }

    private void UpdateGravity()
    {
        if (CurrentData == null) return;

        if (/*IsGrounded || */DisableGravity) return;
        if (RB.linearVelocity.y < _maxYVelForGravity) return;

        RB.AddForce(Vector3.up * CurrentData.gravity * _gravityModifier, ForceMode.Acceleration);
    }

    private void UpdateMovement()
    {
        if (IsStopped || AdjustedDesiredMovement == Vector3.zero) return;

        //calculate move force
        Vector3 moveForce = AdjustedDesiredMovement * AccelerationAdjusted;

        //Calculate velocity that would be added by applying move force (F = v / t)
        Vector3 predictedAddedVel = moveForce * Time.deltaTime;

        //If we are over max speed and we would increase it, lower current speed by to be added speed(this way, at max speed, we can still strafe other ways but never increase our total speed via input)
        float predictedAddedSpeed = predictedAddedVel.magnitude;
        float predictedTotalSpeed = CurrentMoveSpeed + predictedAddedSpeed;

        //would we exceed max speed by adding the current force amount? (also check if we would actually inscrease speed)
        if (predictedTotalSpeed > MaxSpeedAdjusted && predictedTotalSpeed > CurrentMoveSpeed)
        {
            //there might be some speed still left before reaching max speed
            float speedAllowed = Mathf.Clamp(MaxSpeedAdjusted - CurrentMoveSpeed, 0.0f, float.MaxValue);

            //calculate new speed by lowering current velocity untill we are at (max - speedToAdd) and set on rigidbody
            float newSpeed = CurrentMoveSpeed - (predictedAddedSpeed - speedAllowed);
            CurrentMoveVelocity = CurrentMoveVelocity.normalized * newSpeed;

            //sync RB vel
            SyncRBVelocity();
        }

        //finally, add the force
        RB.AddForce(moveForce, ForceMode.Acceleration);
    }

    private void UpdateRotation()
    {
        if (!CanRotate || DesiredForward == Vector3.zero) return;

        Vector3 desiredDir = DesiredForward;

        //ignore y if cant rotate around X or cannot fly
        //if (!_rotateAroundX || !CurrentData.canMoveVertical && !IsGrounded) desiredDir.y = 0.0f;

        //slop rotation -> use raycast straight down so that if we rotate we dont get jittery movement
        //if (_rotateForSlopes && Physics.Raycast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), Vector3.down, out RaycastHit hitInfo, 1.5f, ~_ignoreForGrounded, QueryTriggerInteraction.Ignore))
        if (IsGrounded && CurrentMoveSpeed > 0.1f && _rotateForSlopes && Physics.SphereCast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), _rotateForSlopesSphereCastRadius, Vector3.down, out RaycastHit hitInfo,
            1.1f + _rotateForSlopesSphereCastRadius, ~_ignoreForGrounded, QueryTriggerInteraction.Ignore))
        {
            desiredDir = Vector3.ProjectOnPlane(desiredDir.normalized, hitInfo.normal);
        }
        else if (!_rotateAroundX || !CurrentData.canMoveVertical) desiredDir.y = 0.0f;

        //could be zero vector
        if (desiredDir == Vector3.zero) return;

        //calculate desired angle to check if rotate partially
        if (_useSnappyRotations || Vector3.Angle(transform.forward, desiredDir) < _minAngleForRotate)
        {
            transform.forward = desiredDir;
            CurrentRotationSpeed = 0.0f;
        }
        else
        {
            desiredDir.Normalize();

            //update rotation speed with acceleration 
            CurrentRotationSpeed = Mathf.Clamp(CurrentRotationSpeed + _rotationAcceleration * RotationMultiplier * Time.deltaTime, 
                0.0f, _maxRotationSpeed * RotationMultiplier);

            //rotate
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
        //Reset
        _maxSpeedModifier = 1.0f;
        _accelerationModifier = 1.0f;
        _decelerationModifier = 1.0f;
        _gravityModifier = 1.0f;
        RotationMultiplier = 1.0f;
        _anglePauseMoveCurrent = _anglePauseMoveBase;

        //Loop over active mods (sorted on priority)
        for (int i = 0; i < _modifiers.Count; i++)
        {
            MovementModifier mod = _modifiers[i];

            //apply mod
            _maxSpeedModifier *= mod.maxSpeedMultiplier;
            _accelerationModifier *= mod.accelerationMultiplier;
            _decelerationModifier *= mod.decelerationMultiplier;
            RotationMultiplier *= mod.rotationMultiplier;
            _gravityModifier *= mod.gravityMultiplier;

            if (mod.overrideAnglePauseMove) _anglePauseMoveCurrent = mod.anglePauseMove;
        }
    }

    private void UpdateGrounded()
    {
        //Are any grounded objects disabled or deleted?
        for (int i = 0; i < _groundedObjects.Count; i++)
        {
            if (_groundedObjects[i] != null && _groundedObjects[i].gameObject.activeInHierarchy && _groundedObjects[i].enabled) continue;

            _groundedObjects.RemoveAt(i);
            CheckGrounded(null);
            i--;
        }

        //update air time
        if (!IsGrounded) AirTime += Time.deltaTime;
    }

    private void CheckGrounded(Collision collision)
    {
        _isGrounded = _groundedObjects.Count > 0;

        //Re-evaluate whether we are on surface (Accept first found)
        foreach (Collider collider in _groundedObjects)
        {
            CurrentSurface = collider.GetComponent<Surface>();
            if (CurrentSurface != null) break;
        }

        //Evaluate Current grounded state compared to last frame and fire events if necessary
        //Internal code that should fire before or after the event should be put here
        if (IsGrounded && !_isGroundedPrev)
        {
            GroundedCollision = collision;
            OnGrounded?.Invoke();
            _isGroundedPrev = true;

            //Vel multiplier
            float velMultScale = Mathf.InverseLerp(_onGroundedVelocityScaleBounds.x, _onGroundedVelocityScaleBounds.y, collision.relativeVelocity.magnitude);

            Vector2 velMultBounds = _onGroudedVelMultBounds;
            if (CurrentSurface != null && CurrentSurface.Asset && CurrentSurface.Asset.OverrideVelMultBounds) velMultBounds = CurrentSurface.Asset.OnGroundedVelMultBounds;
            float velMultiplier = Mathf.Lerp(velMultBounds.x, velMultBounds.y, velMultScale);

            RB.linearVelocity *= velMultiplier;

            //Reset air time
            AirTime = 0.0f;
        }
        else if (!IsGrounded && _isGroundedPrev)
        {
            GroundedCollision = collision;
            CurrentSurface = null;
            OnStopGrounded?.Invoke();
            _isGroundedPrev = false;
        }
    }

    //this is meant to sync the RB velocity with the move velocity(assumed to be changed), taking into account whether we can move vertically or not (leaving that part unchanged)
    private void SyncRBVelocity()
    {
        //figure out if y part of RB velocity should be changed
        float yVel = CurrentData.canMoveVertical || IsGrounded ? CurrentMoveVelocity.y : RB.linearVelocity.y;

        //set RB velocity
        RB.linearVelocity = new Vector3(CurrentMoveVelocity.x, yVel, CurrentMoveVelocity.z);
    }

    private void UpdateStepping()
    {
        if (_disableStepping) return;

        //Update step timers
        _stepTimer -= Time.deltaTime;
        _stepCooldownTimer -= Time.deltaTime;

        // If step not in progress AND if air stepping is not allowed, not grounded and air time is bigger than time allowed after unground return
        if (_stepTimer < 0.0f && _disableSteppingAir && !IsGrounded && (AirTime > _airStepTime)) return;

        // Dont allow step above max Y vel
        if (RB.linearVelocity.y > _maxYVelForStep) return;

        //Is stepping on cooldown and not in progress and no input, return
        if ((!IsStepping && _stepCooldownTimer > 0.0f) || AdjustedDesiredMovement == Vector3.zero) return;

        //Calculate max step distance
        float maxDistance = MainCollider.bounds.size.z + _stepDistanceOffset + 0.01f;

        //Set origin to our position with sleight y offset (to make sure we are not alligning with ground)
        Vector3 origin = transform.position;
        origin.y += 0.01f;

        //Horizontal raycast -> check if anything could be stepped and if so how far is it
        if (!Physics.Raycast(origin, AdjustedDesiredMovement, out RaycastHit horizontalHit, maxDistance, _stepLayerMask, QueryTriggerInteraction.Ignore))
        {
            // Debug.DrawRay(horizontalHit.point, horizontalHit.normal * 2.0f, Color.red);
            //Return if first ray misses
            return;
        }

        // Debug.DrawRay(origin, AdjustedDesiredMovement * maxDistance, Color.green);

        //Calculate step distance + heightOffset
        float stepDistance = horizontalHit.distance + _stepAngleCorrection;
        float heightOffset = MainCollider.bounds.size.y;

        //Move origin
        origin += stepDistance * AdjustedDesiredMovement;
        origin.y += heightOffset;

        //Vertical raycast -> shoot from above step, downwards, to check for step size
        if (Physics.Raycast(origin, -Vector3.up, out RaycastHit verticalHit, heightOffset, _stepLayerMask, QueryTriggerInteraction.Ignore))
        {
            //Debug.DrawRay(verticalHit.point, verticalHit.normal * 2.0f, Color.white);

            float stepSize = verticalHit.point.y - transform.position.y;

            //return if step size is negative or too large
            if (stepSize < 0.0f || stepSize > _stepHeight) return;

            //float absDot = MathF.Abs(Vector3.Dot(verticalHit.normal, Vector3.up));
            // Checking if aligned enough with up vector, this is to prevent weird/accidental hits from making us step when we shouldn't be able to
            if (Vector3.Dot(verticalHit.normal, Vector3.up) < 0.5f) return;

            //Are we already stepping?
            if (!IsStepping)
            {
                //This will set the step timer to step duration
                IsStepping = true;
                _stepCooldownTimer = _stepCooldown;
            }

            //Calculate step force from bounds
            float stepForce = Mathf.Lerp(_stepForceBounds.x, _stepForceBounds.y, stepSize / _stepHeight);

            //Finally add step force + set timer
            RB.AddForce(transform.up * stepForce, _stepForceMode);

            //Debug.Log("Stepping for: " + stepSize + ", with force: " + stepForce);
        }
        else
        {
            //Debug.DrawRay(origin, -Vector3.up * heightOffset, Color.magenta);
        }
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGrounded(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGrounded(collision);
    }

    private void UpdateGrounded(Collision collision)
    {
        //Debug.DrawRay(transform.position + new Vector3(0.0f, _stepHeight, 0.0f), DesiredMovement.normalized * (MainCollider.bounds.size.z + 0.1f));
        //can we walk on this layer?
        if (_ignoreForGrounded.Contains(collision.gameObject.layer)) return;

        bool validGround = false;

        GroundedSurfaceNormal = Vector3.zero;
        int validContacts = 0;

        foreach (ContactPoint contact in collision.contacts)
        {
            //calculate angle between up and contact normal
            float angle = Vector3.Angle(Vector3.up, contact.normal);

            //if angle is bigger than max grounded angle continue
            if (angle > _groundedAngle) continue;

            GroundedSurfaceNormal += contact.normal;
            validContacts++;
            validGround = true;
            if (_groundedObjects.Contains(collision.collider)) continue;

            //at this point we are grounded on collision.gameobject
            _groundedObjects.Add(collision.collider);

            CheckGrounded(collision);
        }

        //Calc combined surface normal
        if (validContacts > 0) GroundedSurfaceNormal /= validContacts;

        //In case we are touching a object still, and we used to be grounded on it but no longer, remove it and check grounded 
        if (!validGround && _groundedObjects.Contains(collision.collider))
        {
            _groundedObjects.Remove(collision.collider);
            CheckGrounded(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!_groundedObjects.Contains(collision.collider)) return;

        _groundedObjects.Remove(collision.collider);
        CheckGrounded(collision);
    }
}

public enum MoveType { ground, air } //water?
public enum MoveID { basic, falling, flying }

[System.Serializable]
public sealed class MoveData
{
    public MoveType MoveType = 0;
    public MoveID MoveID = 0;

    public float maxSpeed = 5.0f;
    public float acceleration = 40.0f;
    public float deceleration = 60.0f;
    public float gravity = -10.0f;
    public bool canMoveVertical = false;
    public bool useSpeedBounds = true;
    public Vector2 speedBounds = Vector2.one; //Bounds for max speed based on desired movement angle to forward

    [Header("MaxSpeedBuffer")]
    [Tooltip("Allowed amount we can exceed the max speed when running straight (enough) after x amount of seconds")] public float maxSpeedBuffer = 1.0f;
    [Tooltip("The acceleration speed for increasing the max speed buffer")] public float maxSpeedBufferAcceleration = 5.0f;
    [Tooltip("The deceleration of the buffer")] public float maxSpeedBufferDeceleration = 5.0f;
    [Tooltip("The minimum duration after which we start increasing max speed with the buffer")] public float maxSpeedBufferMinTime = 1.0f;
    [Tooltip("The required minimum precision to maintain the max speed buffer (1 is perfect, 0 is 90 degree diff)"), Range(0, 0.99f)] public float maxSpeedBufferAccuracy = 0.9f;

}

[System.Serializable]
public sealed class MovementModifier : INetworkSerializable
{
    [Tooltip("Only set when non multipliers used and there could be overlapping")] public int priority = 0;
    public float duration = 1.0f;
    [Space]
    public float maxSpeedMultiplier = 0.5f;
    public float accelerationMultiplier = 0.5f;
    public float decelerationMultiplier = 1.0f;
    public float rotationMultiplier = 1.0f;

    public float gravityMultiplier = 1.0f;
    [Space]
    public bool overrideAnglePauseMove = false;
    public float anglePauseMove = 0.0f;

    public float elapsed = 0.0f;
    public GameObject Source { get; set; }

    // Option to remove on source null?

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref priority);
        serializer.SerializeValue(ref duration);
        serializer.SerializeValue(ref maxSpeedMultiplier);
        serializer.SerializeValue(ref accelerationMultiplier);
        serializer.SerializeValue(ref decelerationMultiplier);
        serializer.SerializeValue(ref rotationMultiplier);
        serializer.SerializeValue(ref gravityMultiplier);
        serializer.SerializeValue(ref overrideAnglePauseMove);
        serializer.SerializeValue(ref anglePauseMove);
        serializer.SerializeValue(ref elapsed);

        // For Source, we'll serialize its NetworkObjectId.
        // If Source is null, we send 0.
        ulong sourceNetworkId = 0;
        if (serializer.IsWriter)
        {
            if (Source != null)
            {
                var netObj = Source.GetComponent<NetworkObject>();
                if (netObj != null)
                    sourceNetworkId = netObj.NetworkObjectId;
            }
            serializer.SerializeValue(ref sourceNetworkId);
        }
        else
        {
            serializer.SerializeValue(ref sourceNetworkId);
            // Optionally, you can resolve the source on the reader side using:
            // if (sourceNetworkId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(sourceNetworkId, out var netObj))
            //     Source = netObj.gameObject;
        }
    }

    public MovementModifier Copy()
    {
        MovementModifier modifier = new MovementModifier();

        modifier.priority = priority;
        modifier.duration = duration;

        modifier.maxSpeedMultiplier = maxSpeedMultiplier;
        modifier.accelerationMultiplier = accelerationMultiplier;
        modifier.decelerationMultiplier = decelerationMultiplier;
        modifier.rotationMultiplier = rotationMultiplier;
        modifier.gravityMultiplier = gravityMultiplier;

        modifier.overrideAnglePauseMove = overrideAnglePauseMove;
        modifier.anglePauseMove = anglePauseMove;

        modifier.elapsed = 0.0f;
        modifier.Source = Source;

        return modifier;
    }
}