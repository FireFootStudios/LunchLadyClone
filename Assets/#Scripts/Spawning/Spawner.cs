using System;
using UnityEngine;

public sealed class Spawner : MonoBehaviour
{
    [SerializeField, Tooltip("Set this to link this object to another parenting spawner so we reset after they reset!")] private Spawner _parentSpawner = null;

    [Space]
    [SerializeField, Tooltip("If true and a health comp is linked, this object will respawn on revive")] private bool _respawnOnRevive = true;
    [SerializeField, Tooltip("If set, the health comp will be reset on a Respawn")] private Health _linkedHealth = null;
    [Space]
    [SerializeField, Tooltip("Set to unparent all on reset")] private CharacterParent _characterParent = null;
    [SerializeField, Tooltip("Set to reset movement modifiers + grounded objects on reset")] private FreeMovement _charMovement = null;
    [SerializeField] private AbilityManager _abilityManager = null;

    private SpawnManager _spawnManager = null;
    private Rigidbody _rigidbody = null;
    private SpawnInfo _spawnInfo = null;


    public SpawnInfo SpawnInfo
    {
        get
        {
            if (_spawnInfo == null) RegisterSpawnInfo();
            return _spawnInfo;
        }
    }
    
    public Action OnRespawn;




    public void ReRegisterSpawnInfo(bool pos, bool rot)
    {
        RegisterSpawnInfo();

        if (pos)
        {
            SpawnInfo.pos = transform.position;
            SpawnInfo.localPos = transform.localPosition;
        }

        if (rot)
        {
            SpawnInfo.rotation = transform.rotation;
            SpawnInfo.forward = transform.forward;
            SpawnInfo.rotLocalEuler = transform.localEulerAngles;
        }
    }

    public void Resett()
    {
        // Very important to unparent all potential parented characters, if we reset when they are still parented (and they have already been reset), they will move along and have wrong transform data
        if (_characterParent) _characterParent.UnparentAll();
        if (_charMovement)
        {
            _charMovement.Stop();
            _charMovement.ClearGrounded();
            _charMovement.ClearModifiers();
        }
        if (_abilityManager) _abilityManager.Resett();

        transform.SetPositionAndRotation(SpawnInfo.pos, SpawnInfo.rotation);
        if (transform.parent != null) transform.localPosition = SpawnInfo.localPos;


        if (_rigidbody && !_rigidbody.isKinematic) _rigidbody.linearVelocity = Vector3.zero;

        //Revive if dead, else just reset
        if (_linkedHealth && _linkedHealth.IsDead) _linkedHealth.Revive_Server();
        else if (_linkedHealth) _linkedHealth.Resett();
    }

    public void Respawn()
    {
        Resett();

        OnRespawn?.Invoke();
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _spawnManager = SpawnManager.Instance;

        if (_linkedHealth && _respawnOnRevive) _linkedHealth.OnRevive += Respawn;
    }

    private void OnValidate()
    {
        if (!_abilityManager) _abilityManager = GetComponent<AbilityManager>();
    }

    private void Start()
    {
        //If not yet registered
        RegisterSpawnInfo();
    }

    private void OnEnable()
    {
        //Only if no linked parenting spawner do we register to spawner ourselves, otherwise we reset on parent reset!
        if (!_parentSpawner)
        {
            _spawnManager.Register(this);
            SpawnManager.OnReset += Respawn;
        }
        else
        {
            _parentSpawner.OnRespawn += Respawn;
        }
    }

    private void OnDisable()
    {
        if (_spawnManager)
        {
            _spawnManager.UnRegister(this);
            SpawnManager.OnReset -= Respawn;
        }

        if (_parentSpawner)
        {
            _parentSpawner.OnRespawn -= Respawn;
        }
    }

    private void RegisterSpawnInfo()
    {
        if (_spawnInfo != null) return;

        _spawnInfo = new SpawnInfo
        {
            pos = transform.position,
            localPos = transform.localPosition,
            rotation = transform.rotation,
            forward = transform.forward,
            rotLocalEuler = transform.localEulerAngles,
        };
    }
}

public sealed class SpawnInfo
{
    public Vector3 pos;
    public Vector3 localPos;
    public Quaternion rotation;
    public Vector3 forward;
    public Vector3 rotLocalEuler;
}