using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image _fillImage = null;

    private PlayerN _localPlayer = null;
    private Sprint _localPlayerSprint = null;

    private void OnEnable()
    {

    }

    private void Update()
    {
        if (!_fillImage) return;

        // Init if not yet
        if (!_localPlayerSprint)
        {
            _localPlayer = GameManager.Instance.SceneData.LocalPlayer;
        
            if (_localPlayer)
                _localPlayerSprint = _localPlayer.GetComponentInChildren<Sprint>();
        }

        if (!_localPlayerSprint) return;

        // Update fill
        _fillImage.fillAmount = _localPlayerSprint.StaminaPercentage;
    }
}