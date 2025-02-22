using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndHUD : MonoBehaviour
{
    [SerializeField] private Button _playAgainBtn = null;

    [Space]
    [SerializeField] private GameObject _winView = null;
    [SerializeField] private GameObject _loseView = null;


    private SessionInfo _prevSessionInfo = null;


    private void Awake()
    {
        if (_playAgainBtn) _playAgainBtn.onClick.AddListener(OnPlayAgainBtnClick);
    }

    private void OnEnable()
    {
        _prevSessionInfo = GameManager.Instance.CurrentGameMode.PrevSessionInfo;

        if (_playAgainBtn) _playAgainBtn.gameObject.SetActive(NetworkManager.Singleton.IsHost);

        if (_winView) _winView.gameObject.SetActive(_prevSessionInfo.ValidComplete);
        if (_loseView) _loseView.gameObject.SetActive(!_prevSessionInfo.ValidComplete);
    }

    private async void OnPlayAgainBtnClick()
    {
        if (_prevSessionInfo == null) return;

        // Try to load scene for all players 
        bool succes = await GameManager.Instance.TrySceneChangeNetworkAsync<PlayingState>(_prevSessionInfo.Level.Asset.SceneName);
        if (!succes) return;

        // Switch state sync
        GameManager.Instance.SwitchStateClientRpc(GameStateID.playing, false);

        // Spawn Players
        GameManager.Instance.SpawnPlayersNetwork();

        // Start gamemode sync (all player should have a gamemode session start)
        GameManager.Instance.CurrentGameMode.TryStartSessionNetwork();
    }
}