using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;

public sealed class LobbySlot : Slot
{
    [SerializeField] private TextMeshProUGUI _playerCountTMP = null;
    [SerializeField] private TextMeshProUGUI _lobbyNameTMP = null;


    private Lobby _lobby = null;

    public Lobby Lobby { get { return _lobby; } }


    public void Init(Lobby lobby)
    {
        if (lobby == null) return;

        _lobby = lobby;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_lobby == null) return;

        // Lobby player count
        int filledSlots = _lobby.MaxPlayers - _lobby.AvailableSlots;
        if (_playerCountTMP) _playerCountTMP.text = filledSlots + "/" + _lobby.MaxPlayers;

        // Lobby name
        if(_lobbyNameTMP) _lobbyNameTMP.text = _lobby.Name;
    }
}
