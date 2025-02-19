using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Health))]
public class Projectile : MonoBehaviour
{
    #region Fields
    [SerializeField] private ParticleSystem _onHitPsTemplate = null;
    [SerializeField] private ParticleSystem _onKillPsTemplate = null;
    [SerializeField] private List<ParticleSystem> _playWhileAlive = new List<ParticleSystem>();

    [SerializeField] private SoundSpawnData _onHitSFX = null;
    [SerializeField] private SoundSpawnData _onFireSFX = null;
    [SerializeField] private SoundSpawnData _flyingSFX = null;
    [SerializeField] private SoundSpawnData _onKillSFX = null;

    private ProjectileData _data = null;

    private Rigidbody _rigidbody = null;
    private Health _health = null;

    private bool _isInitialized = false;
    private bool _isActive = false;

    private int _hitCount = 0;
    private float _gravity = 0.0f;

    private Vector3 _startPos = Vector3.zero;
    private Vector3 _lastTargetPos = Vector3.zero;
    private Vector3 _initialTargetPos = Vector3.zero;

    //These are set by the creation script (Through the Init method)
    private Executer _executer = null;
    private List<Tag> _targetTags = null;

    private GameObject _initialTarget = null; //Can be null
    private Health _initialTargetHealth = null; //Can be null

    private Sound _flyingSound = null;
    #endregion

    public Rigidbody RB
    {
        get
        {
            if (!_rigidbody) _rigidbody = GetComponent<Rigidbody>();
            return _rigidbody;
        }
    }
    public Health Health
    {
        get
        {
            if (!_health) _health = GetComponent<Health>();
            return _health;
        }
    }
    
    //Read only after fire and if valid target was passed
    public Vector3 InitialTargetPos { get { return _initialTargetPos; } }
    public GameObject InitialTarget { get { return _initialTarget; } }

    public enum Mode { basic, trajectory/*, bezier*/ }

