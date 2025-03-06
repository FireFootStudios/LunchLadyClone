using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class Health : NetworkBehaviour
{
    #region fields

    [SerializeField] private List<TargetType> _targettingTypes = new List<TargetType>();
    [SerializeField] private HealthData _data = new HealthData();

    [Space]
    [SerializeField] private List<GameObject> _enableWhileAlive = new List<GameObject>();
    [SerializeField] private List<GameObject> _enableWhileDeath = new List<GameObject>();
    [SerializeField] private Collider _toDisableColliderOnDeath = null;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem _onDeathPsTemplate = null;
    [SerializeField] private float _onDeathPsScale = 1.0f;
    [Space]
    [SerializeField] private ParticleSystem _onSpawnTemplate = null;
    [SerializeField] private float _onSpawnPsScale = 1.0f;
    [Space]
    [SerializeField] private SoundSpawnData _onDeathSFX = null;
    [SerializeField] private SoundSpawnData _onSpawnSFX = null;

    private NetworkVariable<float> _current = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const string _reviveStr = "Revive";

    #endregion
    #region Properties

    public List<TargetType> TargettingTypes { get { return _targettingTypes; } }
    public HealthData Data { get { return _data; } private set { _data = value; } }
    public float Current { get { return _current.Value; } }
    public float Max { get { return Data.max; } }
    public bool IsDead { get { return _isDead.Value; } }
    public float Percentage { get { return Data.start / Data.max; } }
    public float LifeElapsed { get; private set; }

    public float RegenCooldownTimer { get; private set; }

    #endregion

    #region Events

    public Action OnDeath;
    public Action OnRevive;
    public Action<float, GameObject> OnDamaged = null; //amount, source
    public Action<float, GameObject> OnHealed = null; //amount, source

    #endregion

    // This will reset the component
    public void SetData(HealthData data)
    {
        Data = data;
        Resett();
    }

    public void Add_Server(float delta, GameObject source)
    {
        if (IsDead) return;

        // Prevent changing if not host for now 
        if (NetworkManager && NetworkManager.Singleton.IsListening && !IsHost) return;

        // float prev = Current;
        float newCurrent = Current;

        // Add delta
        newCurrent += delta;

        // Kill if close to 0 or negative
        if (newCurrent <= 0.0f && Data.canBeDamaged) Kill();

        // Clamp between 0 and max
        newCurrent = Mathf.Clamp(newCurrent, 0.0f, Max);

        // Finally update network variable
        _current.Value = newCurrent;
    }

    // Call when a client needs to revive someone
    [ServerRpc(RequireOwnership = false)]
    public void ReviveClientServerRpc()
    {
        Resett();
    }

    // Call when a client needs to make a health change, this will go through the server first
    [ServerRpc]
    public void Add_ClientServerRpc(float delta)
    {
        Add_Server(delta, null);
    }

    // Call when a client needs to make a health change, this will go through the server first
    [ServerRpc(RequireOwnership = false)]
    public void Set_ClientServerRpc(float newValue)
    {
        _current.Value = newValue;
    }

    public void Kill()
    {
        if (IsDead) return;
        if (NetworkManager && NetworkManager.Singleton.IsListening && !IsHost) return;

        _isDead.Value = true;
    }

    // Same as Reset() but is ignored if not dead
    public void Revive_Server()
    {
        if (!IsDead) return;
        if (NetworkManager && NetworkManager.Singleton.IsListening && !IsHost) return;

        Resett();
    }

    public void Resett()
    {
        if (!IsHost) return;

        _isDead.Value = false;
        _current.Value = _data.start;
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to Network changes
        _current.OnValueChanged += OnHealthChangedNetwork;
        _isDead.OnValueChanged += OnIsDeadChangedNetwork;
    }

    private void Awake()
    {
        if (_targettingTypes.Count == 0)
            Debug.LogError("A gameobject with a health component requires atleast 1 target type!");

        // For local testing, not sure if this can cause issues
        if (!NetworkManager || !NetworkManager.Singleton.IsListening)
        {
            // Subscribe to Network changes
            _current.OnValueChanged += OnHealthChangedNetwork;
            _isDead.OnValueChanged += OnIsDeadChangedNetwork;
        }
    }

    private void Start()
    {
        Set_ClientServerRpc(_data.start);
    }

    private void Update()
    {
        LifeElapsed += Time.deltaTime;

        // Update life time if enabled
        if (Data.hasMaxLifeTime && LifeElapsed > Data.maxLifeTime) Kill();

        UpdateRegen();
    }

    private void UpdateRegen()
    {
        if (IsDead) return;

        if (!(Data.regen > 0.0f)) return;

        RegenCooldownTimer -= Time.deltaTime;
        if (RegenCooldownTimer > 0.0f) return;

        Add_Server(Data.regen * Time.deltaTime, this.gameObject);
    }

    private void EvaluateWhileDeathOrAlive()
    {
        foreach (GameObject go in _enableWhileDeath)
            go.SetActive(IsDead);

        foreach (GameObject go in _enableWhileAlive)
            go.SetActive(!IsDead);

        if (_toDisableColliderOnDeath) _toDisableColliderOnDeath.enabled = !IsDead;
    }

    private void OnHealthChangedNetwork(float previousValue, float newValue)
    {
        // If host return, as he already changed this initially
        //if (IsHost) return;

        // Calculate the actual change in health
        float delta = newValue - previousValue;
        float change = Mathf.Abs(delta);

        // Events -> fired when delta != 0, doesnt mean that health change might not be 0
        if (delta > 0.0f) OnHealed?.Invoke(change, null);
        else if (delta < 0.0f)
        {
            RegenCooldownTimer = Data.regenCDSinceDamage;
            OnDamaged?.Invoke(change, null);
        }
    }

    private void OnIsDeadChangedNetwork(bool previousValue, bool newValue)
    {
        // Did we die
        if (newValue == true)
        {
            // Enable this and other gameobjects in case they were disabled
            EvaluateWhileDeathOrAlive();
            OnDeath?.Invoke();

            // Feedback
            SoundManager.Instance.PlaySound(_onDeathSFX);
            //VFXManager.Instance.PlayVFXSimple(_onDeathPsTemplate, FocusPos, 0.0f, _onDeathPsScale);

            // Destroy
            if (Data.destroyOnDeath) Destroy(this.gameObject, Data.destroyOnDeathDelay);

            // Invoke revive method if auto revive is checked
            if (Data.autoRevive) Invoke(_reviveStr, Data.autoReviveTime);
        }
        // Did we revive
        else if (newValue == false && previousValue == true)
        {
            OnRevive?.Invoke();

            //VFXManager.Instance.PlayVFXSimple(_onSpawnTemplate, FocusPos, 0.0f, _onSpawnPsScale);
            SoundManager.Instance.PlaySound(_onSpawnSFX);
        }
        // Reset
        else if (newValue == false && previousValue == false)
        {
            RegenCooldownTimer = 0.0f;
            LifeElapsed = 0.0f;

            // Enable this and other gameobjects in case they were disabled
            EvaluateWhileDeathOrAlive();

            // Cancel any remaining invokes
            CancelInvoke();
        }
    }
}

[System.Serializable]
public sealed class HealthData
{
    [Header("General"), SerializeField] public float start = 1.0f;
    [SerializeField] public float max = 1.0f;
    [SerializeField] public float regen = 0.25f;
    [SerializeField] public float regenCDSinceDamage = 3.0f;

    [Header("Death/Revive"), SerializeField, Tooltip("If false, cannot die through damage.")] public bool canBeDamaged = true;
    [SerializeField] public bool autoRevive = true;
    [SerializeField] public float autoReviveTime = 5.0f;
    [SerializeField, Space] public bool destroyOnDeath = false;
    [SerializeField] public float destroyOnDeathDelay = 3.0f;
    [SerializeField, Space] public bool hasMaxLifeTime = false;
    [SerializeField] public float maxLifeTime = 30.0f;
}