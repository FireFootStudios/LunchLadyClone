using System.Collections.Generic;
using UnityEngine;

public sealed class PredeterminedTargetSystem : TargetSystem
{
    [Space]
    [SerializeField] private List<GameObject> _targets = new List<GameObject>(); 
    [SerializeField] private bool _random = false;

    protected override void UpdateCurrentTargetPairs()
    {
        //check if target null or has 0 effectiveness, remove if any true
        for (int i = 0; i < _targetPairs.Count; i++)
        {
            TargetPair targetPair = _targetPairs[i];
            if (!targetPair.target) _targetPairs.Remove(targetPair);
            else if (!_random)
            {
                //recalculate effectiveness
                targetPair.effectiveness = EffectivenessTarget(targetPair.target);
                if (targetPair.effectiveness < _minEffectivenessForValid) _targetPairs.Remove(targetPair);
            }

            //if targetpair got removed, decrement index
            if (!_targetPairs.Contains(targetPair)) i--;
        }
    }

    protected override void PopulateTargetPairs()
    {
        float effectiveness = 0.0f;

        foreach (GameObject target in _targets)
        {
            if (!IsTargetValid(target)) continue;

            effectiveness = EffectivenessTarget(target);
            if (effectiveness >= _minEffectivenessForValid) _targetPairs.Add(new TargetPair(target, effectiveness));
        }

        //sort OR randomise list
        if (_targetPairs.Count > 1)
        {
            if (_random) _targetPairs.Shuffle();
            else
            {
                //sort targets based on caculated effectivenesses
                _targetPairs.Sort((a, b) =>
                {
                    return b.effectiveness.CompareTo(a.effectiveness);
                });
            }
        }
    }
}