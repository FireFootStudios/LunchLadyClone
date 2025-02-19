using System.Collections;
using UnityEngine;

public sealed class Wander : FSMState
{
    [SerializeField] private RandomisationSeed _randomSeed = null;
    [Space]
    [SerializeField, Tooltip("How often long should a wander step take")] private Vector2 _wanderStepDurationBounds = Vector2.one;
    [SerializeField, Tooltip("How long should the agent pause in between steps")] private Vector2 _delayRange = Vector2.one;
    [SerializeField] private float _maxAngleChangeOnStep = 90.0f;
    [Space]
    [SerializeField] private MovementModifier _mod = null;

    [Space]
    [SerializeField] private bool _useSpawnZone = true;

    
    private bool _isStepping = false;
    private Character _char = null;


    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _mod.Source = this.gameObject;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        StartCoroutine(WanderStep());

        _char.Movement.CanRotate = true;
        _char.Movement.IsStopped = false;

        // Add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);
    }

    private void Update()
    {
        if (!_char) return;

        // Next update (only when stopped moving/rotating)
        if (!_isStepping && !(_char.Movement.CurrentMoveSpeed > 0.0f) && !(_char.Movement.CurrentRotationSpeed > 0.0f))
        {
            StopAllCoroutines();
            StartCoroutine(WanderStep());
        }
    }

    public override void OnExit()
    {
        StopAllCoroutines();
        
        _isStepping = false;

        _char.Movement.Stop();

        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }

    private IEnumerator WanderStep()
    {
        _isStepping = true;

        //// Calculate delay
        //float delay = _randomSeed ? _randomSeed.RandomRange(_delayRange.x, _delayRange.y) : Random.Range(_delayRange.x, _delayRange.y);

        //yield return new WaitForSeconds(delay);

        //// If valid spawnzones and usage enabled move inside them
        //if (_char.Behaviour.HasValidSpawnZone && _useSpawnZone)
        //{
        //    // Move to random point inside of a random collider part of the spawnzone
        //    _char.Movement.MoveToPos(Utils.RandomPointInCollider(_char.Behaviour.SpawnZone.Colliders.RandomElement(), _randomSeed));
        //}
        //else
        //{
        //    // Get random angles
        //    //float randomYAngle = Random.Range(_angleYChangeRange.x, _angleYChangeRange.y);
        //    float randomXAngle = _randomSeed ? _randomSeed.RandomRange(-_maxAngleChangeOnStep, _maxAngleChangeOnStep) : Random.Range(-_maxAngleChangeOnStep, _maxAngleChangeOnStep);

        //    Vector3 desiredForward = Vector3.RotateTowards(transform.forward, -transform.forward, randomXAngle * Mathf.Deg2Rad, 0.0f);

        //    _char.Movement.MoveToPos()
        //    _char.Movement.DesiredForward = desiredForward;
        //}

        //_char.Movement.IsStopped = false;

        ////Calculate time for next step
        //float stepDuration = _randomSeed ? _randomSeed.RandomRange(_wanderStepDurationBounds.x, _wanderStepDurationBounds.y) 
        //    : Random.Range(_wanderStepDurationBounds.x, _wanderStepDurationBounds.y);

        //yield return new WaitForSeconds(stepDuration);

        ////stop movement
        //_char.Movement.DesiredMovement = Vector3.zero;
        //_char.Movement.DesiredForward = _char.transform.forward;

        //_isStepping = false;
        yield return null;
    }
}