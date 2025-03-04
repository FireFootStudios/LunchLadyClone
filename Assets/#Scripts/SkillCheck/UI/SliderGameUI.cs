using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SliderGameUI : MonoBehaviour
{
    [SerializeField] private GameObject _visuals = null;
    [SerializeField] private List<SkillCheckSlider> _sliders = null;


    private SliderGame _sliderGame = null;
    private SkillCheckSlider _currentSlider = null;


    private void Awake()
    {
        SliderGame.OnStarted += OnGameStarted;
        SliderGame.OnEnded += OnGameEnded;

        InputManager.Instance.Controls.Player.Jump.performed += OnPlayerInput;
    }

    private void OnEnable()
    {
        if (_visuals) _visuals.SetActive(false);

        foreach (SkillCheckSlider slider in _sliders)
            slider.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        SliderGame.OnStarted -= OnGameStarted;
        SliderGame.OnEnded -= OnGameEnded;

        InputManager.Instance.Controls.Player.Jump.performed -= OnPlayerInput;
    }

    private void OnGameStarted(SliderGame game)
    {
        _sliderGame = game;

        // Enable visuals
        if (_visuals) _visuals.SetActive(true);

        // Reset sliders
        foreach (SkillCheckSlider slider in _sliders)
            slider.gameObject.SetActive(false);

        // Start slider
        _currentSlider = _sliders.RandomElement();
        if (_currentSlider)
        {
            _currentSlider.gameObject.SetActive(true);
            _currentSlider.Resume();
        } 
    }

    private void OnGameEnded(SliderGame obj)
    {
        if (_visuals) _visuals.SetActive(false);
    }

    private void OnPlayerInput(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (!_sliderGame || !_sliderGame.InProgress) return;
        if (!_sliderGame.CanEnd()) return;

        if (!_currentSlider)
        {
            _sliderGame.ForceEnd(true, 1.0f);
            return;
        }

        _currentSlider.Stop();
        bool succes = _currentSlider.InSuccesZone();
        _sliderGame.ForceEnd(succes, .5f);
    }
}