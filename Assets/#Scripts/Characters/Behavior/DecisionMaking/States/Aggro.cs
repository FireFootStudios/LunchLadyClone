using System.Collections;
using UnityEngine;

public sealed class Aggro : FSMState
{
    [SerializeField, Tooltip("Time after entering the aggro state before the AI will move")] private Vector2 _aggroDelayBounds = new Vector2(0.5f, 2.0f);
    [SerializeField] private bool _allowMove = true;
    [SerializeField, Tooltip("Min time for aggro the same target")] private float _minTargetAggroTime = 10.0f;
    [SerializeField] private float _updatePathingInterval = .5f;
    [Space]
    [SerializeField] private bool _useLastValidPos = true;
    [SerializeField] private float _useLastValidPosTime = 1.0f; // if above this time, we use last valid (seen) pos of target instead
    [Space]
    [SerializeField] private MovementModifier _mod = null;

    [Space]
    [SerializeField, Tooltip("If there is no preffered ability, stop move? (for example due to cooldowns)")] private bool _pauseMoveNoAbility = true;

    [Space]
    [SerializeField] private bool _tryPredictMovement = false;
    [SerializeField] private float _predictAmountMultiplier = 0.75f;

    private Character _char = null;
    private TargetPair _currentAggroTp = null;

    private float _currentDelay = 0.0f;
    private float _currentTargetAggroElapsed = 0.0f;
    private float _aggroElapsed = 0.0f;
    private bool _pathingStarted = false;


    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _mod.Source = this.gameObject;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        // Random delay
        _currentDelay = Utils.GetRandomFromBounds(_aggroDelayBounds);
        _aggroElapsed = 0.0f;

        if (!_allowMove) _char.Movement.Stop();
        _char.Movement.CanRotate = true;

        // Add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);

