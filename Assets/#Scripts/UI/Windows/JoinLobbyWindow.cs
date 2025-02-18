using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public sealed class JoinLobbyWindow : MonoBehaviour
{
    [SerializeField] private Button _quickJoinBtn = null;
    [SerializeField] private Button _joinWCodeBtn = null;
    [SerializeField] private TMP_InputField _codeInput = null;

    [Header("Lobby Listing")]
    [SerializeField] private LobbySlot _lobbySlotTemplate = null;
    [SerializeField] private Transform _lobbySlotsContentT = null;


    private List<LobbySlot> _lobbySlots = new List<LobbySlot>();



    private void Awake()
    {
        if (_quickJoinBtn) _quickJoinBtn.onClick.AddListener(OnQuickJoinBtnClick);
        if (_joinWCodeBtn) _joinWCodeBtn.onClick.AddListener(OnJoinWCodeBtnClick);
    }

    private void OnEnable()
    {
        InitLobbySlots();
    }

    private async void OnQuickJoinBtnClick()
    {
        bool succes = await LobbyManager.Instance.QuickJoin();
        if (succes)
        {
            // Change state to preplay
            GameManager.Instance.TrySwitchState<PrePlayingState>();
        }
    }

    private async void OnJoinWCodeBtnClick()
    {
        if (!_codeInput || string.IsNullOrEmpty(_codeInput.text)) return;

        bool succes = await LobbyManager.Instance.JoinWithCode(_codeInput.text);
        if (succes)
        {
            // Change state to preplay
            GameManager.Instance.TrySwitchState<PrePlayingState>();
        }
    }

    private async void InitLobbySlots()
    {
        if (!_lobbySlotTemplate || !_lobbySlotsContentT) return;

        // Disable existing slots first
        foreach (LobbySlot slot in _lobbySlots)
            slot.gameObject.SetActive(false);

        // Get active lobbies
        await LobbyManager.Instance.GetLobbies();

        List<Lobby> lobbiesFound = LobbyManager.Instance.FoundLobbies;
        for (int i = 0; i < lobbiesFound.Count; i++)
        {
            Lobby lobby = lobbiesFound[i];

            LobbySlot slot = null;
            if (i < _lobbySlots.Count)
            {
                slot = _lobbySlots[i];
                slot.gameObject.SetActive(true);
            }
            else
            {
                // Create new slot
                slot = Instantiate(_lobbySlotTemplate, _lobbySlotsContentT);
                slot.OnClick += OnLobbySlotClick;
                _lobbySlots.Add(slot);
            }

            // Init with found lobby
            slot.Init(lobby);
        }
    }

    private async void OnLobbySlotClick(Slot slot)
    {
        LobbySlot lobbySlot = slot as LobbySlot;

        if (lobbySlot.Lobby == null) return;

        // Join lobby with ID
        bool succes = await LobbyManager.Instance.Join(lobbySlot.Lobby.Id);
        if (succes)
        {
            // Change state to preplay
            GameManager.Instance.TrySwitchState<PrePlayingState>();
        }
    }
}
