using System;
using TMPro;
using UnityEngine;

public sealed class GamemodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _papersTMP = null;

    private MainGamemode _mainGM = null;


    private void Awake()
    {
        MainGamemode.OnPapersChangeClient += OnPapersChanged;
    }

    private void OnDestroy()
    {
        MainGamemode.OnPapersChangeClient -= OnPapersChanged;
    }

    private void OnEnable()
    {
        _mainGM = GameManager.Instance.CurrentGameMode as MainGamemode;
        if (!_mainGM) return;

        UpdateUI();
    }

    private void OnPapersChanged()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_papersTMP) _papersTMP.text = _mainGM.PapersCollected + "/" + _mainGM.PapersTotal;
    }
}