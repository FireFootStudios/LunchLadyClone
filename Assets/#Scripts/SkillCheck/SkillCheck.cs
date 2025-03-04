using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public sealed class SkillCheck : NetworkBehaviour
{
    [SerializeField] private SkillCheckGameType _gameType = default;
    [SerializeField] private float _localCooldown = 2.0f;

    [Space]
    [SerializeField] private SoundSpawnData _skillCheckStartSFX = null;
    [SerializeField] private SoundSpawnData _skillCheckSucceedSFX = null;
    [SerializeField] private SoundSpawnData _skillCheckFailSFX = null;


    //private NetworkVariable<bool> _inProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> _activeSkillCheckClientId = new NetworkVariable<ulong>(0);

    public TaskCompletionSource<bool> PlayingGameTCS { get; private set; }
    public PlayerN TargetPlayer {  get; private set; }
    public bool WaitingServerResponse { get; private set; }
    public float CooldownTimer { get; private set; }


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

        TargetPlayer = player;
        PlayingGameTCS = null;
        WaitingServerResponse = true;
        
        // Get confirmation from server that we (our clientID) started...
        TryStartServerRpc();

        // Await server response
        while (WaitingServerResponse)
            await Awaitable.NextFrameAsync();
        
        // If no tcs we failed to start the game (server or local)
        if (PlayingGameTCS == null) return false;

        // Await skill check TCS
        bool succes = await PlayingGameTCS.Task;

        // SFX
        if (succes) SoundManager.Instance.PlaySound(_skillCheckSucceedSFX);
        else SoundManager.Instance.PlaySound(_skillCheckFailSFX);

        // Tell server we are done
        ClientFinishedSkillCheckServerRpc();

        // Set local cooldown
        CooldownTimer = _localCooldown;
        return succes;
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryStartServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        if (_activeSkillCheckClientId.Value == 0)
        {
            _activeSkillCheckClientId.Value = requestingClientId;
            NotifyClientCanStartClientRpc(true, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestingClientId } } });
        }
        else
        {
            // Skill check already in progress; deny request
            NotifyClientCanStartClientRpc(false, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestingClientId } } });
        }
    }

    [ClientRpc]
    private void NotifyClientCanStartClientRpc(bool canStart, ClientRpcParams clientRpcParams = default)
    {
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

    private void Update()
    {
        CooldownTimer -= Time.deltaTime;
    }
}