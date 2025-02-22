using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MainGamemode : GameMode
{
    [SerializeField] private LevelAsset _sessionEndLevel = null;

    private List<PlayerN> _playersEscaped = new List<PlayerN>();


    public override bool CanStartGame()
    {
        return true;
    }

    protected override void Update()
    {
        base.Update();

        UpdateEnd();
        //UpdateWin();
    }

    protected override void StartSession()
    {
        base.StartSession();

        if (!IsHost) return;

        _gameManager.SceneData.EscapeHitBox.OnTargetEnter += OnEnterEscapeHB;
    }

    private void OnEnterEscapeHB(Collider coll)
    {
        if (!coll.TryGetComponent(out PlayerN player)) return;
        if (!InSession) return;
        if (!IsHost) return;

        //if (_playersEscaped.Contains(player)) return;

        EndSessionWin();
        //player.gameObject.SetActive(false);

        // Disable player and mark as escaped
        //_playersEscaped.Add(player);
    }

    private void UpdateWin()
    {
        if (!InSession) return;
        if (!IsSpawned || !IsHost) return;

        // Get escape hitbox
        HitBox hitbox = _gameManager.SceneData.EscapeHitBox;
        if (!hitbox) return;

        // Check for all players if they are in hitbox
        int playersEscaped = 0;
        int playersAlive = 0;
        foreach (PlayerN player in _gameManager.SceneData.Players)
        {
            if (player.Health.IsDead) continue;
            playersAlive++;

            bool insideHitbox = hitbox.Targets.Contains(player.gameObject);
            if (insideHitbox) 
                playersEscaped++;
        }

        // Return if not all players who are alive have escaped
        if (playersAlive != playersEscaped) return;

        EndSessionWin();
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
        TryEndSessionNetwork(false);

        // Make sure to clean up players, as they will migrate to scene otherwise, not really sure why
        _gameManager.DeSpawnPlayers();

        // Change to end scene
        if (_sessionEndLevel)
            await _gameManager.ChangeSceneNetworkAsync(_sessionEndLevel.SceneName);

        // Switch all clients to the end state
        _gameManager.SwitchStateClientRpc(GameStateID.end);
    }

    private async void EndSessionWin()
    {
        TryEndSessionNetwork(true);

        // Make sure to clean up players, as they will migrate to scene otherwise, not really sure why
        _gameManager.DeSpawnPlayers();

        // Change to end scene
        if (_sessionEndLevel)
            await _gameManager.ChangeSceneNetworkAsync(_sessionEndLevel.SceneName);

        // Switch all clients to the end state
        _gameManager.SwitchStateClientRpc(GameStateID.end);
    }
}