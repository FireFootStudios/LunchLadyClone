using System.Collections.Generic;
using UnityEngine;

public sealed class BadGuy : Character
{
    [SerializeField] private List<GameObject> _hostOnlyGos = new List<GameObject>();
    [SerializeField] private List<MonoBehaviour> _hostOnlyComps = new List<MonoBehaviour>();
    [Space]
    [SerializeField] private bool _offlineMode = false;

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
        // Behaviors indicating
        //if (!IsHost)
        //{
        //    Behaviour.enabled = false;
        //    NavMeshAgent.enabled = false;
        //    Movement.enabled = false;
        //    AttackBehaviour.enabled = false;
        //}
    }
}