using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Random = UnityEngine.Random;

public class RandomizeKeys : NetworkBehaviour
{
    [SerializeField] private List<GameObject> _paperSOne = null;
    [SerializeField] private List<GameObject> _paperSOneTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSTwo = null;
    [SerializeField] private List<GameObject> _paperSTwoTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSThree = null;
    [SerializeField] private List<GameObject> _paperSThreeTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSFour = null;
    [SerializeField] private List<GameObject> _paperSFourTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSFive = null;
    [SerializeField] private List<GameObject> _paperSFiveTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSOne = null;
    [SerializeField] private List<GameObject> _keysSOneTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSTwo = null;
    [SerializeField] private List<GameObject> _keysSTwoTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSThree = null;
    [SerializeField] private List<GameObject> _keysSThreeTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _keysSFour = null;
    [SerializeField] private List<GameObject> _keysSFourTrans = null;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsHost)
            return;

        RandomizeObjectPositions(_paperSOne, _paperSOneTrans);
        RandomizeObjectPositions(_paperSTwo, _paperSTwoTrans);
        RandomizeObjectPositions(_paperSThree, _paperSThreeTrans);
        RandomizeObjectPositions(_paperSFour, _paperSFourTrans);
        RandomizeObjectPositions(_paperSFive, _paperSFiveTrans);
        RandomizeObjectPositions(_keysSOne, _keysSOneTrans);
        RandomizeObjectPositions(_keysSTwo, _keysSTwoTrans);
        RandomizeObjectPositions(_keysSThree, _keysSThreeTrans);
        RandomizeObjectPositions(_keysSFour, _keysSFourTrans);

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
