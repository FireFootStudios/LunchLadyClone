using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class JoinLobbyWindow : MonoBehaviour
{
    [SerializeField] private Button _quickJoinBtn = null;
    [SerializeField] private Button _joinWCodeBtn = null;
    [SerializeField] private TMP_InputField _codeInput = null;


    private void Awake()
    {
        if (_quickJoinBtn) _quickJoinBtn.onClick.AddListener(OnQuickJoinBtnClick);
        if (_joinWCodeBtn) _joinWCodeBtn.onClick.AddListener(OnJoinWCodeBtnClick);
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
}
