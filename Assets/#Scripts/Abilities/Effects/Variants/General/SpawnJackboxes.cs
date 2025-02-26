using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnJackboxes : Effect
{
    [Header("General")]
    [SerializeField] private JackBox _spawnTemplate = null;
    [SerializeField] private Transform _spawnT = null;
    [SerializeField] private int _maxAliveSpawns = 10;

    private List<JackBox> _currentJackboxes = new List<JackBox>();


    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        for (int i = 0; i < _currentJackboxes.Count; i++)
        {
            JackBox jackBox = _currentJackboxes[i];
            if (jackBox == null || jackBox.Health.IsDead || !jackBox.gameObject.activeInHierarchy)
            {
                _currentJackboxes.RemoveAt(i);
                i--;

                // Destroy/despawn
                if (!jackBox.TryGetComponent(out NetworkObject networkObject)) return;

                networkObject.Despawn(true);
            }
        }
    }

    protected override void OnApply(GameObject target, Transform executerT, EffectModifiers effectMods)
    {
        if (!_spawnTemplate || !_spawnT) return;

        JackBox jackBox = Instantiate(_spawnTemplate, _spawnT.transform.position, _spawnT.transform.rotation);
        _currentJackboxes.Add(jackBox);

        if (!jackBox.TryGetComponent(out NetworkObject networkObject)) return;
        
        networkObject.Spawn(true);
    }

    protected override float Effectiveness(GameObject target)
    {
        if (_spawnTemplate == null || !_spawnT) return 0.0f;

        return 1.0f;
    }

    public override bool CanApply()
    {
        return _currentJackboxes.Count < _maxAliveSpawns;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}