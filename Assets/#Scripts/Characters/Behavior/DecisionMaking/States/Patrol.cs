using System;
using Unity.VisualScripting;
using UnityEngine;

public sealed class Patrol : FSMState
{
    [SerializeField] private PatrolPath _path = null;
    [Space]
    [SerializeField] private MovementModifier _mod = null;

    [Header("Detect Stuck"), SerializeField] private float _detectStuckInterval = 2.0f;
    [SerializeField] private float _stuckDistanceThreshold = 1.0f;

    private float _detectStuckTimer = 0.0f;
    private float _lastCheckedDistance = 0.0f;

    private Character _char = null;
    private PatrolTraverser _traverser = null;


    private void OnValidate()
    {
        _char = GetComponentInParent<Character>();
        if (_char)
        {
            _traverser = _char.GetComponent<PatrolTraverser>();
            if (!_traverser) _traverser = _char.AddComponent<PatrolTraverser>();
        }
    }

    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _traverser = _char.GetComponent<PatrolTraverser>();

        // Set mod source
        _mod.Source = this.gameObject;

        if (_path)
        {
            _traverser.SetPath(_path);

            //_traverser.OnOutputDelta += (Vector3 output) =>
            //{
            //    _char.Movement.
            //    _char.Movement.DesiredMovement = _char.Movement.DesiredForward = output;
            //};

            _traverser.OnMoveChange += OnTraversMoveChange;
        }
        else Debug.Log("Character has patrol state but no assigned path! (" + _char.gameObject.name + ")");
    }

    private void Update()
    {
        UpdateDetectStuck();
    }

    public override void OnEnter()
    {
        if (!_char) return;

        //set can rotate and limit movementToRot
        _char.Movement.CanRotate = true;
        _char.Movement.IsStopped = false;

        //add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);

        _traverser.ResetPathing();
        _traverser.Begin();
    }

    //private void UpdateDetectStuck()
    //{
    //    if (_char.Movement.DesiredMovement == Vector3.zero || CurrentPP == null) return;

    //    _detectStuckTimer -= Time.deltaTime;
    //    if (_detectStuckTimer > 0.0f) return;

    //    //current distance to spawnpos
    //    float currentDistanceSqr = (CurrentPP.transform.transform.position - transform.position).sqrMagnitude;

    //    //if distance difference is smaller than threshold, set position to spawnpos and return
    //    if (_lastCheckedDistanceSqr - currentDistanceSqr < _stuckDistanceThreshold)
    //    {
    //        _char.Movement.transform.position = CurrentPP.transform.transform.position;
    //        return;
    //    }

    //    //update timer and last checked with current distance
    //    _lastCheckedDistanceSqr = currentDistanceSqr;
    //    _detectStuckTimer = _detectStuckInterval;
    //}

    private void UpdateDetectStuck()
    {
        if (_char.Movement.DesiredMovement == Vector3.zero || !_traverser || _traverser.CurrentPP == null) return;

        _detectStuckTimer -= Time.deltaTime;
        if (_detectStuckTimer > 0.0f) return;

        // Current distance to spawnpos
        float currentDistance = (_traverser.CurrentPP.transform.position - transform.position).magnitude;

        // If distance difference is smaller than threshold, set position to spawnpos and return
        if (_lastCheckedDistance - currentDistance < _stuckDistanceThreshold)
        {
            //_char.Movement.RB.MovePosition(_traverser.CurrentPP.transform.position);
            //_char.Movement.DesiredMovement = Vector3.zero;
        }
        else
        {
            // Update timer and last checked with current distance
            _lastCheckedDistance = currentDistance;
            _detectStuckTimer = _detectStuckInterval;
        }
    }

    private void OnTraversMoveChange(Vector3 targetPos, Vector3 targetForward)
    {
        // Detect stuck init
        _lastCheckedDistance = (targetPos - transform.position).magnitude;
        _detectStuckTimer = _detectStuckInterval;
    }

    public override void OnExit()
    {
        _traverser.Stop();

        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }
}