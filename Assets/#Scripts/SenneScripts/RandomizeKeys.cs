using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Random = UnityEngine.Random;

public class RandomizeKeys : NetworkBehaviour
{
    [SerializeField] private List<GameObject> _paperOne = null;
    [SerializeField] private List<GameObject> _paperOneTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperTwo = null;
    [SerializeField] private List<GameObject> _paperTwoTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperThree = null;
    [SerializeField] private List<GameObject> _paperThreeTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperFour = null;
    [SerializeField] private List<GameObject> _paperFourTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperFive = null;
    [SerializeField] private List<GameObject> _paperFiveTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSix = null;
    [SerializeField] private List<GameObject> _paperSixTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperSeven = null;
    [SerializeField] private List<GameObject> _paperSevenTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperEight = null;
    [SerializeField] private List<GameObject> _paperEightTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperNine = null;
    [SerializeField] private List<GameObject> _paperNineTrans = null;
    [Space]
    [SerializeField] private List<GameObject> _paperTen = null;
    [SerializeField] private List<GameObject> _paperTenTrans = null;
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

        RandomizeObjectPositions(_paperOne, _paperOneTrans);
        RandomizeObjectPositions(_paperTwo, _paperTwoTrans);
        RandomizeObjectPositions(_paperThree, _paperThreeTrans);
        RandomizeObjectPositions(_paperFour, _paperFourTrans);
        RandomizeObjectPositions(_paperFive, _paperFiveTrans);
        RandomizeObjectPositions(_paperSix, _paperSixTrans);
        RandomizeObjectPositions(_paperSeven, _paperSevenTrans);
        RandomizeObjectPositions(_paperEight, _paperEightTrans);
        RandomizeObjectPositions(_paperNine, _paperNineTrans);
        RandomizeObjectPositions(_paperTen, _paperTenTrans);
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
            objects[i].transform.rotation = shuffledTransforms[i].transform.rotation;
        }
    }
}
