using UnityEngine;

public sealed class Combat : FSMState
{
    [SerializeField] private MovementModifier _mod = null;
    [SerializeField] private float _1stAttackDelay = 0.0f;
    [SerializeField] private LayerMask _moveLayerMask = 0;

    private Character _char = null;

    private void Awake()
    {
        _char = GetComponentInParent<Character>();

        _mod.Source = this.gameObject;

        // Disable attackbehaviour initially (assumes it has one)
        _char.GetComponent<AttackBehaviour>().enabled = false;
    }

    public override void OnEnter()
    {
        if (!_char) return;

        if (_char.Movement)
        {
            _char.Movement.IsStopped = true;
            _char.Movement.CanRotate = true;
        }

        _char.AttackBehaviour.enabled = true;
        if (_1stAttackDelay > _char.AbilityManager.DisableTimer) _char.AbilityManager.SetDisableTime(_1stAttackDelay);

        // Add mod
        _char.Movement.AddOrUpdateModifier(_mod, false);
    }

    private void Update()
    {
        if (!_char || !_char.Movement) return;

        // Update desired rotation
        Vector3 desiredForward = _char.AttackBehaviour.GetDesiredForward();
        _char.Movement.DesiredForward = desiredForward;

        // Update desired movement (if allowed)
        AiAbility preferred = _char.AttackBehaviour.PreferedAbility;

        if (preferred && preferred.AllowMove && _char.AttackBehaviour.GetDesiredMovement().magnitude > preferred.StopDistance)
            /*!Physics.Raycast(_char.Health.FocusPos, desiredForward.normalized, preferred.StopDistance, _moveLayerMask, QueryTriggerInteraction.Ignore))*/
        {
            _char.Movement.MoveToPos(desiredForward);
        }
        else _char.Movement.IsStopped = true;
    }

    public override void OnExit()
    {
        if (!_char) return;

        _char.AttackBehaviour.enabled = false;

        // Remove mod
        _char.Movement.RemoveMod(_mod);
    }

    private Vector3 DirToFocusT(Ability ability)
    {
        Vector3 dir = Vector3.zero;
        if (!ability.TargetSystem) return dir;

        TargetPair targetP = ability.TargetSystem.GetFirstTarget();
        if (targetP == null || targetP.target == null) return dir;

        TargetInfo targetInfo = targetP.target.GetComponent<TargetInfo>();
        if (!targetInfo) return dir;

        dir = (targetInfo.FocusPos - _char.TargetInfo.FocusPos).normalized;

        return dir;
    }
}