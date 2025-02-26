using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class MainGamemode : GameMode
{
    [SerializeField] private LevelAsset _sessionEndLevel = null;

    private List<PlayerN> _playersEscaped = new List<PlayerN>();


    private NetworkVariable<int> _papersCollected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _papersTotal= new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int PapersCollected { get { return _papersCollected.Value; } }
    public int PapersTotal { get { return _papersTotal.Value; } }


    public static Action<ItemN> OnPaperPickedUp;
    public static Action OnPaperRegistered;

    public static Action OnPapersChangeClient;

    public override bool CanStartGame()
    {
        return true;
    }

    protected override void Awake()
    {
        base.Awake();

        ItemManager.OnItemPickedUp += OnItemPickedUp;
        ItemManager.OnItemRegister += OnItemRegister;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _papersTotal.OnValueChanged += OnPapersRegister;
        _papersCollected.OnValueChanged += OnPapersPickedUp;
    }

    private void OnPapersRegister(int previousValue, int newValue)
    {
        OnPapersChangeClient?.Invoke();
    }

    private void OnPapersPickedUp(int previousValue, int newValue)
    {
        OnPapersChangeClient?.Invoke();
    }

    private void OnItemRegister(ItemN item)
    {
        if (item.ID != itemID.paper) return;
        if (!IsHost) return;

        _papersTotal.Value = ItemManager.Instance.GetItemsByType(itemID.paper).Count;
    }

    private void OnItemPickedUp(ItemN item)
    {
        if (item.ID != itemID.paper) return;
        if (!IsHost) return;

        List<ItemN> papers = ItemManager.Instance.GetItemsByType(itemID.paper);
        _papersTotal.Value = papers.Count;

        int collected = 0;
        foreach (ItemN paper in papers)
        {
            if (!paper.IsPickedUp) continue;

            collected++;
        }

        _papersCollected.Value = collected;

        OnPaperPickedUp?.Invoke(item);
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

        //_gameManager.SceneData.EscapeHitBox.OnTargetEnter += OnEnterEscapeHB;
        _gameManager.SceneData.EscapeHitBox.OnTargetsChange += OnEscapeHBChange;

        _papersCollected.Value = 0;
    }

    private void OnEnterEscapeHB(Collider coll)
    {
        if (!coll.TryGetComponent(out PlayerN player)) return;
        if (!InSession) return;
        if (!IsHost) return;

        if (_playersEscaped.Contains(player)) return;

        // Win con
        if (PapersCollected != PapersTotal) return;

        EndSessionWin();
        //player.gameObject.SetActive(false);

        // Disable player and mark as escaped
        //_playersEscaped.Add(player);
    }

    private void OnEscapeHBChange()
    {
        //if (!coll.TryGetComponent(out PlayerN player)) return;
        if (!InSession) return;
        if (!IsHost) return;

        // Win con
        if (PapersCollected != PapersTotal) return;

        int playersInsideHB = 0;
        foreach (PlayerN player in _gameManager.SceneData.Players)
        {
            if (!player) continue;
            if (player.Health.IsDead) continue;

            if (_gameManager.SceneData.EscapeHitBox.Targets.Exists(go => go == player.gameObject))
                playersInsideHB++;
        }

        // Return aslong as all players are not alive and well inside escape hitbox
        if (playersInsideHB < _gameManager.SceneData.Players.Count)
            return;
            
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