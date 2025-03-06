using UnityEngine;

public sealed class Crouch : Effect
{

    [Header("Stamina")]
    [SerializeField] private float _staminaUsage = 1.0f;
    [SerializeField] private float _staminaRegen = 1.0f;
    [SerializeField] private float _maxStamina = 7.0f;
    [SerializeField] private float _staminaRegenCD = 1.0f;
    [SerializeField] private float _minStaminaForUse = 0.1f;


    private FreeMovement _movement = null;



    protected override void Awake()
    {
        base.Awake();

        _movement = Ability.Source.GetComponent<FreeMovement>();


        // Cancel ability on exit grounded
        //_movement.OnStopGrounded += Ability.Cancel;
    }

    public override void OnCleanUp()
    {
        base.OnCleanUp();
    }


    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_movement) return;

    }

    public override void OnCancel()
    {
    }

    public override bool IsFinished()
    {
        return false;
    }

    public override bool CanApply()
    {
        if (!_movement) return false;

        //if we are in air and mapped id for air is not flying, dont allow sprint to be started
        //if (!_movement.IsGrounded && _movement.GetMappedMoveID(MoveType.air) != MoveID.flying) return false;

        return true;
    }

    protected override void Copy(Effect effect)
    {
        // TODO

    }
}