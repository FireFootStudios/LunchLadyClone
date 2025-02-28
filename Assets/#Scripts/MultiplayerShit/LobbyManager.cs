using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Vivox;
using UnityEngine;

public sealed class LobbyManager : SingletonBase<LobbyManager>
{

    [SerializeField, Tooltip("Keeps lobby alive by pinging every interval")] private float _heartBeatInterval = 60.0f;
    [SerializeField] private bool _debug = true;


    public static int MaxPlayerCount = 4;
    private const string RelayJoinCodeKey = "KEY_RelayJoinCode";

    private Lobby _currentLobby = null;
    private float _heartBeatTimer = 0.0f;

    private string _playerName = null;

    private bool _initialized = false;
    private bool _initializing = false;

    public Lobby CurrentLobby {  get { return _currentLobby; } } 
    public List<Lobby> FoundLobbies { get; private set; }

    public bool IsHost { get; private set; }



    public async Task<bool> CreateLobby(string lobbyName, bool isPrivate)
    {
        await InitIfNeeded();

        if (string.IsNullOrEmpty(lobbyName)) return false;

        // Return while in a lobby already for now
        if (_currentLobby != null) return false;

        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = isPrivate;

            // Create the lobby
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayerCount, options);

            // Allocate with the relay service
            //Allocation allocation = await AllocateRelay();

