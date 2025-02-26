using UnityEngine;

public sealed class Aggro : FSMState
{
    [SerializeField] private bool _allowMove = true;
    [SerializeField] private bool _useLastValidPos = true;
    [SerializeField] private float _useLastValidPosTime = 1.0f; // if above this time, we use last valid (seen) pos of target instead
    [SerializeField, Tooltip("Time after entering the aggro state before the AI will move")] private Vector2 _aggroDelayBounds = new Vector2(0.5f, 2.0f);
    [Space]
    [SerializeField] private MovementModifier _mod = null;

    [Space]
    [SerializeField, Tooltip("If there is no preffered ability, stop move? (for example due to cooldowns)")] private bool _pauseMoveNoAbility = true;

    [Space]
    [SerializeField] private bool _tryPredictMovement = false;
    [SerializeField] private float _predictAmountMultiplier = 0.75f;

    private float _aggroElapsed = 0.0f;
    private float _currentDelay = 0.0f;
    private bool _wasAggroBefore = false;

    private Character _char = null;


    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _mod.Source = this.gameObject;
    }

    private void Start()
    {
        _char.Spawner.OnRespawn += OnRespawn;
    }

    private void OnRespawn()
    {
        _wasAggroBefore = false;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        // Random delay
        _currentDelay = Utils.GetRandomFromBounds(_aggroDelayBounds);

        // Reset timer only when aggro for first time
        if (!_wasAggroBefore)
        {
            _aggroElapsed = 0.0f;
            _wasAggroBefore = true;
        }

        if (!_allowMove) _char.Movement.Stop();

        _char.Movement.CanRotate = true;

        // Add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);
    }

    public override void OnExit()
    {
        // Reset was aggro before if we are leaving state due to not being aggro anymore
        _wasAggroBefore = _char.Behaviour.HasAggroTarget;

        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }

    private void Update()
    {
        if (!_char) return;

        UpdateMovement();
    }

    private void UpdateMovement()
    {
        // Update timer (return if smaller than delay)
        _aggroElapsed += Time.deltaTime;

        // Get target pair
        TargetPair targetP = _char.Behaviour.AggroTargetSystem.GetFirstTarget();
        if (targetP == null)
        {
            _char.Movement.Stop();
            return;
        }

        Vector3 targetPos = targetP.target.transform.position;

        // Use the last position when the target was valid (ie in range or fov, ...)
        if (_useLastValidPos && targetP.lifeElapsed > _useLastValidPosTime) 
            targetPos = targetP.lastValidPos;

        // Pos prediction (this works pretty horribly for now)
        if (_tryPredictMovement && targetP.target.TryGetComponent(out FreeMovement movement))
        {
            targetPos = Utils.PredictPosition(targetPos, transform.position, movement.CurrentMoveVelocity, _char.Movement.CurrentMoveSpeed * _predictAmountMultiplier);
        }

        // Return if delayed (update rot only)
        if (_aggroElapsed < _currentDelay)
        {
            _char.Movement.DesiredForward = targetPos - transform.position;
            return;
        }

        // Pause move when no ability can be used? If abilities are disabled or none could be used due to CD (update rot only)
        if (_pauseMoveNoAbility && _char.AttackBehaviour && (!_char.AttackBehaviour.CouldUseAbilityIfInRange() || _char.AbilityManager.DisableTimer > 0.0f))
        {
            _char.Movement.DesiredForward = targetPos - transform.position;
            _char.Movement.Stop();
            return;
        }

        // Update movement
        if (_allowMove) _char.Movement.MoveToPos(targetPos);
    }
}