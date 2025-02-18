using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Spawner))]
public abstract class Character : MonoBehaviour
{
    #region Fields
    [SerializeField] private Health _health = null;

    private CharBehaviour _behaviour = null;
    private CharMovement _movement = null;
    #endregion

    #region Properties
    public Health Health { get { return _health; } }
    //public DialogueSpeaker DialogueSpeaker { get { return _dialogueSpeaker; } }

    public CharBehaviour Behaviour
    {
        get
        {
            if (!_behaviour) _behaviour = GetComponent<CharBehaviour>();
            return _behaviour;
        }
    }
    public CharMovement Movement
    {
        get
        {
            if (!_movement) _movement = GetComponent<CharMovement>();
            return _movement;
        }
    }
    public AbilityManager AbilityManager { get; private set; }
    public Spawner Spawner { get; private set; }
    #endregion

    protected virtual void Awake()
    {
        AbilityManager = GetComponent<AbilityManager>();
        Spawner = GetComponent<Spawner>();

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