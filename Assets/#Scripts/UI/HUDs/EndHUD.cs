using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndHUD : MonoBehaviour
{
    [SerializeField] private Button _playAgainBtn = null;

    private SessionInfo _prevSessionInfo = null;


    private void Awake()
    {
        if (_playAgainBtn) _playAgainBtn.onClick.AddListener(OnPlayAgainBtnClick);
    }

    private void OnEnable()
    {
        _prevSessionInfo = GameManager.Instance.CurrentGameMode.PrevSessionInfo;

        if (_playAgainBtn) _playAgainBtn.gameObject.SetActive(NetworkManager.Singleton.IsHost);
    }

    private async void OnPlayAgainBtnClick()
    {
        if (_prevSessionInfo == null) return;

        // Try to load scene for all players 
        bool succes = await GameManager.Instance.TrySceneChangeNetworkAsync<PlayingState>("JochenTest");

        if (!succes) return;

        // Switch state sync
        GameManager.Instance.SwitchStateClientRpc(GameStateID.playing, false);

        // Spawn Players
        GameManager.Instance.SpawnPlayersNetwork();

        // Start gamemode sync (all player should have a gamemode session start)
        GameManager.Instance.CurrentGameMode.TryStartSession();
    }
}