using UnityEngine;
using DG.Tweening;

public sealed class HeadBob : MonoBehaviour
{
    [SerializeField] private PlayerCameras _playerCameras = null;
    [SerializeField] private Transform _targetT = null;
    [Space]
    [SerializeField] private Vector2 _velScalBounds = new Vector2(.5f, 6.0f);
    [SerializeField] private Vector2 _bobAmountBounds = new Vector2(0.0f, 0.5f);
    [SerializeField] private Vector2 _bobDurBounds = new Vector2(1.0f, 0.25f);

    [Space]
    [SerializeField] private Ease _ease = Ease.InOutBounce;

    private Tween _currentBobTween = null;

    private void Update()
    {
        if (!_playerCameras || !_playerCameras.Player || !_targetT) return;

        float playerSpeed = _playerCameras.Player.Movement.CurrentMoveSpeed;

        // Check if speed is above the minimum threshold
        if (playerSpeed > _velScalBounds.x)
        {
            // If no tween is active, start a new one
            if (_currentBobTween == null || !_currentBobTween.IsActive())
            {
                StartHeadBob(playerSpeed);
            }
        }
        else
        {
            // If speed is too low, stop tween and reset position
            //_currentBobTween?.Kill();
            //_currentBobTween = null;
            //_targetT.localPosition = Vector3.zero;
        }
    }

    private void StartHeadBob(float speed)
    {
        // Calculate bob intensity based on speed
        float t = Mathf.InverseLerp(_velScalBounds.x, _velScalBounds.y, speed);
        float bobAmount = Mathf.Lerp(_bobAmountBounds.x, _bobAmountBounds.y, t);
        float bobDuration = Mathf.Lerp(_bobDurBounds.x, _bobDurBounds.y, t);

        _currentBobTween = _targetT.DOPunchPosition(Vector3.up * bobAmount, bobDuration, 0)
            .SetEase(_ease);
    }
}