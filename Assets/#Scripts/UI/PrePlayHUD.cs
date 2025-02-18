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

        // Only host is allowed to startf
        if (!LobbyManager.Instance.IsHost) return;

        // Start host/client
        //bool succesNetwork = false;
        //if (LobbyManager.Instance.IsHost) succesNetwork = NetworkManager.Singleton.StartHost();
        //else succesNetwork = NetworkManager.Singleton.StartClient();

        // Network fail?
        //if (!succesNetwork) return;


        bool succes = await GameManager.Instance.TrySceneChangeNetworkAsync<PlayingState>(_toLoadLevel.SceneName);
        if (!succes) return;

        // Clean up lobby, this is no longer needed (apparently)
        bool succesfulDelete = await LobbyManager.Instance.DeleteLobby();
        
        // Start game...
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
