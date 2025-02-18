using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuHUD : MonoBehaviour
{
    [SerializeField] private Button _hostBtn = null;
    [SerializeField] private Button _clientBtn = null;
    [SerializeField] private Button _returnBtn = null;
    [Space]
    [SerializeField] private CreateLobbyWindow _hostWindow = null;
    [SerializeField] private JoinLobbyWindow _clientWindow = null;

    [Space]
    [SerializeField] private GameObject _playerNameGo = null;
    [SerializeField] private TMP_InputField _playerNameInput = null;


    private void Awake()
    {
        if (_hostBtn) _hostBtn.onClick.AddListener(OnHostBtnClick);
        if (_clientBtn) _clientBtn.onClick.AddListener(OnClientBtnClick);
        if (_returnBtn) _returnBtn.onClick.AddListener(OnReturnBtnClick);

        if (_playerNameInput) _playerNameInput.onValueChanged.AddListener(OnPlayerNameInputChange);
    }

    private void Start()
    {
        ResetView();
    }

    private void OnHostBtnClick()
    {
        if (_hostWindow) _hostWindow.gameObject.SetActive(true);
        if (_returnBtn) _returnBtn.gameObject.SetActive(true);

        if (_hostBtn) _hostBtn.gameObject.SetActive(false);
        if (_clientBtn) _clientBtn.gameObject.SetActive(false);
        if (_playerNameGo) _playerNameGo.SetActive(false);
    }

    private void OnClientBtnClick()
    {
        if (_clientWindow) _clientWindow.gameObject.SetActive(true);
        if (_returnBtn) _returnBtn.gameObject.SetActive(true);

        if (_hostBtn) _hostBtn.gameObject.SetActive(false);
        if (_clientBtn) _clientBtn.gameObject.SetActive(false);
        if (_playerNameGo) _playerNameGo.SetActive(false);
    }

    private void OnPlayerNameInputChange(string value)
    {
        LobbyManager.Instance.SetPlayerName(value);
    }

    private void OnReturnBtnClick()
    {
        ResetView();
    }

    private void ResetView()
    {
        if (_hostWindow) _hostWindow.gameObject.SetActive(false);
        if (_clientWindow) _clientWindow.gameObject.SetActive(false);
        if (_hostBtn) _hostBtn.gameObject.SetActive(true);
        if (_clientBtn) _clientBtn.gameObject.SetActive(true);

        if (_returnBtn) _returnBtn.gameObject.SetActive(false);

        if (_playerNameGo) _playerNameGo.SetActive(true);
    }
}