        _currentAggroTp = null;
        StopAllCoroutines();
    }

    public override void OnExit()
    {
        // Remove mod
        _char.Movement.RemoveMod(_mod);

        _currentAggroTp = null;
        StopAllCoroutines();
    }

    private void Update()
    {
        UpdateCurrentTarget();
        UpdateDestReach();
    }

    private void UpdateCurrentTarget()
    {
        if (!_char) return;

        // Check if current target and is still valid
        bool hasValidTarget = _currentAggroTp != null ? _char.Behaviour.AggroTargetSystem.HasSpecificTarget(_currentAggroTp.target) : false;
        bool hasBetterTarget = false;

        // If valid current target, return as long as we are not allowed to change targets
        if (hasValidTarget)
        {
            _currentTargetAggroElapsed += Time.deltaTime;

            // Check if we have better target
            if (_currentTargetAggroElapsed > _minTargetAggroTime)
            {
                // TODO
                // This would only be useful if we could say include the distance to targets as effectiveness, cuz this is only reason why we might want to change to a new player
                // Or just for the sake of not chasing the same guy if another is near
                // We could make this an option, but for now we dont need to switch if current target is still valid
            }
        }

        // Return if current target is still valid and we do not have a better target
        if (hasValidTarget && !hasBetterTarget) return;

        // Get best target
        TargetPair targetPair = _char.Behaviour.AggroTargetSystem.GetFirstTarget();

        // If no valid target anymore or target is same as current, return
        if ((targetPair == null || !targetPair.target) || (hasValidTarget && _currentAggroTp.target == targetPair.target))
        {
            _char.Movement.Stop();
            return;
        }

        _currentAggroTp = null;
        StopAllCoroutines();
        StartCoroutine(ChaseTargetCo(targetPair));
    }

    private void UpdateDestReach()
    {
        if (_currentAggroTp == null || !_currentAggroTp.target) return;
        if (!_pathingStarted) return;

        if (!_char.Movement.DestinationReached()) return;

        if (_currentAggroTp.lifeElapsed < .5f)
            return;
            
        _char.Behaviour.AggroTargetSystem.RemoveTarget(_currentAggroTp.target, false);
        _currentAggroTp = null;
        StopAllCoroutines();
        return;
    }

    //private void UpdateMovement()
    //{
    //    // Update timer (return if smaller than delay)
    //    _aggroElapsed += Time.deltaTime;

    //    // Get target pair
    //    TargetPair targetP = _char.Behaviour.AggroTargetSystem.GetFirstTarget();
    //    if (targetP == null)
    //    {
    //        _char.Movement.Stop();
    //        return;
    //    }

    //    Vector3 targetPos = targetP.target.transform.position;

    //    // Use the last position when the target was valid (ie in range or fov, ...)
    //    bool trackingTarget = false;
    //    if (_useLastValidPos && targetP.lifeElapsed > _useLastValidPosTime)
    //    {
    //        targetPos = targetP.lastValidPos;
    //        trackingTarget = true;
    //    }

    //    // Pos prediction (this works pretty horribly for now)
    //    if (_tryPredictMovement && targetP.target.TryGetComponent(out FreeMovement movement))
    //    {
    //        targetPos = Utils.PredictPosition(targetPos, transform.position, movement.CurrentMoveVelocity, _char.Movement.CurrentMoveSpeed * _predictAmountMultiplier);
    //    }

    //    // Return if delayed (update rot only)
    //    if (_aggroElapsed < _currentDelay)
    //    {
    //        _char.Movement.DesiredForward = targetPos - transform.position;
    //        return;
    //    }

    //    // Pause move when no ability can be used? If abilities are disabled or none could be used due to CD (update rot only)
    //    if (_pauseMoveNoAbility && _char.AttackBehaviour && (!_char.AttackBehaviour.CouldUseAbilityIfInRange() || _char.AbilityManager.DisableTimer > 0.0f))
    //    {
    //        _char.Movement.DesiredForward = targetPos - transform.position;
    //        _char.Movement.Stop();
    //        return;
    //    }

    //    // Update movement
    //    if (_allowMove)
    //    {
    //        if (trackingTarget && _targetTrackStarted && _char.Movement.DestinationReached())
    //        {
    //            // Remove target since we reached the last 'seen' position
    //            _char.Behaviour.AggroTargetSystem.RemoveOverrideTarget(targetP.target);
    //            return;
    //        }

    //        _char.Movement.MoveToPos(targetPos);
    //        _targetTrackStarted = true;
    //        // If we are not tracking a moving/live target, only set when stopped and not already there!
    //        //if (trackingTarget && _char.Movement.DestinationReached()) _char.Movement.MoveToPos(targetPos);
    //        //else if (!trackingTarget)
    //        //{
    //        //    _char.Movement.MoveToPos(targetPos);
    //        //}
    //    }
    //}


    private IEnumerator ChaseTargetCo(TargetPair newTarget)
    {
        // Init
        _currentAggroTp = newTarget;
        _pathingStarted = false;

        // Set rotation
        _char.Movement.DesiredForward = _currentAggroTp.target.transform.position - transform.position;

        // Delay
        if (_currentDelay > 0.0f)
            yield return new WaitForSeconds(_currentDelay);

        while (_currentAggroTp != null && _currentAggroTp.target)
        {
            Vector3 targetPos = _currentAggroTp.target.transform.position;

            // Use the last position when the target was valid (ie in range or fov, ...)
            // bool trackingTarget = false;
            if (_useLastValidPos && _currentAggroTp.lifeElapsed > _useLastValidPosTime)
            {
                targetPos = _currentAggroTp.lastValidPos;
                //trackingTarget = true;
            }

            // Pos prediction (this works pretty horribly for now)
            if (_tryPredictMovement && _currentAggroTp.target.TryGetComponent(out FreeMovement movement))
            {
                targetPos = Utils.PredictPosition(targetPos, transform.position, movement.CurrentMoveVelocity, _char.Movement.CurrentMoveSpeed * _predictAmountMultiplier);
            }

            // Pause move when no ability can be used? If abilities are disabled or none could be used due to CD (update rot only)
            if (_pauseMoveNoAbility && _char.AttackBehaviour && (!_char.AttackBehaviour.CouldUseAbilityIfInRange() || _char.AbilityManager.DisableTimer > 0.0f))
            {
                _char.Movement.DesiredForward = targetPos - transform.position;
                _char.Movement.Stop();
                yield return null;
            }

            // Update movement
            if (_allowMove)
            {
                _char.Movement.MoveToPos(targetPos);
                _pathingStarted = true;
            }

            yield return new WaitForSeconds(_updatePathingInterval);
        }

        _currentAggroTp = null;
        yield return null;
    }
}