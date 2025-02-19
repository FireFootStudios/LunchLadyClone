using UnityEngine;

public sealed class Flee : FSMState
{
    [SerializeField] private bool _lookAtTarget = false;
    [Space]
    [SerializeField] private MovementModifier _mod = null;
    //range?
    //duration?
    //speed scaling for distance?


    private Character _char = null;

    private void Awake()
    {
        _char = GetComponentInParent<Character>();

        _mod.Source = this.gameObject;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        // Set max speed, can rotate and limit movementToRot
        _char.Movement.CanRotate = true;
        _char.Movement.IsStopped = false;

        UpdateFlee();
    }

    public override void OnExit()
    {
        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }

    private void Update()
    {
        UpdateFlee();
    }

    private void UpdateFlee()
    {
        TargetPair targetP = _char.Behaviour.AggroTargetSystem.GetFirstTarget();
        //closest target?

        // Movement
        if (targetP != null) _char.Movement.MoveToPos(targetP.target.transform.position);
        else _char.Movement.IsStopped = true;

        // Rotationn
        if (_lookAtTarget && targetP != null) _char.Movement.DesiredForward = targetP.target.transform.position - transform.position;
        else _char.Movement.DesiredForward = _char.Movement.DesiredForward;
    }
}