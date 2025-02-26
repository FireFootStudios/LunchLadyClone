using System;
using System.Collections.Generic;
using UnityEngine;

public class Ability : MonoBehaviour
{
    #region Fields
    [SerializeField, Tooltip("\"Used to find parent which serves as 'Source' of effects\"")] private int _childIndex = 2;
    [Space]
    [SerializeField] private ExecuterData _executeData = null;
    [SerializeField] private float _cooldown = 1.0f;
    [SerializeField] private float _startCooldown = 0.0f;
    [SerializeField] private float _generalCooldown = 0.0f;
    [SerializeField, Tooltip("If bigger than 0, a random value between 0 and this variable will be added to the cooldown timer")] private float _randomCooldown = 0.0f;
    [SerializeField] private bool _ignoreGeneralCooldown = false;
    [SerializeField] private bool _updateCooldownDuringFire = true;

    [Space, SerializeField] private AnimationClip _baseAnimationOverride = null;
    [SerializeField] private List<AnimationClip> _animationChains = null;

    [Space, SerializeField, Tooltip("Cancel on using any of these abilities")] private List<Ability> _cancelAbilities = new List<Ability>();
    
    protected Executer _executer = null;

    private GameObject _source = null;

    #endregion
    #region Properties
    public GameObject Source
    {
        get
        {
            if (_source) return _source;

            //find source
            Transform t = transform;
            for (int i = 0; i < _childIndex; i++)
            {
                if (!t.parent) return null;
                t = t.parent;
                _source = t.gameObject;
            }
            return _source;
        }
    }


    public float Cooldown { get { return _cooldown; } }
    public float GeneralCooldown { get { return _generalCooldown; } }
    public float CooldownTimer { get; private set; }
    public bool IsOnCooldown { get { return CooldownTimer > 0.0f; } }

    public bool IsFiring { get { return _executer && _executer.IsExecuting; } }
    public float ElapsedFiring { get; private set; }
    
    public AnimationClip BaseAnimationOverride { get { return _baseAnimationOverride; } }
    public List<AnimationClip> AttackChains { get { return _animationChains; } }

    public TargetSystem TargetSystem { get; private set; } //optional
    #endregion

    public Action OnFire;
    public Action OnCancel;

    private void Awake()
    {
        //Cache potential targetsystem
        TargetSystem = GetComponentInChildren<TargetSystem>();

        //create executer and set data
        _executer = this.gameObject.AddComponent<Executer>();
        _executer.Data = _executeData;

        //cancel abilities
        foreach (Ability ability in _cancelAbilities)
            ability.OnFire += Cancel;
    }

    private void OnEnable()
    {
        Resett();
    }

    private void OnDisable()
    {
        CooldownTimer = 0.0f;
        Cancel();
    }

    public virtual bool CanFire(float generalCooldownTimer, bool checkRotation = true)
    {
        if (!this.gameObject.activeInHierarchy || !_executer) return false;
        if (IsOnCooldown || !_executer.CanExecute()) return false;
        if (generalCooldownTimer > 0.0f && !_ignoreGeneralCooldown) return false;
        if (TargetSystem && !HasEnoughTargets()) return false;

        return true;
    }

    public bool TryFire(float generalCooldownTimer)
    {
        if (!CanFire(generalCooldownTimer)) return false;

        //set cooldown
        CooldownTimer = _cooldown + Utils.GetRandomFromBounds(0.0f, _randomCooldown);

        //reset
        ElapsedFiring = 0.0f;

        //execute
        _executer.Execute(TargetSystem ? TargetSystem.GetTargetsAsGameObjects() : null);

        OnFire?.Invoke();
        return true;
    }

    public void Cancel()
    {
        if (!IsFiring || !_executer) return;

        _executer.Cancel();

        OnCancel?.Invoke();
    }

    public void Resett()
    {
        if (!_executer) return;

        Cancel();

        CooldownTimer = _startCooldown + Utils.GetRandomFromBounds(0.0f, _randomCooldown);
        ElapsedFiring = 0.0f;

        _executer.CleanUp();

        // Reset override target if any
        // if (TargetSystem) TargetSystem.OverrideTarget = null;
    }

    private void Update()
    {
        UpdateFiring();
    }

    protected virtual void UpdateFiring()
    {
        //cooldown timer
        if (_updateCooldownDuringFire || !IsFiring) CooldownTimer -= Time.deltaTime;

        //general elapsed since fired
        if (IsFiring) ElapsedFiring += Time.deltaTime;
    }

    public bool HasTargets()
    {
        return TargetSystem && TargetSystem.GetTargets().Count > 0;
    }

    public bool HasEnoughTargets()
    {
        if (!RequiresTargets()) return true;

        return TargetSystem && _executer && TargetSystem.GetTargets().Count >= _executer.MinTargets;
    }

    public bool RequiresTargets()
    {
        return TargetSystem && _executer && _executer.MinTargets > 0 && _executer.MaxTargets > 0;
    }

    public float GetEffectiveness(GameObject target)
    {
        if (!_executer) return 0.0f;

        return _executer.CalculateEffectiveness(target);
    }

    public float CalculateCurrentEffectiveness()
    {
        if (!_executer) return 0.0f;

        float effectiveness = 0.0f;

        if (!TargetSystem) return _executer.CalculateEffectiveness(null);

        List<TargetPair> targetPairs = TargetSystem.GetTargets();
        if (targetPairs.Count == 0) return effectiveness;
        else if (_executer.MaxTargets <= 1) return targetPairs[0].effectiveness;

        int nrTargetsEff = 0;
        for (int i = 0; i < targetPairs.Count && i < _executer.MaxTargets; i++)
        {
            effectiveness += targetPairs[i].effectiveness;
            nrTargetsEff++;
        }
        effectiveness /= nrTargetsEff;

        return effectiveness;
    }
}