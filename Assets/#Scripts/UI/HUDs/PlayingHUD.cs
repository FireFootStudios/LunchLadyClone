using TMPro;
using UnityEngine;

public sealed class PlayingHUD : MonoBehaviour
{
    [SerializeField] private GameObject _deathGo = null;


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
        if (_deathGo) _deathGo.SetActive(false);
    }

    private void OnPlayerDeath()
    {
        if (_deathGo) _deathGo.SetActive(true);
    }
}