    #region Init
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _health = GetComponent<Health>();
        _health.OnDeath += () =>
        {
            //if still active
            Kill();

            //Stop VFX
            foreach (ParticleSystem ps in _playWhileAlive)
                ps.Stop();
        };
    }

    public void Init(ProjectileData data, Executer executer, List<Tag> targetTags = null)
    {
        if (!executer) return;

        //Destroy previous executer if any
        if (_executer && _executer != executer) Destroy(_executer);

        //Set fields
        _data = data;
        _executer = executer;
        _targetTags = targetTags;

        _isInitialized = true;
    }

    //Resets and fires the projectile
    public void ResetAndFire(GameObject initialTarget = null)
    {
        if (!_isInitialized) return;

        //Cache target and its health comp if any
        _initialTarget = initialTarget;
        _initialTargetHealth = initialTarget ? initialTarget.GetComponent<Health>() : null;

        //Reset values \/\/\/
        _isActive = true;
        _rigidbody.isKinematic = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _startPos = transform.position;
        _gravity = _data.gravity;
        _hitCount = 0;

        //Reset health comp
        _health.Reset();

        //Initial + last target pos
        if (_initialTargetHealth) _initialTargetPos = _initialTargetHealth.FocusPos;
        else if (_initialTarget) _initialTargetPos = _initialTarget.transform.position;
        else _initialTargetPos = Vector3.zero;

        //Set this to current target pos
        _lastTargetPos = _initialTargetPos;

        //Add torque
        _rigidbody.AddTorque(_data.torque.x * transform.right + _data.torque.y * transform.up + _data.torque.z * transform.forward, ForceMode.VelocityChange);

        //Fire (can be overriden for different kind of projectiles)
        Fire();
    }

    protected virtual void Fire()
    {
        switch (_data.mode)
        {
            case Mode.basic:
                FireBasic();
                break;

            case Mode.trajectory:
                FireTrajectoryAngleVariance();
                break;
        }

        //SFX
        SoundManager.Instance.PlaySound(_onFireSFX);
        _flyingSound = SoundManager.Instance.PlaySound(_flyingSFX, this.gameObject);

        //VFX
        foreach (ParticleSystem ps in _playWhileAlive)
            ps.Play();
    }

    private void FireBasic()
    {
        if (_initialTarget)
        {
            //predict movement of target, if enabled and target has movement
            if (_data.predictMovement && _initialTarget.TryGetComponent(out FreeMovement movement))
            {
                _initialTargetPos = Utils.PredictPosition(_initialTargetPos, transform.position,
                    movement.CurrentMoveVelocity, _data.speed);
            }

            Vector3 targetDir = _data.ignoreInitialTargetdir ? transform.forward : (_initialTargetPos - _startPos).normalized;
            targetDir.Normalize();

            if (_data.rotateWithVel) transform.forward = targetDir;
            _rigidbody.AddForce(_data.speed * targetDir, ForceMode.VelocityChange);
        }
        //In case no initial target, just fire in direction of our forward
        else _rigidbody.AddForce(_data.speed * transform.forward, ForceMode.VelocityChange);
    }

    //CHATGPTTTTTTTTTTTTTTTTTTTTT \/\/\/\/\\/\/\/\/\/\/\/\/\\/\/\/\/\/\/\/
    private void FireTrajectory()
    {
        if (_initialTarget)
        {
            float absGravity = Mathf.Abs(_data.gravity);

            float distanceToTarget = Vector3.Distance(_startPos, _initialTargetPos);
            float timeOfFlight = Mathf.Sqrt(2f * distanceToTarget / absGravity);

            // Calculate required initial horizontal velocity
            Vector3 horizontalDirection = (_initialTargetPos - _startPos).normalized;
            Vector3 horizontalVelocity = horizontalDirection * (distanceToTarget / timeOfFlight);

            // Calculate required initial vertical velocity
            float verticalVelocity = 0.5f * absGravity * timeOfFlight;

            // Set the total initial velocity
            Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

            _rigidbody.AddForce(initialVelocity, ForceMode.VelocityChange);
        }
    }

    private void FireTrajectoryAngleVariance()
    {
        if (!_initialTarget) return;

        float absGravity = Mathf.Abs(_data.gravity);

        //Calculate angle from initial rotation
        float angle = Mathf.Clamp((Vector3.Angle(transform.forward, Vector3.down) - 90.0f), 5.0f, 89.0f);

        // Convert angle to radians
        float angleInRadians = angle * Mathf.Deg2Rad;

        // Calculate the distance to the target
        float distanceToTarget = Vector3.Distance(_startPos, _initialTargetPos);

        // Calculate the initial velocity magnitude needed to reach the target
        float initialVelocityMagnitude = Mathf.Sqrt(distanceToTarget * absGravity / Mathf.Sin(2 * angleInRadians));

        // Calculate the initial velocity components
        float verticalVelocity = initialVelocityMagnitude * Mathf.Sin(angleInRadians);
        float horizontalVelocityMagnitude = initialVelocityMagnitude * Mathf.Cos(angleInRadians);

        // Calculate horizontal direction
        Vector3 horizontalDirection = (_initialTargetPos - _startPos).normalized;
        Vector3 horizontalVelocity = horizontalDirection * horizontalVelocityMagnitude;

        // Set the total initial velocity
        Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        // Apply the initial velocity to the rigidbody
        _rigidbody.AddForce(initialVelocity, ForceMode.VelocityChange);
    }

    #endregion

    #region Updates
    private void FixedUpdate()
    {
        //update last target pos
        if (_initialTarget) _lastTargetPos = _initialTargetHealth ? _initialTargetHealth.FocusPos : _initialTarget.transform.position;

        UpdateAceleration();
        UpdateFollowTarget();
        UpdateRotation();
        UpdateGravity();
    }

    private void UpdateAceleration()
    {
        if (!_isActive || !(_data.aceleration > 0.0f)) return;

        _rigidbody.AddForce(_data.aceleration * _rigidbody.linearVelocity.normalized, ForceMode.Acceleration);
    }

    private void UpdateFollowTarget()
    {
        if (!(_data.followSpeed > 0.0f) || _rigidbody.useGravity) return;
        if (!_isActive || !_initialTarget) return;
        if (Utils.AreVectorsEqual(_rigidbody.linearVelocity, Vector3.zero)) return;

        Vector3 desiredDir = (_lastTargetPos - transform.position).normalized;
        _rigidbody.linearVelocity = Vector3.RotateTowards(_rigidbody.linearVelocity, desiredDir, _data.followSpeed * Time.deltaTime, 0.0f);
    }

    private void UpdateRotation()
    {
        if (!_isActive || !_data.rotateWithVel) return;

        if (_rigidbody.linearVelocity != Vector3.zero)
            transform.forward = _rigidbody.linearVelocity;
    }

    private void UpdateGravity()
    {
        if (_gravity < 0.01f && _gravity > -0.01f) return;

        _rigidbody.AddForce(Vector3.up * _gravity, ForceMode.Acceleration);
    }
    #endregion

    #region Executing

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;

        OnHit(other, false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsValid(collision.collider))
        {
            if (_data.killOnCollision) Kill();
            return;
        }

        OnHit(collision.collider, true);
    }

    private bool IsValid(Collider collider)
    {
        if (!_isInitialized || !_isActive) return false;
        if (_data.focusInitialTarget && collider.gameObject != _initialTarget) return false;
        
        //Return true if no tags specified
        if (_targetTags.Count == 0) return true;

        //Contains valid tag?
        foreach (Tag tag in _targetTags)
            if (collider.gameObject.CompareTag(TagManager.Instance.GetTagValue(tag))) return true;

        return false;
    }

    private void OnHit(Collider collider, bool collision)
    {
        if (!_executer) return;

        //execute effects on target
        _executer.Execute(collider.gameObject);

        _hitCount++;

        //Validate
        if (_hitCount > _data.maxHit)
        {
            Kill();
        }

        //SFX
        SoundManager.Instance.PlaySound(_onHitSFX);

        //VFX
        VFXManager.Instance.PlayVFXSimple(_onHitPsTemplate, transform.position, 0.0f, transform.localScale.x);
    }

    public void Kill()
    {
        if (!_isActive) return;

        _isActive = false;
        _gravity = _data.gravityOnDead;

        if (_data.clearVelOnDead)
        {
            _rigidbody.linearVelocity = Vector3.zero;   
            _rigidbody.angularVelocity = Vector3.zero;
        }
        _rigidbody.isKinematic = _data.kinematicOnDead;
        _health.Kill();

        //SFX
        SoundManager.Instance.PlaySound(_onKillSFX);
        if (_flyingSound && _flyingSound.Origin == this.gameObject) _flyingSound.Stop();

        //VFX
        VFXManager.Instance.PlayVFXSimple(_onKillPsTemplate, transform.position, 0.0f, transform.localScale.x);
    }
    #endregion
}

