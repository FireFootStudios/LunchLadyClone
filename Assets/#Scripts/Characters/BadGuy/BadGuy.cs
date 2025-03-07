using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BadGuy : Character
{
    [SerializeField] private List<GameObject> _hostOnlyGos = new List<GameObject>();
    [SerializeField] private List<MonoBehaviour> _hostOnlyComps = new List<MonoBehaviour>();
    [Space]
    [SerializeField] private bool _offlineMode = false;

    [Space]
    [SerializeField] private SoundSpawnData _aggroStartSFX = null;


    protected override void Awake()
    {
        base.Awake();

        if (!_offlineMode)
        {
            foreach (GameObject go in _hostOnlyGos)
                go.SetActive(false);

            foreach (MonoBehaviour comp in _hostOnlyComps)
                comp.enabled = false;

            if (_agent) _agent.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        foreach (GameObject go in _hostOnlyGos)
            go.SetActive(IsHost);

        if (_agent) _agent.enabled = IsHost;

        foreach (MonoBehaviour comp in _hostOnlyComps)
            comp.enabled = IsHost;

        if (IsHost)
        {
            Behaviour.OnAggroChange += OnAggroChange;
        }
    }

    protected virtual void OnAggroChange(bool isAggro)
    {
        // Tell clients aggro changed
        OnAggroChangeClientRpc(isAggro);
    }

    [ClientRpc]
    protected virtual void OnAggroChangeClientRpc(bool isAggro)
    {
        if (isAggro) SoundManager.Instance.PlaySound(_aggroStartSFX);
    }
}