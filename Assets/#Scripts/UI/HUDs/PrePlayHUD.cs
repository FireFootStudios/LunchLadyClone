using System;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public sealed class PrePlayHUD : MonoBehaviour
{
    [SerializeField] private Button _startBtn = null;
    [SerializeField] private LevelAsset _toLoadLevel = null;
    [Space]
    [SerializeField] private TextMeshProUGUI _lobbyNameTMP = null;
    [SerializeField] private TextMeshProUGUI _lobbyCodeTMP = null;
    [SerializeField] private TextMeshProUGUI _lobbyPlayersTMP = null;

    [Space]
    [SerializeField] private TextMeshProUGUI _playerNameTMP = null;


    private Lobby _currentLobby = null;


    private void Awake()
    {
        if (_startBtn) _startBtn.onClick.AddListener(OnStartBtnClick);

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
    }

    private void OnDestroy()
    {
        //NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
    }

    private void OnEnable()
    {
        _currentLobby = LobbyManager.Instance.CurrentLobby;
        if (_currentLobby == null)
        {
            ResetUI();
            return;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (_currentLobby == null) return;

        // Lobby player count
        if (_lobbyPlayersTMP) _lobbyPlayersTMP.text = _currentLobby.Players.Count + "/" + _currentLobby.MaxPlayers;
    }

    private void OnClientConnect(ulong obj)
    {
        UpdateUI();
    }

    private async void OnStartBtnClick()
    {
        if (!_toLoadLevel) return;

        // Only host is allowed to start
        if (!LobbyManager.Instance.IsHost) return;

        // Try to load scene for all players 
        bool succes = await GameManager.Instance.TrySceneChangeNetworkAsync<PlayingState>(_toLoadLevel.SceneName);
        if (!succes) return;

        // Switch state sync
        GameManager.Instance.SwitchStateClientRpc(GameStateID.playing, false);

        // Spawn Players
        GameManager.Instance.SpawnPlayersNetwork();

        // Start gamemode sync (all player should have a gamemode session start)
        GameManager.Instance.CurrentGameMode.TryStartSessionNetwork();

        // Clean up lobby, this is no longer needed
        bool succesfulDelete = await LobbyManager.Instance.DeleteLobby();
    }

    private void ResetUI()
    {
        // Update lobby info
        if (_lobbyNameTMP) _lobbyNameTMP.text = "Invalid";
        if (_lobbyCodeTMP) _lobbyCodeTMP.text = "XXX";
        if (_lobbyPlayersTMP) _lobbyPlayersTMP.text = "X";
        if (_playerNameTMP) _playerNameTMP.text = "PlayerX";
    }

    private void UpdateUI()
    {
        if (_currentLobby == null) return;

        // Update lobby info
        if (_lobbyNameTMP) _lobbyNameTMP.text = _currentLobby.Name;
        if (_lobbyCodeTMP) _lobbyCodeTMP.text = _currentLobby.LobbyCode;
        if (_lobbyPlayersTMP) _lobbyPlayersTMP.text = _currentLobby.Players.Count + "/" + _currentLobby.MaxPlayers;

        if (_playerNameTMP) _playerNameTMP.text = LobbyManager.Instance.GetPlayerName();

        if (_startBtn) _startBtn.gameObject.SetActive(LobbyManager.Instance.IsHost);
    }
}
