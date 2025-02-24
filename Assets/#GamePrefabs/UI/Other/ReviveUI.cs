using UnityEngine;
using UnityEngine.UI;

public sealed class ReviveUI : MonoBehaviour
{
    [SerializeField] private GameObject _reviveCanFireUI = null;
    [Space]
    [SerializeField] private GameObject _reviveProgressUI = null;
    [SerializeField] private Image _reviveProgressFillImage = null;


    private PlayerN _localPlayer = null;
    private Revive _localPlayerRevive = null;


    private void OnEnable()
    {
        _localPlayer = GameManager.Instance.SceneData.LocalPlayer;
        _localPlayerRevive = _localPlayer.GetComponentInChildren<Revive>();
    }

    private void Update()
    {
        UpdateCanFire();
        UpdateReviveProgress();
    }

    private void UpdateCanFire()
    {
        if (!_localPlayerRevive) return;
        if (!_reviveCanFireUI) return;

        bool canRevive = _localPlayer.AbilityManager.CanUseAbility(_localPlayer.ReviveAbility);
        _reviveCanFireUI.SetActive(canRevive);
    }

    private void UpdateReviveProgress()
    {
        if (!_localPlayerRevive) return;
        if (!_reviveProgressUI || !_reviveProgressFillImage) return;

        bool isReviving = _localPlayerRevive.Ability.IsFiring;
        _reviveProgressUI.SetActive(isReviving);

        _reviveProgressFillImage.fillAmount = _localPlayerRevive.ProgressPerc;
    }
}