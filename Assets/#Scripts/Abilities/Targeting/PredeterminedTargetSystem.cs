using System.Collections.Generic;
using UnityEngine;

public sealed class PredeterminedTargetSystem : TargetSystem
{
    [Space]
    [SerializeField] private List<GameObject> _targets = new List<GameObject>(); 

    protected override void UpdateCurrentTargetPairs()
    {
        //check if target null or has 0 effectiveness, remove if any true
        //for (int i = 0; i < _targetPairs.Count; i++)
        //{
        //    TargetPair targetPair = _targetPairs[i];
        //    if (!targetPair.target) _targetPairs.Remove(targetPair);
        //    else
        //    {
        //        //recalculate effectiveness
        //        targetPair.effectiveness = EffectivenessTarget(targetPair.target);
        //        if (targetPair.effectiveness < _minEffectivenessForValid) _targetPairs.Remove(targetPair);
        //    }

        //    //if targetpair got removed, decrement index
        //    if (!_targetPairs.Contains(targetPair)) i--;
        //}
    }

    protected override void PopulateTargetPairs(ref List<TargetPair> targets)
    {
        //float effectiveness = 0.0f;

        //foreach (GameObject target in _targets)
        //{
        //    if (!IsTargetValid(target)) continue;

        //    effectiveness = EffectivenessTarget(target);
        //    if (effectiveness >= _minEffectivenessForValid) targets.Add(new TargetPair(target, effectiveness));
        //}
    }
}