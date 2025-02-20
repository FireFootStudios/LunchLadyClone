using System;
using TMPro;
using Unity.Netcode;
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


    private void Awake()
    {
        if (_startBtn) _startBtn.onClick.AddListener(OnStartBtnClick);

        GameManager.OnSceneChangeFinish += OnSceneChangeFinish;
    }

    private void OnEnable()
    {
        Lobby currentLobby = LobbyManager.Instance.CurrentLobby;
        if (currentLobby == null)
        {
            ResetUI();
            return;
        }

        // Update lobby info
        if (_lobbyNameTMP) _lobbyNameTMP.text = currentLobby.Name;
        if (_lobbyCodeTMP) _lobbyCodeTMP.text = currentLobby.LobbyCode;
        if (_lobbyPlayersTMP) _lobbyPlayersTMP.text = currentLobby.Players.Count + "/" + currentLobby.MaxPlayers;

        if (_playerNameTMP) _playerNameTMP.text = LobbyManager.Instance.GetPlayerName();

        if (_startBtn) _startBtn.gameObject.SetActive(LobbyManager.Instance.IsHost);
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
        GameManager.Instance.CurrentGameMode.TryStartSession();

        // Clean up lobby, this is no longer needed
        bool succesfulDelete = await LobbyManager.Instance.DeleteLobby();
    }

    // For now we wait here
    private void OnSceneChangeFinish(string obj)
    {

    }

    private void ResetUI()
    {
        // Update lobby info
        if (_lobbyNameTMP) _lobbyNameTMP.text = "Invalid";
        if (_lobbyCodeTMP) _lobbyCodeTMP.text = "XXX";
        if (_lobbyPlayersTMP) _lobbyPlayersTMP.text = "X";
        if (_playerNameTMP) _playerNameTMP.text = "PlayerX";
    }
}
