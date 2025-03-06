using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Spawner))]
public abstract class Character : NetworkBehaviour
{
    #region Fields
    [SerializeField] private Health _health = null;
    [SerializeField] private TargetInfo _targetInfo = null;

    private CharBehaviour _behaviour = null;
    private NavMeshMovement _movement = null;
    protected NavMeshAgent _agent = null;

    #endregion

    #region Properties
    public Health Health { get { return _health; } }
    public TargetInfo TargetInfo { get { return _targetInfo; } }
    //public DialogueSpeaker DialogueSpeaker { get { return _dialogueSpeaker; } }

    public CharBehaviour Behaviour
    {
        get
        {
            if (!_behaviour) _behaviour = GetComponent<CharBehaviour>();
            return _behaviour;
        }
    }
    public NavMeshMovement Movement
    {
        get
        {
            if (!_movement) _movement = GetComponent<NavMeshMovement>();
            return _movement;
        }
    }
    public NavMeshAgent NavMeshAgent
    {
        get
        {
            if (!_agent) _agent = GetComponent<NavMeshAgent>();
            return _agent;
        }
    }

    public AttackBehaviour AttackBehaviour { get; private set; }
    public AbilityManager AbilityManager { get; private set; }
    public RagdollController RagdollController { get; private set; }
    public Spawner Spawner { get; private set; }
    #endregion

    protected virtual void Awake()
    {
        AbilityManager = GetComponent<AbilityManager>();
        AttackBehaviour = GetComponent<AttackBehaviour>();
        RagdollController = GetComponent<RagdollController>();
        Spawner = GetComponent<Spawner>();
        _agent = GetComponent<NavMeshAgent>();

        Spawner.OnRespawn += OnRespawn;
    }

    protected virtual void OnRespawn()
    {
        if (Movement)
        {
            Movement.ClearModifiers();
        }
    }
}