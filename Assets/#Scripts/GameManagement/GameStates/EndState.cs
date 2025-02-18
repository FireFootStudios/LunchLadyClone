using UnityEngine;

public sealed class EndState : GameState
{
    [SerializeField] private SoundSpawnData _onLevelEndSFX = null;

    private PlayerN _player = null;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEnter()
    {
        //Needs to happen after session result is created
        //_gameManager.CurrentGameMode.TryEndSession(SessionResult);

        _player = _gameManager.SceneData.Player;

        //disable player input
        //_gameManager.SceneData.Player.DisableMoveInput = true;
        if (_player)
        {
            _player.gameObject.SetActive(false);
            _player.DisableInput = true;
            //_player.Movement.DesiredMovement = Vector3.zero;
            //_player.Movement.RB.isKinematic = true;
        }

        //SFX
        SoundManager.Instance.PlaySound(_onLevelEndSFX);

        //enables HUD
        base.OnEnter();
    }

    public override void OnExit()
    {
        //if (_player) _player.Movement.RB.isKinematic = false;
        if (_player) _player.gameObject.SetActive(true);

        base.OnExit();
    }
}