using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class CreateLobbyWindow : MonoBehaviour
{
    [SerializeField] private Button _createBtn = null;
    [SerializeField] private TMP_InputField _lobbyNameInput = null;
    [SerializeField] private Toggle _isPrivateToggle = null;


    private void Awake()
    {
        if (_createBtn) _createBtn.onClick.AddListener(OnCreateBtnClick);
    }

    private async void OnCreateBtnClick()
    {
        _createBtn.interactable = false;

        // Lobby name
        string lobbyName = "Tralala";
        if (_lobbyNameInput) lobbyName = _lobbyNameInput.text;

        // Private toggle
        bool isPrivate = false;
        if (_isPrivateToggle) isPrivate = _isPrivateToggle.isOn;

        // Try create lobby
        bool succes = await LobbyManager.Instance.CreateLobby(lobbyName, isPrivate);
        if (succes)
        {
            // Change state to preplay
            GameManager.Instance.TrySwitchState<PrePlayingState>();
        }

        _createBtn.interactable = true;
    }
}