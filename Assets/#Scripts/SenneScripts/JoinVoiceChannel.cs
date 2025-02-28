using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

public class JoinVoiceChannel : MonoBehaviour
{

    async void Start()
    {
        //await UnityServices.InitializeAsync();
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync();
        await VivoxService.Instance.JoinGroupChannelAsync("Lobby", ChatCapability.AudioOnly); 
    }
    async void InitializeAsync()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        await VivoxService.Instance.InitializeAsync();
    }


    public async void LoginToVivoxAsync()
    {
        LoginOptions options = new LoginOptions();
        await VivoxService.Instance.LoginAsync(options);
    }

    public async void JoinLobbyVoice()
    {
        ChannelOptions channelOptions = new ChannelOptions();
        await VivoxService.Instance.JoinGroupChannelAsync("Lobby", ChatCapability.AudioOnly); 
        //await VivoxService.Instance.JoinEchoChannelAsync("ChannelName", ChatCapability.AudioOnly);
    }

    public async void LeaveLobbyVoice()
    {
        string channelToLeave = "Lobby";
        await VivoxService.Instance.LeaveChannelAsync(channelToLeave);
    }


}
