using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;

public sealed class PlayerLight : NetworkBehaviour
{
    [SerializeField] private Light _light = null;
    [Space]
    [SerializeField] private float _onIntensity = 4.0f;
    [SerializeField] private float _turnOnDur = 0.5f;
    [SerializeField] private Ease _turnOnEase = Ease.OutBack;

    [Space]
    [SerializeField] private float _offIntensity = .25f;
    [SerializeField] private float _turnOffDur = 0.5f;
    [SerializeField] private Ease _turnOffEase = Ease.InBack;


    private NetworkVariable<bool> _isLit = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public bool IsLit { get { return _isLit.Value; } }
    

    public void SetLit(bool lit)
    {
        if (!IsSpawned || !IsOwner) return;
        if (_isLit.Value == lit) return;

        _isLit.Value = lit;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _isLit.OnValueChanged += OnLitValueChanged;
    }

    private void OnLitValueChanged(bool previousValue, bool isOn)
    {
        if (!_light) return;

        float targetInt = isOn ? _onIntensity : _offIntensity;
        float targetDur = isOn ? _turnOnDur : _turnOffDur;
        Ease targetEase = isOn ? _turnOnEase : _turnOffEase;

        _light.DOIntensity(targetInt, targetDur).SetEase(targetEase);
    }
}