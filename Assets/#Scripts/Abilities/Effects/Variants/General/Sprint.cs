using System.Collections.Generic;
using UnityEngine;

public sealed class Sprint : Effect
{
    [SerializeField, Tooltip("Target move data for when ability is active")] private List<MoveData> _overrideMoveData = new List<MoveData>();

    private CharMovement _movement = null;
    private List<MoveData> _defaultMoveData = null;



    protected override void Awake()
    {
        base.Awake();

        _movement = Ability.Source.GetComponent<CharMovement>();

        //cache default move data
        _defaultMoveData = _movement.MoveData;

        //cancel ability on exit grounded
        //_movement.OnStopGrounded += Ability.Cancel;
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_movement) return;

        _movement.MoveData = _overrideMoveData;
    }

    public override void OnCancel()
    {
        _movement.MoveData = _defaultMoveData;
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
        //TODO
    }
}