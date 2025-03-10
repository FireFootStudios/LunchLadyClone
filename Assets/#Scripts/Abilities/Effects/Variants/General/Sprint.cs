using System.Collections.Generic;
using UnityEngine;

public sealed class Sprint : Effect
{
    [SerializeField] private PlayerN _player = null;
    [SerializeField, Tooltip("Target move data for when ability is active")] private List<MoveData> _overrideMoveData = new List<MoveData>();

    [Space]
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0.0f, 1.0f, 0.0f);
    [SerializeField] private float _smoothTimeIn = 0.1f;
    [SerializeField] private float _smoothTimeOut = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float _staminaUsage = 1.0f;
    [SerializeField] private float _staminaRegen = 1.0f;
    [SerializeField] private float _maxStamina = 7.0f;
    [SerializeField] private float _staminaRegenCD = 1.0f;
    [SerializeField] private float _minStaminaForUse = 0.1f;


    private FreeMovement _movement = null;
    private List<MoveData> _defaultMoveData = null;


    private float _currentStamina = 0.0f;
    private float _staminaCDTimer = 0.0f;


    public float StaminaPercentage { get { return _currentStamina / _maxStamina; } }

    protected override void Awake()
    {
        base.Awake();

        _movement = Ability.Source.GetComponent<FreeMovement>();

        // Cache default move data
        _defaultMoveData = _movement.MoveData;

        // Cancel ability on exit grounded
        //_movement.OnStopGrounded += Ability.Cancel;

        _currentStamina = _maxStamina;
    }

    public override void OnCleanUp()
    {
        base.OnCleanUp();

        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        UpdateStaminaUse();
        UpdateStaminaRegen();
    }

    private void UpdateStaminaUse()
    {
        if (!Ability.IsFiring) return;

        // Set cooldown for regen
        _staminaCDTimer = _staminaRegenCD;
        
        // Lower stamina with cost
        _currentStamina -= Time.deltaTime * _staminaUsage;

        // Check if depleted
        if (_currentStamina < 0.0f)
        {
            // Cancel ability and recent current to 0
            Ability.Cancel();
            _currentStamina = 0.0f;
        }
    }

    private void UpdateStaminaRegen()
    {
        if (Ability.IsFiring) return;

        _staminaCDTimer -= Time.deltaTime;
        if (_staminaCDTimer > 0.0f) return;

        _currentStamina = Mathf.Clamp(_currentStamina + Time.deltaTime * _staminaRegen, 0.0f, _maxStamina);
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_movement) return;

        _movement.MoveData = _overrideMoveData;
        //_player.PlayerCameras.DoOffsetCo(_cameraOffset, _smoothTimeIn);
    }

    public override void OnCancel()
    {
        _movement.MoveData = _defaultMoveData;
        //_player.PlayerCameras.DoOffsetCo(_player.DefaultCameraOffset, _smoothTimeOut, 100, false);
    }

    public override bool IsFinished()
    {
        return false;
    }

    public override bool CanApply()
    {
        if (!_movement) return false;

        if (_currentStamina < _minStaminaForUse) return false;

        //if we are in air and mapped id for air is not flying, dont allow sprint to be started
        //if (!_movement.IsGrounded && _movement.GetMappedMoveID(MoveType.air) != MoveID.flying) return false;

        return true;
    }

    protected override void Copy(Effect effect)
    {
        // TODO

    }
}