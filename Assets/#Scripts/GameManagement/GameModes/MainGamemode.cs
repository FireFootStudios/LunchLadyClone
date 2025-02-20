using UnityEngine;

public sealed class MainGamemode : GameMode
{
    [SerializeField] private LevelAsset _sessionEndLevel = null;

    public override bool CanStartGame()
    {
        return true;
    }

    protected override void Update()
    {
        base.Update();

        UpdateEnd();
    }

    private void UpdateEnd()
    {
        if (!InSession) return;
        if (!IsSpawned || !IsHost) return;

        // Check for all players if they are still alive
        foreach (PlayerN player in _gameManager.SceneData.Players)
            if (!player.Health.IsDead) return;

        // All players are dead
        EndSessionFailed();
    }

    private async void EndSessionFailed()
    {
        TryEndSession(false);

        // Change to end scene
        if (_sessionEndLevel)
            await _gameManager.ChangeSceneNetworkAsync(_sessionEndLevel.SceneName);

        // Switch all clients to the end state
        _gameManager.SwitchStateClientRpc(GameStateID.end);
    }
}