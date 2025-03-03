using UnityEngine;
using Unity.Services.Vivox;
using System.Threading.Tasks;
using Unity.Netcode;
using System;

public sealed class PlayerVoiceChat : MonoBehaviour
{
    [SerializeField] private PlayerN _player = null;
    [Space]
    [SerializeField] private int _audibleDistance = 15;
    [SerializeField] private int _conversationalDistance = 12;
    [SerializeField] private float _audioFadeIntensityByDistanceaudio = .5f;
    [SerializeField] private AudioFadeModel _audioFadeModel = AudioFadeModel.InverseByDistance;

    private string _channelName = default;
    private bool _joinedChannel = false;

    private void Awake()
    {
        _player.OnNetworkSpawned += OnPlayerNetworkSpawned;
    }

    private void OnDisable()
    {
        VivoxService.Instance.LeaveAllChannelsAsync();
    }

    private void Update()
    {
        UpdateVivoxVoice();
    }

    private void UpdateVivoxVoice()
    {
        if (string.IsNullOrEmpty(_channelName)) return;
        if (!_joinedChannel) return;

        VivoxService.Instance.Set3DPosition(this.gameObject, _channelName);
    }

    private async void OnPlayerNetworkSpawned()
    {
        _channelName = LobbyManager.Instance.LobbyName;

        _joinedChannel = await JoinVivoxChannel(_channelName);
    }

    private async Task<bool> JoinVivoxChannel(string lobbyName)
    {
        if (!_player || !_player.IsSpawned || !_player.IsOwner) return false;
        if (!VivoxService.Instance.IsLoggedIn) return false;
        if (NetworkManager.Singleton.ConnectedClients.Count < 2) return false;
        if (string.IsNullOrEmpty(lobbyName)) return false;

        //#1 audibleDistance: The maximum distance from the listener that a speaker can be heard. Must be > 0
        //#2 conversationalDistance: The distance from the listener within which a speaker’s voice is heard at its original volume. Must be >= 0 and <= audibleDistance.
        //#3 audioFadeIntesityByDistanceAudio: The strength of the audio fade effect as the speaker moves away from the listener. Must be >= 0. This value is rounded to three decimal places.
        //#4 audioFadeModel: The model used to determine voice volume at different distances.
        Channel3DProperties channelProperties = new Channel3DProperties(_audibleDistance, _conversationalDistance, _audioFadeIntensityByDistanceaudio, _audioFadeModel);

        await VivoxService.Instance.JoinPositionalChannelAsync(lobbyName, ChatCapability.AudioOnly, channelProperties);
        // Initial set position for testing / this should be done when players join a lobby. Then when players join the game, their position gets updated to the playerGO
        //VivoxService.Instance.Set3DPosition(Vector3.zero, Vector3.zero, Vector3.forward, Vector3.up, "Lobby", true);
        return true;
    }
}