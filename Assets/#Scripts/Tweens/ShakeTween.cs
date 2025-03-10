using DG.Tweening;
using System;
using UnityEngine;

public sealed class ShakeTween : MonoBehaviour
{
    [SerializeField] private Transform _targetT = null;

    [Space]
    [SerializeField] private Health _onDamage = null;

    [Space]
    [SerializeField] private Vector3 _strength = Vector3.zero;
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private int _vibrato = 10;
    [SerializeField] private Ease _ease = Ease.Linear;
    [SerializeField] private float _overshoot = 0.5f;
    [SerializeField] private bool _fadeOut = false;
    [SerializeField] private float _randomness = 90.0f;
    [SerializeField] private ShakeRandomnessMode _randomnessMod = ShakeRandomnessMode.Full;
    [SerializeField] private bool _snapping = false;

    //[SerializeField] private Interactible _onInteract = null;

    private void Awake()
    {
        if (_onDamage) _onDamage.OnDamaged += OnDamage;
    }

    private void OnDamage(float arg1, GameObject arg2)
    {
        if (!_targetT) return;

        _targetT.DOShakePosition(_duration, _strength, _vibrato, _randomness, _snapping, _fadeOut, _randomnessMod)
            .SetEase(_ease, _overshoot);
    }
}