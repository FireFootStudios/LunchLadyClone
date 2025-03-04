using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillCheckSlider : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 10.0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [Space]
    [SerializeField] private RectTransform _moveT = null;
    [SerializeField] private Image _backgroundImage = null;
    [SerializeField] private Image _succesZoneImage = null;
    [SerializeField] private Image _moveDotImage = null;

    private Tween _currentMoveTween = null;
    
    public bool IsStopped { get; private set; }

    public void Stop()
    {
        IsStopped = true;
        if (_currentMoveTween != null) _currentMoveTween.Kill();
    }

    public void Resume()
    {
        IsStopped = false;
    }

    public bool InSuccesZone()
    {
        float dotX = _moveT.localPosition.x;
        float successMinX = _succesZoneImage.rectTransform.localPosition.x - _succesZoneImage.rectTransform.rect.width / 2;
        float successMaxX = _succesZoneImage.rectTransform.localPosition.x + _succesZoneImage.rectTransform.rect.width / 2;

        // Outside of bounds?
        if (dotX < successMinX || dotX > successMaxX) 
            return false;

        return true;
    }

    private void Update()
    {
        UpdateMove();
    }

    private void UpdateMove()
    {
        if (IsStopped) return;
        if (!_moveT) return;
        if (_currentMoveTween != null && _currentMoveTween.active) return;

        float barWidth = _backgroundImage.rectTransform.rect.width;
        float dotWidth = _moveDotImage.rectTransform.rect.width;
        float minX = -barWidth / 2 + dotWidth / 2;
        float maxX = barWidth / 2 - dotWidth / 2;

        // Reset pos
        _moveT.localPosition = new Vector3(minX, _moveT.localPosition.y, _moveT.localPosition.z);

        // Start looping tween
        _currentMoveTween = _moveT.DOAnchorPosX(maxX, 1.0f / _moveSpeed)
                         .SetEase(_ease)
                         .SetLoops(-1, LoopType.Yoyo);
    }
}