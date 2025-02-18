using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    #region fields
    [SerializeField] private List<TargetType> _targettingTypes = new List<TargetType>();
    [SerializeField, Tooltip("Used for VFX and targeting")] private Transform _focusT = null;
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


    private const string _reviveStr = "Revive";

    #endregion
    #region Properties

    public List<TargetType> TargettingTypes { get { return _targettingTypes; } }
    public HealthData Data { get { return _data; } private set { _data = value; } }
    public Transform FocusT { get { return _focusT; } }
    public float Current { get { return Data.current; } private set { Data.current = value; } }
    public float Max { get { return Data.max; } }
    public bool IsDead { get; private set; }
    public float Percentage { get { return Data.current / Data.max; } }
    public Vector3 FocusPos { get { return FocusT ? FocusT.transform.position : transform.position; } }
    public float FocusScale { get { return FocusT ? FocusT.transform.localScale.x : 1.0f; } }
    public float LifeElapsed { get; private set; }

    public float RegenCooldownTimer { get; private set; }
    #endregion
    #region Events
    public Action OnDeath;
    public Action OnRevive;
    public Action<float, GameObject> OnDamaged = null; //amount, source
    public Action<float, GameObject> OnHealed = null; //amount, source
    #endregion

    //this will reset the component
    public void SetData(HealthData data)
    {
        Data = data;
        Reset();
    }

    public void Add(float delta, GameObject source)
    {
        if (IsDead) return;

        float prev = Current;

        //add delta
        Current += delta;

        //kill if close to 0 or negative
        if (Current <= 0.0f && Data.canBeDamaged) Kill();

        //clamp between 0 and max
        Current = Mathf.Clamp(Current, 0.0f, Max);

        //Calculate the actual change in health
        float healthChange = Mathf.Abs(Current - prev);

        //Events -> fired when delta != 0, doesnt mean that health change might not be 0
        if (delta > 0.0f) OnHealed?.Invoke(healthChange, source);
        else if (delta < 0.0f)
        {
            RegenCooldownTimer = Data.regenCDSinceDamage;
            OnDamaged?.Invoke(healthChange, source);
        }
    }

    public void Kill()
    {
        if (IsDead) return;

        IsDead = true;

        //enable this and other gameobjects in case they were disabled
        EvaluateWhileDeathOrAlive();
        OnDeath?.Invoke();

        //Feedback
        SoundManager.Instance.PlaySound(_onDeathSFX);
        VFXManager.Instance.PlayVFXSimple(_onDeathPsTemplate, FocusPos, 0.0f, _onDeathPsScale);

        //destroy
        if (Data.destroyOnDeath) Destroy(this.gameObject, Data.destroyOnDeathDelay);

        //invoke revive method if auto revive is checked
        if (Data.autoRevive) Invoke(_reviveStr, Data.autoReviveTime);
    }

    //Same as Reset() but invokes event and is ignored if not dead
    public void Revive()
    {
        if (!IsDead) return;

        Reset();
        OnRevive?.Invoke();

        VFXManager.Instance.PlayVFXSimple(_onSpawnTemplate, FocusPos, 0.0f, _onSpawnPsScale);
        SoundManager.Instance.PlaySound(_onSpawnSFX);
    }

    public void Reset()
    {
        RegenCooldownTimer = 0.0f;
        LifeElapsed = 0.0f;
        IsDead = false;
        Current = Max;

        //enable this and other gameobjects in case they were disabled
        EvaluateWhileDeathOrAlive();

        //cancel any remaining invokes
        CancelInvoke();
    }

    private void Awake()
    {
        if (_targettingTypes.Count == 0)
            Debug.LogError("A gameobject with a health component requires atleast 1 target type!");
    }

    private void Update()
    {
        LifeElapsed += Time.deltaTime;

        //Update life time if enabled
        if (Data.hasMaxLifeTime && LifeElapsed > Data.maxLifeTime) Kill();

        UpdateRegen();
    }

    private void UpdateRegen()
    {
        if (IsDead) return;

        RegenCooldownTimer -= Time.deltaTime;
        if (RegenCooldownTimer > 0.0f) return;

        Add(Data.regen * Time.deltaTime, this.gameObject);
    }

    private void EvaluateWhileDeathOrAlive()
    {
        foreach (GameObject go in _enableWhileDeath)
            go.SetActive(IsDead);

        foreach (GameObject go in _enableWhileAlive)
            go.SetActive(!IsDead);

        if (_toDisableColliderOnDeath) _toDisableColliderOnDeath.enabled = !IsDead;
    }
}

[System.Serializable]
public sealed class HealthData
{
    [Header("General"), SerializeField] public float current = 1.0f;
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