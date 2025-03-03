using UnityEngine;

public sealed class InteractUI : MonoBehaviour
{
    [SerializeField] private GameObject _canInteractGo = null;


    private PlayerN _localPlayer = null;
    private Interact _interact = null;


    private void OnEnable()
    {
        _localPlayer = GameManager.Instance.SceneData.LocalPlayer;
        _interact = _localPlayer.GetComponentInChildren<Interact>();
    }

    private void Update()
    {
        UpdateCanFire();
    }

    private void UpdateCanFire()
    {
        if (!_localPlayer) return;
        if (!_canInteractGo) return;

        bool canFire = _localPlayer.AbilityManager.CanUseAbility(_localPlayer.InteractAbility);
        _canInteractGo.SetActive(canFire);
    }
}