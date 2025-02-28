using UnityEngine;

public sealed class GrabUI : MonoBehaviour
{
    [SerializeField] private GameObject _canGrabGo = null;


    private PlayerN _localPlayer = null;
    private Grab _grab = null;


    private void OnEnable()
    {
        _localPlayer = GameManager.Instance.SceneData.LocalPlayer;
        _grab = _localPlayer.GetComponentInChildren<Grab>();
    }

    private void Update()
    {
        UpdateCanFire();
    }

    private void UpdateCanFire()
    {
        if (!_localPlayer) return;
        if (!_canGrabGo) return;

        bool canRevive = _localPlayer.AbilityManager.CanUseAbility(_localPlayer.GrabAbility);
        _canGrabGo.SetActive(canRevive);
    }
}
