using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayingHUD : MonoBehaviour
{
    [SerializeField] private GameObject _deathGo = null;
    [SerializeField] private GameObject _jumpScareGo = null;


    private void OnEnable()
    {
        PlayerN playerLocal = GameManager.Instance.SceneData.LocalPlayer;
        if (playerLocal)
        {
            playerLocal.Health.OnDeath += OnPlayerDeath;
            playerLocal.Health.OnRevive += OnPlayerRevive;
        }

        if (_deathGo) _deathGo.SetActive(playerLocal ? playerLocal.Health.IsDead : false);
    }

    private void OnPlayerRevive()
    {
        if (_deathGo)
        {
            _deathGo.SetActive(false);
            _jumpScareGo.SetActive(true);
        }
    }

    private void OnPlayerDeath()
    {
        if (_deathGo) _deathGo.SetActive(true);
    }
}