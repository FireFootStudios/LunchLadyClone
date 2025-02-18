using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargettingEffect : Effect
{
    //[SerializeField] private float _applyEffectsDelay = .00f;

    private List<GameObject> _targets = new List<GameObject>();

    public List<GameObject> Targets { get { return _targets; } }
    //public float ApplyEffectsDelay { get { return _applyEffectsDelay; } }


    //Event for telling the managing execute that targets are ready, the execute will then apply all other effects on the targets in our list
    public Action OnTargetsReady;
}
