using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(HitBox))]
public sealed class HitBoxTargetSystem : TargetSystem
{
    [Space]
    [SerializeField] private List<HitBox> _hitboxes = null;

    public List<HitBox> Hitbox { get { return _hitboxes; } }
    //{
    //    get { return _hitbox; }
    //    set
    //    {
    //        if (!value) return;
    //        _hitbox = value;
    //        //Prob should reupdate targetsystem but idk rn
    //    }
    //}

    protected override void Awake()
    {
        if (_hitboxes.Count == 0)
        {
            HitBox hitbox = GetComponent<HitBox>();
            if (hitbox) _hitboxes.Add(hitbox);
        }

        base.Awake();
    }

    //protected override void UpdateCurrentTargetPairs()
    //{
    //    GameObject source = Source;

    //    // Check if target null or has 0 effectiveness, remove if any true
    //    for (int i = 0; i < _targetPairs.Count; i++)
    //    {
    //        TargetPair targetPair = _targetPairs[i];
    //        if (!targetPair.target) _targetPairs.Remove(targetPair);
    //        else if (_requireOverrideToBeInHitBox && !_hitboxes.Exists(hitBox => hitBox.Targets.Contains(targetPair.target) && targetPair.target != source)) _targetPairs.Remove(targetPair);
    //        else
    //        {
    //            // Recalculate effectiveness
    //            targetPair.effectiveness = EffectivenessTarget(targetPair.target);
    //            if (targetPair.effectiveness < _minEffectivenessForValid)
    //            {
    //                _targetPairs.Remove(targetPair);
    //            }
    //        }

    //        // If targetpair got removed, decrement index
    //        if (!_targetPairs.Contains(targetPair)) i--;
    //    }
    //}

    protected override void PopulateTargetPairs(ref List<TargetPair> targets)
    {
        if (_hitboxes.Count == 0) return;

        // Is override in hitbox?
        //bool overrideInHitbox = OverrideTarget && _hitboxes.Exists(hitBox => hitBox.Targets.Contains(OverrideTarget));

        // Check if ignore others on override and if so return
        //if (OverrideTarget && !overrideInHitbox && _requireOverrideToBeInHitBox && _ignoreOthersOnOverride) return;

        // Update target pairs with targets in hitbox
        foreach (HitBox hitbox in _hitboxes)
        {
            if (hitbox.Targets.Count == 0) continue;

            // Calculate effectiveness of every target
            for (int i = 0; i < hitbox.Targets.Count; i++)
            {
                GameObject target = hitbox.Targets[i];

                // Skip if override target (will be added later)
                //if (target == OverrideTarget) continue;

                if (!IsTargetValid(target)) continue;

                // Calc effectiveness and add to targetpairs if enough
                float effectiveness = EffectivenessTarget(target);
                if (effectiveness < _minEffectivenessForValid) continue;

                targets.Add(new TargetPair(target, effectiveness, DefaultTargetLifeTime));
            }
        }

        // Add self
        GameObject source = Source;
        if (_canIncludeSelf && source && IsTargetValid(source))
        {
            float effectiveness = EffectivenessTarget(source);
            if (effectiveness >= _minEffectivenessForValid) targets.Add(new TargetPair(source, effectiveness, DefaultTargetLifeTime));
        }
    }
}