[System.Serializable]
public sealed class ProjectileData
{
    [SerializeField] public Projectile.Mode mode = 0;
    [SerializeField] public float speed = 12.0f;
    [SerializeField] public float aceleration = 0.0f;
    [SerializeField] public Vector3 torque = Vector3.zero;
    [SerializeField, Tooltip("Will override torque if any")] public bool rotateWithVel = false;
    [SerializeField] public float gravity = 0.0f;
    //[SerializeField, Tooltip("Trajectory mode only")] public float trajectoryAngle = 60.0f;

    [Space]
    [SerializeField, Tooltip("Max allowed rotational change towards target")] public float followSpeed = 0.0f;
    [SerializeField, Tooltip("Target requires a movement script")] public bool predictMovement = false;
    [SerializeField] public bool ignoreInitialTargetdir = true;

    [Space]
    [Tooltip("Ignore targets that are not the initial target (collision will still work)"), SerializeField] public bool focusInitialTarget = false;
    [SerializeField, Tooltip("How many targets can be hit before marking the proj as invalid/dead")] public int maxHit = 0;

    [Space]
    [SerializeField, Tooltip("if we collide with a invalid collider")] public bool killOnCollision = true;

    [Space]
    [SerializeField] public bool clearVelOnDead = false;
    [SerializeField] public bool kinematicOnDead = false;
    [SerializeField] public float gravityOnDead = 0.0f;
}