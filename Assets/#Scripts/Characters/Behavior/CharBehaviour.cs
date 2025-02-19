using System;
using UnityEngine;

[RequireComponent(typeof(FSM))]
[RequireComponent(typeof(Character))]
public abstract class CharBehaviour : MonoBehaviour
{
    [Header("General")]
    [SerializeField] protected TargetSystem _aggroTargetSystem = null;
    [SerializeField] protected GameObject _statesGo = null;
    //[SerializeField, Tooltip("Is this character a dialogue speaker?")] protected DialogueSpeaker _dialogueSpeaker = null;

    [Header("Customization")]
    [SerializeField, Tooltip("Will override distance check behaviour")] protected HitBox _spawnZone = null;
    [SerializeField] protected bool _useSpawnZoneAsAggroHitbox = true;
    [SerializeField] protected bool _deAggroOnSpawnExit = true;
    [SerializeField] protected float _maxRangeFromSpawn = 5.0f;
    [SerializeField, Tooltip("Time after spawning before the AI can move.")] protected float _spawnDelay = 1f;

    [Space]
    [SerializeField, Tooltip("The minimum required time to be in attack range before going into the combat state. Can help characters attack at the edge of their hitboxes")] private float _desiredDistanceToTarget = 2.0f;


    private bool _lastAggroCheck = false;
    private AttackBehaviour _attackBeh = null;

    protected FSM FSM { get; private set; }

    public TargetSystem AggroTargetSystem { get { return _aggroTargetSystem; } }
    public bool HasAggroTarget
    {
        get
        {
            if (!_aggroTargetSystem || _aggroTargetSystem.GetTargets().Count == 0) return false;
            return true;
        }
    }
    // public bool IsAggro { get { return HasAggroTarget &&  } }

    public bool DeAggroOnSpawnExit { get { return _deAggroOnSpawnExit; } }
    public float SpawnDelay { get { return _spawnDelay; } }
    public HitBox SpawnZone {  get { return _spawnZone; } }
    public bool HasValidSpawnZone { get { return _spawnZone && _spawnZone.Colliders.Count > 0; } }

    public float MinTimeInAttackRange { get { return _desiredDistanceToTarget; } }
    public float DesiredDistanceToTarget { get; private set; }


    public Action<bool> OnAggroChange;


    protected virtual void Awake()
    {
        FSM = GetComponent<FSM>();
        _attackBeh = GetComponent<AttackBehaviour>();

        // Check if spawner, if so reset FSM on respawn
        if (TryGetComponent(out Spawner spawner))
        {
            spawner.OnRespawn += OnRespawn;
        }

        // If spawnzone set it as the aggro hitbox (if desired)
        if (_useSpawnZoneAsAggroHitbox && _spawnZone  && _aggroTargetSystem is HitBoxTargetSystem)
        {
            HitBoxTargetSystem hbts = _aggroTargetSystem as HitBoxTargetSystem;
            hbts.Hitbox.Add(_spawnZone);
        }

        if (!_statesGo) _statesGo = this.gameObject;

        if (_aggroTargetSystem)
        {
            _aggroTargetSystem.OnHasFirstTarget += (target) => CheckAggro();
            _aggroTargetSystem.OnLoseLastTarget += CheckAggro;

            //Check if already target inside
            CheckAggro();
        }
        InitFSM();
    }

    private void CheckAggro()
    {
        bool isAggro = HasAggroTarget;
        if (_lastAggroCheck == isAggro) return;

        OnAggroChange?.Invoke(isAggro);
        _lastAggroCheck = isAggro;
    }

    protected virtual void OnRespawn()
    {
        FSM.Resett();
        if (_aggroTargetSystem) _aggroTargetSystem.OverrideTarget = null;
    }

    protected abstract void InitFSM();
}