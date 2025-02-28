using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ChaseStart : MonoBehaviour
{
    [SerializeField] private GameObject _whaleGO = null;

    private Transform _whaleSpawnT = null;
    private OldInventory _playerInventory = null;

    private void Start()
    {
        _whaleSpawnT = GameObject.Find("WhaleSpawnT").transform;
        _playerInventory = FindFirstObjectByType<OldInventory>();
    }

    // Update is called once per frame
    void Update()
    {
        SpawnMonster();
    }

    private void SpawnMonster()
    {
        if (Input.GetKeyUp(KeyCode.C) && _whaleSpawnT != null)
        {
            Instantiate(_whaleGO, _whaleSpawnT);
        }
    }
}
