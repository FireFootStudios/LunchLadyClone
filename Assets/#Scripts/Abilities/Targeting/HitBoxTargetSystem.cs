using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(HitBox))]
public sealed class HitBoxTargetSystem : TargetSystem
{
    [SerializeField] private bool _requireOverrideToBeInHitBox = true; //for abilities set to true, aggro to false
    [SerializeField] private bool _ignoreOthersOnOverride = true; //only ignores others as a first target

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

    protected override void UpdateCurrentTargetPairs()
    {
        GameObject source = Source;

        //check if target null or has 0 effectiveness, remove if any true
        for (int i = 0; i < _targetPairs.Count; i++)
        {
            TargetPair targetPair = _targetPairs[i];
            if (!targetPair.target) _targetPairs.Remove(targetPair);
            else if (_requireOverrideToBeInHitBox && !_hitboxes.Exists(hitBox => hitBox.Targets.Contains(targetPair.target) && targetPair.target != source)) _targetPairs.Remove(targetPair);
            else
            {
                //recalculate effectiveness
                targetPair.effectiveness = EffectivenessTarget(targetPair.target);
                if (targetPair.effectiveness < _minEffectivenessForValid)
                {
                    _targetPairs.Remove(targetPair);
                }
            }

            //if targetpair got removed, decrement index
            if (!_targetPairs.Contains(targetPair)) i--;
        }
    }

    protected override void PopulateTargetPairs()
    {
        if (_hitboxes.Count == 0) return;

        //is override in hitbox?
        bool overrideInHitbox = OverrideTarget && _hitboxes.Exists(hitBox => hitBox.Targets.Contains(OverrideTarget));

        //check if ignore others on override and if so return
        if (OverrideTarget && !overrideInHitbox && _requireOverrideToBeInHitBox && _ignoreOthersOnOverride) return;

        //update target pairs with targets in hitbox
        foreach (HitBox hitbox in _hitboxes)
        {
            if (hitbox.Targets.Count == 0) continue;

            //calculate effectiveness of every target
            for (int i = 0; i < hitbox.Targets.Count; i++)
            {
                GameObject target = hitbox.Targets[i];

                //skip if override target (will be added later)
                if (target == OverrideTarget) continue;

                if (!IsTargetValid(target)) continue;

                //calc effectiveness and add to targetpairs if enough
                float effectiveness = EffectivenessTarget(target);
                if (effectiveness < _minEffectivenessForValid) continue;

                _targetPairs.Add(new TargetPair(target, effectiveness));
            }
        }

        //add self
        GameObject source = Source;
        if (_canIncludeSelf && source && IsTargetValid(source))
        {
            float effectiveness = EffectivenessTarget(source);
            if (effectiveness >= _minEffectivenessForValid) _targetPairs.Add(new TargetPair(source, effectiveness));
        }

        //sort targets based on caculated effectivenesses
        if (_targetPairs.Count > 1)
        {
            _targetPairs.Sort((a, b) =>
            {
                return b.effectiveness.CompareTo(a.effectiveness);
            });
        }

        //calc effectiveness for override target, insert at beginning if at all effective
        if (OverrideTarget && (overrideInHitbox || !_requireOverrideToBeInHitBox))
        {
            float overrideEffectiveness = EffectivenessTarget(OverrideTarget);
            if (overrideEffectiveness >= _minEffectivenessForValid) _targetPairs.Insert(0, new TargetPair(OverrideTarget, 1.0f));
        }
    }
}
