using UnityEngine;

public sealed class Crouch : Effect
{
    [SerializeField] private MovementModifier _moveMod = null;
    [SerializeField] private PlayerN _player = null;
    [SerializeField] private CapsuleCollider _collider = null;

    [Space]
    [SerializeField] private Vector3 _crouchedCameraOffset = new Vector3(0.0f, 1.0f, 0.0f);
    [SerializeField] private Vector3 _crouchedFocusTPos = new Vector3(0.0f, 1.0f, 0.0f);

    [Space]
    [SerializeField] private float _crouchedColliderHeight = 0.0f;
    [SerializeField] private Vector3 _crouchedColliderCenter = Vector3.zero;

    private bool _defaultsInitialized = false;
    private Vector3 _defaultCameraOffset = Vector3.zero;
    private Vector3 _defaultFocusTPos = Vector3.zero;

    private float _defaultColliderHeight = 0.0f;
    private Vector3 _defaultColliderCenter = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();

        _moveMod.Source = this.gameObject;
        // Cancel ability on exit grounded
        //_movement.OnStopGrounded += Ability.Cancel;
    }

    public override void OnCleanUp()
    {
        base.OnCleanUp();
    }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_defaultsInitialized) return;

        _player.Movement.AddOrUpdateModifier(_moveMod, false);

        _player.PlayerCameras.Spawner.SpawnInfo.localPos = _crouchedCameraOffset;
        _player.TargetInfo.FocusT.localPosition = _crouchedFocusTPos;

        _collider.center = _crouchedColliderCenter;
        _collider.height = _crouchedColliderHeight;
    }

    private void InitDefaults()
    {
        if (_defaultsInitialized) return;
        if (!_player || !_player.PlayerCameras) return;

        _defaultFocusTPos = _player.TargetInfo.FocusT.localPosition;
        _defaultCameraOffset = _player.PlayerCameras.Spawner.SpawnInfo.localPos;
        _defaultsInitialized = true;

        _defaultColliderCenter = _collider.center;
        _defaultColliderHeight = _collider.height;
    }

    public override void OnCancel()
    {
        _player.Movement.RemoveMod(_moveMod);

        _player.PlayerCameras.Spawner.SpawnInfo.localPos = _defaultCameraOffset;
        _player.TargetInfo.FocusT.localPosition = _defaultFocusTPos;

        _collider.center = _defaultColliderCenter;
        _collider.height = _defaultColliderHeight;
    }

    public override bool IsFinished()
    {
        return false;
    }

    public override bool CanApply()
    {
        InitDefaults();
        if (!_defaultsInitialized) return false;

        //if we are in air and mapped id for air is not flying, dont allow sprint to be started
        //if (!_movement.IsGrounded && _movement.GetMappedMoveID(MoveType.air) != MoveID.flying) return false;

        return true;
    }

    protected override void Copy(Effect effect)
    {
        // TODO

    }
}