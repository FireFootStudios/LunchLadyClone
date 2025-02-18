using UnityEngine;

public sealed class PrePlayingState : GameState
{
    public override void OnEnter()
    {
        base.OnEnter();

        //Disable player input
        //_gameManager.SceneData.Player.DisableMoveInput = true;
        //_gameManager.SceneData.Player.DisableInput = false;
        ////_gameManager.SceneData.Player.Movement.RB.isKinematic = true;
        //_gameManager.SceneData.Player.Movement.RB.constraints = RigidbodyConstraints.FreezeAll;

        //// Reset session
        //_gameManager.CurrentGameMode.TryResetSession();
    }

    public override void OnExit()
    {
        base.OnExit();

        //Player player = _gameManager.SceneData.Player;

        ////UnDisable player input
        //player.DisableMoveInput = false;
        //_gameManager.SceneData.Player.DisableInput = false;
        ////player.Movement.RB.isKinematic = false;
        //player.Movement.RB.constraints = RigidbodyConstraints.FreezeRotation;

        ////Reregister spawn info of player as the player might have rotated during preplay!
        //player.PlayerCameras.Spawner.ReRegisterSpawnInfo(false, true);
    }
}