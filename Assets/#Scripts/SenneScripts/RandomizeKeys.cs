using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Random = UnityEngine.Random;

public class RandomizeKeys : NetworkBehaviour
{
    [SerializeField] private List<GameObject> _normalObjectsSOne = null;
    [SerializeField] private List<GameObject> _normalObjectsSOneTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _normalObjectsSTwo = null;
    [SerializeField] private List<GameObject> _normalObjectsSTwoTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _normalObjectsSThree = null;
    [SerializeField] private List<GameObject> _normalObjectsSThreeTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSOne = null;
    [SerializeField] private List<GameObject> _keysSOneTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSTwo = null;
    [SerializeField] private List<GameObject> _keysSTwoTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSThree = null;
    [SerializeField] private List<GameObject> _keysSThreeTrans = null;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsHost)
            return;

        RandomizeObjectPositions(_normalObjectsSOne, _normalObjectsSOneTrans);
        RandomizeObjectPositions(_normalObjectsSTwo, _normalObjectsSTwoTrans);
        RandomizeObjectPositions(_normalObjectsSThree, _normalObjectsSThreeTrans);
        RandomizeObjectPositions(_keysSOne, _keysSOneTrans);
        RandomizeObjectPositions(_keysSTwo, _keysSTwoTrans);
        RandomizeObjectPositions(_keysSThree, _keysSThreeTrans);

    }

    private void RandomizeObjectPositions(List<GameObject> objects, List<GameObject> transforms)
    {
        if (objects == null || transforms == null || objects.Count == 0 || transforms.Count == 0)
        {
            Debug.LogWarning("One of the lists is null or empty. Skipping randomization.");
            return;
        }

        List<GameObject> shuffledTransforms = transforms.OrderBy(t => Random.value).ToList();

        for (int i = 0; i < objects.Count; i++)
        {
            if (i >= shuffledTransforms.Count)
            {
                Debug.LogWarning("Not enough transforms for all objects. Skipping remaining objects.");
                break;
            }

            objects[i].transform.position = shuffledTransforms[i].transform.position;
        }
    }
}
