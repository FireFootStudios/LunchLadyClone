using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public sealed class SkillCheck : NetworkBehaviour
{
    [SerializeField] private SkillCheckGameType _gameType = default;
    [SerializeField] private float _localCooldown = 2.0f;
    [SerializeField] private MovementModifier _playerMoveMod = null;
    [SerializeField, Tooltip("Max amount of time the client will wait for server permission when trying to do this skillCheck")] private float _maxWaitServerPermission = 3.0f;
    [SerializeField, Tooltip("Time out client (on server side) after this duration so skill check can be done by others")] private float _timeOutClient = 60.0f;

    [Space]
    [SerializeField] private SoundSpawnData _skillCheckStartSFX = null;
    [SerializeField] private SoundSpawnData _skillCheckSucceedSFX = null;
    [SerializeField] private SoundSpawnData _skillCheckFailSFX = null;


    //private NetworkVariable<bool> _inProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> _activeSkillCheckClientId = new NetworkVariable<ulong>(0);

    public TaskCompletionSource<bool> PlayingGameTCS { get; private set; }
    public PlayerN TargetPlayer { get; private set; }
    public bool WaitingServerResponse { get; private set; }
    public float CooldownTimer { get; private set; } // Local (per client) cooldown
    public float ClientTimeOutTimer { get; private set; } // Server side
    public bool TimedOut { get; private set; } // True if server told us

 
    public bool CanSkillCheck()
    {
        if (!IsSpawned) return false;

        // Cooldown
        if (CooldownTimer > 0.0f) return false;

        // Check if busy
        if (PlayingGameTCS != null && PlayingGameTCS.Task != null && PlayingGameTCS.Task.Status == TaskStatus.Running) return false;

        // Check if any client is already doing this skill check
        if (_activeSkillCheckClientId.Value != 0) return false;

        return true;
    }

    public async Task<bool> DoSkillCheck(PlayerN player)
    {
        if (!player) return false;
        if (player.Health.IsDead) return false;

        // Player only requires setup once, since this is locally only for 1 player anyway
        if (TargetPlayer == null)
        {
            TargetPlayer = player;
            TargetPlayer.Health.OnDamaged += OnPlayerDamaged;
        }
        PlayingGameTCS = null;
        WaitingServerResponse = true;
        TimedOut = false;

        // Player move mod
        TargetPlayer.Movement.AddOrUpdateModifier(_playerMoveMod);

        // Get confirmation from server that we (our clientID) started...
        TryStartServerRpc();

        // Await server response
        float waitElapsed = 0.0f;
        while (WaitingServerResponse || waitElapsed > _maxWaitServerPermission)
        {
            await Awaitable.NextFrameAsync();
            waitElapsed += Time.deltaTime;
        }

        // If no tcs we failed to start the game (server or local)
        if (PlayingGameTCS == null)
        {
            TargetPlayer.Movement.RemoveMod(_playerMoveMod.Source);
            return false;
        }

        // Await skill check TCS
        bool succes = await PlayingGameTCS.Task;

        // Check if timed out
        if (TimedOut) 
            succes = false;

        // SFX
        if (succes) SoundManager.Instance.PlaySound(_skillCheckSucceedSFX);
        else SoundManager.Instance.PlaySound(_skillCheckFailSFX);

        // Tell server we are done
        ClientFinishedSkillCheckServerRpc();

        // Set local cooldown
        CooldownTimer = _localCooldown;
        TargetPlayer.Movement.RemoveMod(_playerMoveMod.Source);

        return succes;
    }

    private void OnPlayerDamaged(float dmg, GameObject source)
    {
        // Cancel skillcheck
        WaitingServerResponse = false;

        if (PlayingGameTCS != null && PlayingGameTCS.Task != null)
            PlayingGameTCS.TrySetCanceled();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryStartServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        if (_activeSkillCheckClientId.Value == 0)
        {
            _activeSkillCheckClientId.Value = requestingClientId;
            ClientTimeOutTimer = _timeOutClient;
            ClientStartClientRpc(true, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestingClientId } } });
        }
        else
        {
            // Skill check already in progress; deny request
            ClientStartClientRpc(false, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestingClientId } } });
        }
    }

    [ClientRpc]
    private void ClientStartClientRpc(bool canStart, ClientRpcParams clientRpcParams = default)
    {
        if (!WaitingServerResponse) return;
        WaitingServerResponse = false;

        if (canStart)
        {
            // Get a task completions source from game if  succesful start
            bool gameStarted = SkillCheckManager.Instance.TryStartGame(TargetPlayer, _gameType, out TaskCompletionSource<bool> tcs);
            PlayingGameTCS = tcs;

            // Skillcheck start SFX
            if (gameStarted)
                SoundManager.Instance.PlaySound(_skillCheckStartSFX);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientFinishedSkillCheckServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        if (_activeSkillCheckClientId.Value == requestingClientId)
        {
            // Skill check completed by the same client who started it
            _activeSkillCheckClientId.Value = 0;
        }
    }

    private void Awake()
    {
        _playerMoveMod.Source = this.gameObject;
    }

    private void Update()
    {
        CooldownTimer -= Time.deltaTime;

        UpdateClientTimeOut();
    }

    // Only host updates this
    private void UpdateClientTimeOut()
    {
        if (!IsHost) return;

        ulong activeClientID = _activeSkillCheckClientId.Value;
        if (activeClientID == 0) return;

        ClientTimeOutTimer -= Time.deltaTime;
        if (ClientTimeOutTimer > 0.0f) return;

        // Tell client he is timed out
        TimeOutClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { activeClientID } } });
        _activeSkillCheckClientId.Value = 0;
    }

    [ClientRpc]
    private void TimeOutClientRpc(ClientRpcParams clientRpcParams = default)
    {
        TimedOut = true;
        if (PlayingGameTCS != null && PlayingGameTCS.Task != null) 
            PlayingGameTCS.SetCanceled();
    }
}