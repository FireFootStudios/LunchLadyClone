using UnityEngine;

public sealed class MoveToSpawn : FSMState
{
    [SerializeField] private RandomisationSeed _randomSeed = null;
    [Space]
    [SerializeField] private MovementModifier _mod = null;
    [SerializeField, Tooltip("Do we run to spawn pos? Only used if this position is inside the spawnzone (if any)")] private bool _runToSpawnPos = true;

    [Header("Detect Stuck"), SerializeField] private float _detectStuckInterval = 2.0f;
    [SerializeField] private float _stuckDistanceThreshold = 1.0f;
    [SerializeField] private int _maxFixAttempts = 3;

    private float _detectStuckTimer = 0.0f;
    private float _lastCheckedDistance = 0.0f;
    private int _fixAttempts = 0;

    private Character _char = null;

    private Vector3 _targetPos = Vector3.zero;


    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _mod.Source = this.gameObject;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        _fixAttempts = 0;

        CalculateTargetPos(false);
        UpdateMovement();

        // Add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);
    }

    private void Update()
    {
        //UpdateMovement();
        UpdateDetectStuck();
    }

    private void UpdateMovement()
    {
        if (!_char) return;

        _char.Movement.MoveToPos(_targetPos);
    }

    private void CalculateTargetPos(bool forceRandomise)
    {
        // If forced, or desired, or spawn pos is not
        if (_char.Behaviour.HasValidSpawnZone && (!_runToSpawnPos || forceRandomise || 
            !Utils.IsPointInsideAnyColliders(_char.Spawner.SpawnInfo.pos, _char.Behaviour.SpawnZone.Colliders)))
        {
            RandomiseTargetPos();
        }
        else
        {
            _targetPos = _char.Spawner.SpawnInfo.pos;
        }

        // Detect stuck init
        _lastCheckedDistance = (_targetPos - transform.position).magnitude;
        _detectStuckTimer = _detectStuckInterval;
    }

    private void RandomiseTargetPos()
    {
        if (!_char.Behaviour.SpawnZone || _char.Behaviour.SpawnZone.Colliders.Count == 0) return;

        Collider randomCollider = _char.Behaviour.SpawnZone.Colliders.RandomElement(_randomSeed);

        // TODO, figure out how to make sure the y aligns with the ground, right now, depending on the hitbox, we could get a point way up or down
        _targetPos = Utils.RandomPointInCollider(randomCollider, _randomSeed);
    }

    private void UpdateDetectStuck()
    {
        _detectStuckTimer -= Time.deltaTime;
        if (_detectStuckTimer > 0.0f) return;

        // Current distance to spawnpos
        float currentDistance = (_targetPos - transform.position).magnitude;

        // If distance difference is smaller than threshold, set position to spawnpos and return
        if (_lastCheckedDistance - currentDistance < _stuckDistanceThreshold)
        {
            // See if we can change target pos first a couple times (only possible if spawnzone)
            if (_char.Behaviour.HasValidSpawnZone && _fixAttempts < _maxFixAttempts)
            {
                // Force randomise
                CalculateTargetPos(true);
                _fixAttempts++;
            }
            else
            {
                // Kinda has to be spawninfo pos cuz we dont know if target pos is underground or in a wall or smt
                //_char.Movement.RB.MovePosition(_char.Spawner.SpawnInfo.pos);
                _fixAttempts = 0;
            }

            _char.Movement.Stop();
            // TODO, if this happens multiple times in a row, teleport back
        }
        else
        {
            // Update timer and last checked with current distance
            _lastCheckedDistance = currentDistance;
            _detectStuckTimer = _detectStuckInterval;
        }
    }

    public override void OnExit()
    {
        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }
}
