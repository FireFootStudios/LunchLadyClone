using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PatrolTraverser : MonoBehaviour
{
    #region Fields
    [SerializeField, Tooltip("Invoke movement output every x seconds")] private float _movementUpdateInterval = 0.2f;
    [SerializeField, Tooltip("Radius of the trigger that will be created to check if we have reached a point")] private float _triggerRadius = 0.05f;
    [SerializeField, Tooltip("How much seconds should bezier curves be sampled in future, best to be > 0")] private float _bezierSampleOffset = 1.0f;
    [SerializeField, Tooltip("Estimated speed (max speed of movement), used to sample bezier")] private float _bezierEstimatedSpeed = 5.0f;

    [Header("Traversing")]
    [SerializeField, Tooltip("If bigger than zero, the maximum amount of times the platform is allowed to traverse a path (count incremented on any path finish)")] private int _maxTraverse = 0;
    [SerializeField, Tooltip("Parent index change for paths on awake (-1 is null, 0 is this), use to not have paths follow this object")] private int _parentChangeOnAwake = 1;
    [SerializeField] private PatrolPath _autoStartPath = null;
    [SerializeField] private List<TraverseEvent> _traverseEvents = new List<TraverseEvent>();

    [Space]
    [SerializeField, Tooltip("Played when output != vector3.zero")] private SoundSpawnData _onPathSFX = null;
    [SerializeField, Tooltip("Played on a delay")] private SoundSpawnData _onStopSFX = null;
    [SerializeField, Tooltip("Played on a path finish")] private SoundSpawnData _onFinishSFX = null;
    

    //[Header("How much time after having reached the estimated time to the next point we are considered stuck."), SerializeField] private float _detectStuckOffset = 1.0f;

    private PatrolPath _currentPath = null;
    private PatrolPoint _currentPP = null;
    private PatrolPoint _prevPP = null;
    private int _currentPPIndex = 0;
    private float _timeReachNext = 0.0f;//estimated time for reaching point, calculated once when new point is set
    private float _timeReachNextElapsed = 0.0f;
    private float _movementUpdateElapsed = 0.0f;

    private const string _setNextPointStr = "SetNextPoint";
    private const string _patrolLayerStr = "Patrol";

    private const float _dotAlignmentValueCheck = 0.6f;

    //this is cached while overlapping so to prevent issues of not being able to trigger with a point in unique situations (only 1 is cached so we assume no points share the same space, bad assumption, i know...)
    private PatrolPoint _currentOverlapping = null;
    private bool _lastPoint = false;

    private SphereCollider _patrolTrigger = null;

    private Sound _pathingSound = null;
    #endregion

    #region DataTypes

    [System.Serializable]
    private class TraverseEvent
    {
        public PatrolPath path = null;
        public List<PhysicsEvents> physicsEvents = new List<PhysicsEvents>();
        public TraversEventMode mode = 0;
        [Space]
        [Tooltip("Can this event be fired when already travesing?")] public bool allowWhenTraversing = false;
        [Tooltip("Max allowed trigger count (ignored when <= 0)")] public int maxTriggers = 0;

        public int TriggerCount { get; set; }

    }

    [System.Flags]
    private enum TraversEventMode
    {
        onEnter = 2,
        onExit = 4,
        onStay = 8,
    }

    #endregion

    public bool PathFinished { get; private set; }
    public bool PointReached { get; private set; }
    public bool Reversing { get; private set; }
    public bool Traversing { get { return _currentPath != null && !PathFinished && _currentPP != null; } }
    public bool ShouldBeMoving { get { return Traversing && !PointReached; } }
    public bool IsStopped {  get; private set; } //TODO: (if stop when a invoke call was made, pathing will resume on invoked, fix this!)
    public int TraversedCount {  get; private set; }

    public PatrolPath CurrentPath { get { return _currentPath; } }
    public PatrolPoint CurrentPP { get { return _currentPP; } }

    public Action<Vector3> OnOutputDelta; // Vector3 -> desired change (dir to next point)
    public Action<Vector3, Vector3> OnMoveChange; // Position + forward of target point

    public Action OnFinish;
    public Action OnStopDelay;
    public Action OnNextPointSet;

    // Setting path to null is same as stopping
    public void SetPath(PatrolPath path, bool begin = false)
    {
        _currentPath = path;

        ResetPathing();

        if (_currentPath)
        {
            //shuffle points?
            if (_currentPath.Randomise) _currentPath.Points.Shuffle();

            //Begin auto?
            if (begin) Begin();
        }
        else Stop();
    }

    public void Begin(bool reverse = false)
    {
        //Can we still traverse?
        if (_maxTraverse > 0 && TraversedCount >= _maxTraverse) return;

        IsStopped = false;
        Reversing = reverse;
        SetNextPoint();

        EvaluatePathingSound();
    }


    //Stops the traverser and can be resumed by calling begin again (not tested)
    public void Stop()
    {
        IsStopped = true;
        OnOutputDelta?.Invoke(Vector3.zero);
        OnMoveChange?.Invoke(Vector3.zero, Vector3.zero);
    }

    //Resets traverser back to initial state (as if never used :O)
    public void FullReset()
    {
        ResetPathing();

        _currentPath = null;
        TraversedCount = 0;

        foreach(TraverseEvent tEvent in _traverseEvents)
            tEvent.TriggerCount = 0;

        if (_autoStartPath) SetPath(_autoStartPath, true);
    }

    //Resets pathing (keeps set path tho)
    public void ResetPathing()
    {
        _currentPP = null;
        _prevPP = null;
        _currentPPIndex = 0;
        _timeReachNextElapsed = 0.0f;
        _movementUpdateElapsed = 0.0f;
        PathFinished = false;
        PointReached = false;
        Reversing = false;
        _lastPoint = false;
        IsStopped = false;

        EvaluatePathingSound();

        CancelInvoke();
    }

    private void Start()
    {
        //create new gameobject as child for sphere trigger
        GameObject triggerGo = new GameObject("PatrolTrigger");
        triggerGo.transform.parent = this.transform;
        triggerGo.transform.localPosition = Vector3.zero;

        //create sphere collider
        _patrolTrigger = triggerGo.AddComponent<SphereCollider>();
        _patrolTrigger.isTrigger = true;
        _patrolTrigger.radius = _triggerRadius;

        //get patrol layer and set to new GO
        int patrolLayer = LayerMask.NameToLayer(_patrolLayerStr);
        triggerGo.layer = patrolLayer;

        EvaluatePathParent(_autoStartPath);
        if (_autoStartPath) SetPath(_autoStartPath, true);

        InitTraverseEvents();
    }

    private void InitTraverseEvents()
    {
        foreach (TraverseEvent traverseEvent in _traverseEvents)
        {
            foreach (PhysicsEvents pe in traverseEvent.physicsEvents)
            {
                //Just bec we sub to them doesnt mean they will/can ever be called
                if (traverseEvent.mode.HasFlag(TraversEventMode.onEnter)) pe.OnEnter += (_) => ExecuteTraverseEvent(traverseEvent);
                if (traverseEvent.mode.HasFlag(TraversEventMode.onExit)) pe.OnExit += (_) => ExecuteTraverseEvent(traverseEvent);
                if (traverseEvent.mode.HasFlag(TraversEventMode.onStay)) pe.OnStay += (_) => ExecuteTraverseEvent(traverseEvent);
            }

            EvaluatePathParent(traverseEvent.path);
        }
    }

    private void EvaluatePathParent(PatrolPath path)
    {
        if (!path || _parentChangeOnAwake == 0) return;

        Transform parent = null;
        if (_parentChangeOnAwake > 0) parent = Utils.GetUpperTransform(path.transform.parent, _parentChangeOnAwake);

        path.transform.parent = parent;
    }

    private void ExecuteTraverseEvent(TraverseEvent traverseEvent)
    {
        if (Traversing && !traverseEvent.allowWhenTraversing) return;
        if (traverseEvent.maxTriggers > 0 && traverseEvent.TriggerCount >= traverseEvent.maxTriggers) return;

        traverseEvent.TriggerCount++;

        SetPath(traverseEvent.path, true);
    }

    private void Update()
    {
        if (PointReached || PathFinished) return;
        if (IsStopped) return;

        //update elapsed 
        _timeReachNextElapsed += Time.deltaTime;

        //UpdateDetectStuck();
        CheckOverlapping();
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (!_currentPath || _currentPP == null) return;
        if (PointReached || PathFinished) return;
        if (IsStopped) return;

        //update movement by interval
        _movementUpdateElapsed += Time.deltaTime;
        if (_movementUpdateElapsed < _movementUpdateInterval) return;
        _movementUpdateElapsed -= _movementUpdateInterval;

        //set movement (check if bezier or first point)
        if (_currentPP.controlT == null || _prevPP == null)
        {
            OnOutputDelta?.Invoke(_currentPP.transform.position - transform.position);
        }
        else
        {
            //evaluate bezier, always sample towards next frame
            Vector3 newPos = Utils.EvaluateQuadraticCurve(_prevPP.transform.position, _currentPP.transform.position,
                (_timeReachNextElapsed + _bezierSampleOffset) / _timeReachNext, _currentPP.controlT.position);
            Vector3 desiredMovement = (newPos - transform.position).normalized;
            OnOutputDelta?.Invoke(desiredMovement);
        }
    }

    private void CheckOverlapping()
    {
        if (PointReached || PathFinished) return;
        if (_currentPP == null || !_patrolTrigger) return;
        //if (_currentPP == null || _currentPP != _currentOverlapping) return;

        // Very expensive and is only here cuz I dont really care anymore!
        if (Utils.AreSpheresOverlapping(_currentPP.sphereTrigger, _patrolTrigger)) 
            OnPointReached();
    }

    //private void UpdateDetectStuck()
    //{
    //    if (_movement.AdjustedDesiredMovement == Vector3.zero || _currentPP == null || PathFinished || PointReached) return;

    //    if (_timeReachNextElapsed < _timeReachNext + _detectStuckOffset) return;

    //    //set to current pp
    //    _movement.transform.position = _currentPP.transform.position;
    //}

    private void SetNextPoint()
    {
        if (!_currentPath || _currentPath.Points.Count == 0 || PathFinished) return;

        //get start point or next point
        if (_currentPP == null) _currentPP = GetStartPoint();
        else
        {
            // Calculate next index and set current patrol point
            _currentPPIndex = _currentPath.Points.IndexOf(_currentPP);
            _currentPPIndex += Reversing ? -1 : 1;

            // Cache if last point
            if (!_currentPath.Loop && !_currentPath.ReverseLoop &&
                (_currentPPIndex >= _currentPath.Points.Count - 1 ||
                _currentPPIndex <= 0)) _lastPoint = true;

            // Check for loop or end of path
            if (_currentPPIndex >= _currentPath.Points.Count || _currentPPIndex < 0)
            {
                if (_currentPath.Loop) _currentPPIndex = 0;
                else if (_currentPath.ReverseLoop)
                {
                    Reversing = !Reversing;
                    _currentPPIndex = Reversing ? _currentPath.Points.Count - 1 : 1;
                }
                else
                {
                    PathFinished = true;
                    OnOutputDelta?.Invoke(Vector3.zero);
                    OnMoveChange?.Invoke(Vector3.zero, Vector3.zero);
                    OnFinish?.Invoke();

                    TraversedCount++;

                    SoundManager.Instance.PlaySound(_onFinishSFX);
                    return;
                }
            }
            _prevPP = _currentPP;
            _currentPP = _currentPath.Points[_currentPPIndex];
        }

        //Calculate time it would take to reach next point (estimation, ai might be blocked or have acceleration/deceleration and stuff)
        float distanceToNextPoint = (_currentPP.transform.position - transform.position).magnitude;
        _timeReachNext = distanceToNextPoint / _bezierEstimatedSpeed;
        _timeReachNextElapsed = 0.0f;

        //update initial movement and set point reached false
        PointReached = false;
        IsStopped = false;
        UpdateMovement();
        OnMoveChange?.Invoke(_currentPP.transform.position, _currentPP.transform.forward);

        OnNextPointSet?.Invoke();
    }

    private PatrolPoint GetStartPoint()
    {
        // If not looping, start with first or last point (depending if reversed)
        // TODO can make this option instead, so we can start from the closest point even if n
        //if (!_currentPath.Loop)
        //{
        //    if (!Reversing) return _currentPath.Points[0];
        //    else return _currentPath.Points[_currentPath.Points.Count - 1];
        //}

        //find closest patrol point
        PatrolPoint closest = null;
        int closestIndex = -1;
        float closestSqrDist = Mathf.Infinity;
        for (int i = 0; i < _currentPath.Points.Count; i++)
        {
            PatrolPoint pp = _currentPath.Points[i];

            float distanceSqr = Vector3.SqrMagnitude(pp.transform.position - transform.position);
            if (distanceSqr > closestSqrDist) continue;

            closestSqrDist = distanceSqr;
            closest = pp;
            closestIndex = i;
        }


        //Instead of taking the closest point, if it is not the last point in the path, take the next one instead to assure we never move back
        //only return next point if dot between dir to closest and to next are NOT aligned (meaning we are in between the 2 points)

        PatrolPoint nextPp = NextPoint(closestIndex);
        if(nextPp != null )
        {
            Vector3 dirClosest = (closest.transform.position - transform.position).normalized;
            Vector3 dirNext = (nextPp.transform.position - transform.position).normalized;

            //this ensures that we only move to the next point if closest is the opposite way
            if (Vector3.Dot(dirClosest, dirNext) < _dotAlignmentValueCheck) closest = nextPp;
        }

        return closest;
    }

    // returns the next point in current path based on current state if any
    private PatrolPoint NextPoint(int from)
    {
        if (!_currentPath || _currentPath.Points.Count < 2) return null;

        int next = from + (Reversing ? -1 : 1);

        if (next >= _currentPath.Points.Count || next < 0)
        {
            if (_currentPath.Loop) next = 0;
            else if (_currentPath.ReverseLoop) next = Reversing ? _currentPath.Points.Count - 1 : 1;
            else return null;
        }

        return _currentPath.Points[next];
    }

    private void OnPointReached()
    {
        PointReached = true;

        //Point reached, set next after delay
        if (_currentPath.Delay > 0.0f && !_lastPoint)
        {
            OnStopDelay?.Invoke();
            Stop();

            Invoke(_setNextPointStr, _currentPath.Delay);

            //stop move sound
            SoundManager.Instance.PlaySound(_onStopSFX);
        }
        else
        {
            SetNextPoint();
        }

        EvaluatePathingSound();

        //set pos to avoid inaccuracy towards next point (results in jittery movement...)
        //transform.position = _currentPP.transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_currentPP == null || other.gameObject != _currentPP.transform.gameObject/* || PointReached*/) return;

        //cache current overlapping -> CALL FIRST
        _currentOverlapping = _currentPP;

        OnPointReached();
    }

    private void OnTriggerExit(Collider other)
    {
        //remove current overlapping
        if (_currentOverlapping == null || _currentOverlapping.transform.gameObject == other.gameObject)
            _currentOverlapping = null;
    }

    private void EvaluatePathingSound()
    {
        //Start move if not yet active, or reset if non loop
        if (ShouldBeMoving && (!_pathingSound || !_pathingSound.Origin == this.gameObject || _pathingSound.IsFinished || !_pathingSound.AudioSource.loop))
        {
            if (_pathingSound) _pathingSound.Stop();
            _pathingSound = SoundManager.Instance.PlaySound(_onPathSFX, this.gameObject);
        }
        //Cancel path sound if still active
        else if (!ShouldBeMoving && _pathingSound && !_pathingSound.IsFinished && _pathingSound.Origin == this.gameObject)
        {
            _pathingSound.Stop();
        }
    }
}