            // Set relay server data
            if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport transport)) return false;

            Allocation allocation = await AllocateRelay();
            SetRelayHostData(allocation);

            // Finally, start host
            bool startHost = NetworkManager.Singleton.StartHost();

            // Get relay code for allowing users to join
            string relayCode = await GetRelayJoinCode(allocation);
            Debug.Log("Relay join code: " + relayCode);

            // Update lobby with relay code
            UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions();
            updateLobbyOptions.Data = new Dictionary<string, DataObject> { { RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, relayCode) } };

            await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, updateLobbyOptions);


            IsHost = true;
            _heartBeatTimer = _heartBeatInterval;

            if (_debug && _currentLobby != null) Debug.Log("Succesfully created lobby!");
            else if (_debug && _currentLobby == null) Debug.LogError("Failed to create lobby!");

            if (_currentLobby != null) return true;
        }
        catch (LobbyServiceException ex) 
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> QuickJoin()
    {
        await InitIfNeeded();

        // Return while in a lobby already for now
        if (_currentLobby != null) return false;

        try
        {
            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (_debug && _currentLobby != null) Debug.Log("Succesfully joined lobby now containing a total of " + _currentLobby.Players.Count + " players!");
            else if (_debug && _currentLobby == null) Debug.LogError("Failed to join lobby!");

            // Get relay join code from lobby data
            string relayJoinCode = _currentLobby.Data[RelayJoinCodeKey].Value;
            Debug.Log("Relay join code: " + relayJoinCode);

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // Set relay server data
            SetRelayClientData(joinAllocation);

            // Start client
            NetworkManager.Singleton.StartClient();

            IsHost = false;

            if (_currentLobby != null) return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> JoinWithCode(string lobbyCode)
    {
        await InitIfNeeded();

        if (string.IsNullOrEmpty(lobbyCode)) return false;

        // Return while in a lobby already for now
        if (_currentLobby != null) return false;

        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            if (_debug && _currentLobby != null) Debug.Log("Succesfully joined lobby now containing a total of " + _currentLobby.Players.Count + " players!");
            else if (_debug && _currentLobby == null) Debug.LogError("Failed to join lobby!");

            // Get relay join code from lobby data
            string relayJoinCode = _currentLobby.Data[RelayJoinCodeKey].Value;
            Debug.Log("Relay join code: " + relayJoinCode);

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // Set relay server data
            SetRelayClientData(joinAllocation);

            // Start client
            NetworkManager.Singleton.StartClient();

            IsHost = false;

            if (_currentLobby != null) return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> Join(string lobbyID )
    {
        await InitIfNeeded();

        if (string.IsNullOrEmpty(lobbyID)) return false;

        // Return while in a lobby already for now
        if (_currentLobby != null) return false;

        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

            if (_debug && _currentLobby != null) Debug.Log("Succesfully joined lobby now containing a total of " + _currentLobby.Players.Count + " players!");
            else if (_debug && _currentLobby == null) Debug.LogError("Failed to join lobby!");

            // Get relay join code from lobby data
            string relayJoinCode = _currentLobby.Data[RelayJoinCodeKey].Value;
            Debug.Log("Relay join code: " + relayJoinCode);

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // Set relay server data
            SetRelayClientData(joinAllocation);

            // Start client
            NetworkManager.Singleton.StartClient();

            IsHost = false;

            if (_currentLobby != null) return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    private bool SetRelayHostData(Allocation allocation)
    {
        if (allocation == null) return false;

        if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport transport)) return false;

        //RelayServerData rsData = new RelayServerData(
        //      allocation.RelayServer.IpV4, // Relay server IP address
        //      (ushort)allocation.RelayServer.Port, // Relay server port
        //      allocation.AllocationIdBytes, // Allocation ID as byte array
        //      allocation.ConnectionData, // Connection data
        //      allocation.ConnectionData, // Host connection data (use your host's data here)
        //      allocation.Key, // HMAC key
        //      true // Is the connection secure?
        //      );

        // Set relay server data on unity transport
        //transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, true);

        string connectionType = "udp";
        RelayServerData rsData = allocation.ToRelayServerData(connectionType);
        transport.SetRelayServerData(rsData);

        return true;
    }

    private bool SetRelayClientData(JoinAllocation allocation)
    {
        if (allocation == null) return false;

        if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport transport)) return false;

        // Set relay server data on unity transport
        transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData, true);
        
        string connectionType = "udp";
        RelayServerData rsData = allocation.ToRelayServerData(connectionType);
        transport.SetRelayServerData(rsData);
        return true;

    }

    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            // max players - 1 (host excluded)
            //Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayerCount - 1);

            // Use the first region as an example and create the Relay allocation
            List<Region> regions = await RelayService.Instance.ListRegionsAsync();
            string region = regions[0].Id;
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayerCount - 1, region);

            return allocation;

        }
        catch(RelayServiceException ex)
        {
            Debug.LogError(ex);
        }

        return null;
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode)) return null;

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            return allocation;

        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex);
        }

        return null;
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        if (allocation == null) return null;

        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;

        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex);
        }

        return null;
    }

    public void SetPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return;

        _playerName = playerName;
    }

    public string GetPlayerName()
    {
        string playerName = "Player";
        if (!string.IsNullOrEmpty(_playerName))
        {
            playerName = _playerName;
        }

        return playerName;
    }

    public async Task<bool> DeleteLobby()
    {
        if (_currentLobby == null) return false;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            _currentLobby = null;

            Debug.Log("Lobby deleted succesfully!");
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> LeaveLobby()
    {
        if (_currentLobby == null) return false;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
            _currentLobby = null;

            Debug.Log("Left lobby succesfully!");
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> KickPlayer(string playerID)
    {
        if (_currentLobby == null || !IsHost) return false;
        if (string.IsNullOrEmpty(playerID)) return false;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerID);
            _currentLobby = null;

            Debug.Log("Kicked player with ID: " + playerID + "succesfully!");
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    public async Task<bool> GetLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            List<QueryFilter> filters = new List<QueryFilter>();
            filters.Add(new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT));

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log("Lobby list response with a total of " + response.Results.Count + " open lobbies found!");

            FoundLobbies = response.Results;
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }

        return false;
    }

    protected override async void Awake()
    {
        base.Awake();

        await InitIfNeeded();

        await GetLobbies();
    }

    private void OnDestroy()
    {
        if (IsHost) DeleteLobby();
        else LeaveLobby();
    }

    private void Update()
    {
        UpdateHeartbeat();
    }

    // Keeps the lobby alive by sending out periodic heartbeats
    private void UpdateHeartbeat()
    {
        if (!IsHost || _currentLobby == null) return;

        _heartBeatTimer -= Time.deltaTime;
        if (_heartBeatTimer > 0.0f) return;

        _heartBeatTimer = _heartBeatInterval;
        LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
    }

    private async Task<bool> InitIfNeeded()
    {
        if (_initialized || _initializing) return true;
        _initializing = true;

        await UnityServices.InitializeAsync();

        // TODO: Need to create different profiles for testing locally?

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        //Join voice lobby
        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync();

        //#1 audibleDistance: The maximum distance from the listener that a speaker can be heard. Must be > 0
        //#2 conversationalDistance: The distance from the listener within which a speaker’s voice is heard at its original volume. Must be >= 0 and <= audibleDistance.
        //#3 audioFadeIntesityByDistanceAudio: The strength of the audio fade effect as the speaker moves away from the listener. Must be >= 0. This value is rounded to three decimal places.
        //#4 audioFadeModel: The model used to determine voice volume at different distances.
        Channel3DProperties channelProperties = new Channel3DProperties(10,5,.5f,AudioFadeModel.InverseByDistance);

        await VivoxService.Instance.JoinPositionalChannelAsync("Lobby", ChatCapability.TextAndAudio,channelProperties);
        //initial set position for testing / this should be done when players join a lobby. Then when players join the game, their position gets updated to the playerGO
        VivoxService.Instance.Set3DPosition(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.up, "Lobby", true);

        _initialized = true;
        _initializing = false;

        return true;
    }

}