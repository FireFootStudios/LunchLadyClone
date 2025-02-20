using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Wander : FSMState
{
    [SerializeField] private RandomisationSeed _randomSeed = null;
    [SerializeField] private Vector2 _wanderSterpDurationBounds = new Vector2(5.0f, 15.0f);
    [Space]
    [SerializeField] private MovementModifier _mod = null;

    private bool _inStep = false;
    private Character _char = null;

    private List<Transform> _wanderPoints = new List<Transform>();


    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _mod.Source = this.gameObject;
    }

    private void OnEnable()
    {
        _inStep = false;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        // Get wander points from scene data
        _wanderPoints = GameManager.Instance.SceneData.WanderPoints;

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
        if (!_inStep /*&& !(_char.Movement.CurrentMoveSpeed > 0.0f) && !(_char.Movement.CurrentRotationSpeed > 0.0f)*/)
        {
            StopAllCoroutines();
            StartCoroutine(WanderStep());
        }
    }

    public override void OnExit()
    {
        StopAllCoroutines();
        
        _inStep = false;

        _char.Movement.Stop();

        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }

    private IEnumerator WanderStep()
    {
        if(_wanderPoints.Count == 0) yield break;
        _inStep = true;

        Transform wanderT = _wanderPoints.RandomElement();
        _char.Movement.MoveToPos(wanderT.position);

        float duration = Utils.GetRandomFromBounds(_wanderSterpDurationBounds);

        // Wait for duration OR until the navmesh agent is no longer moving (dest reached?)
        float elapsed = 0f;
        bool destReached = false;
        while (elapsed < duration && !destReached)
        {
            destReached = _char.Movement.DestinationReached();
            elapsed += Time.deltaTime;
            yield return null;
        }

        _char.Movement.Stop();

        _inStep = false;
        yield return null;
    }
}