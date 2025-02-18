using System;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private List<Tag> _targetTags = new List<Tag>();
    [SerializeField] private List<PhysicsEvents> _collEvents = new List<PhysicsEvents> ();

    private List<Collider> _colliders = new List<Collider>();

    //Tracks per target how many colliders it currently is in
    private Dictionary<GameObject, HashSet<GameObject>> _targetSources = new Dictionary<GameObject, HashSet<GameObject>>(); //Target, HashSet of sources


    public List<Tag> TargetTags { get { return _targetTags; } set { _targetTags = value; } }
    public List<Collider> Colliders { get { return _colliders; } } // All colliders linked to this hitbox
    public List<GameObject> Targets { get; private set; } = new List<GameObject>();


    public Action<Collider> OnTargetEnter;
    public Action<Collider> OnTargetExit;
    public Action<Collider> OnFirstTargetEnter; // invoked when 1 or more targets added when no targets before
    public Action OnTargetsChange;


    private void Awake()
    {
        //For now we assume that each physics event only has 1 collider tho, otherwise issue could arise
        foreach (PhysicsEvents pEvent in _collEvents)
        {
            pEvent.OnEnter += (Collider) => AddTarget(Collider, pEvent.gameObject);
            pEvent.OnExit += (Collider) => RemoveTarget(Collider, pEvent.gameObject); ;

            //Add colliders from physicsevent
            _colliders.Add(pEvent.GetComponent<Collider>());
        }

        //Add colliders on this gameobject
        _colliders.AddRange(GetComponents<Collider>());
    }

    private void OnDisable()
    {
        //Clear targets cuz physics
        Targets.Clear();
    }

    private void Update()
    {
        UpdateTargets();
    }

    private void UpdateTargets()
    {
        if (Targets.Count == 0) return;

        // Check if any of targets in list is null(due to deletion), if so remove
        for (int i = 0; i < Targets.Count; i++)
        {
            GameObject target = Targets[i];

            // If target null, remove it an move on
            if (!target || !target.activeInHierarchy)
            {
                Targets.RemoveAt(i);
                i--;

                OnTargetsChange?.Invoke();

                // Remove all sources
                if (target) _targetSources[target].Clear();
                continue;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AddTarget(other, this.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveTarget(other, this.gameObject);
    }

    private void AddTarget(Collider target, GameObject source)
    {
        if (!target || !source || !ValidTag(target.tag)) return;

        // If new target, create new pair
        if (!_targetSources.ContainsKey(target.gameObject))
        {
            _targetSources.Add(target.gameObject, new HashSet<GameObject>() );
        }

        //Add new source to hashset
        _targetSources[target.gameObject].Add(source);

        //If first collider call on enter
        if (_targetSources[target.gameObject].Count == 1)
        {
            //add target
            Targets.Add(target.gameObject);
            OnTargetEnter?.Invoke(target);
            if (Targets.Count == 1) OnFirstTargetEnter?.Invoke(target);
            OnTargetsChange?.Invoke();
        }
    }

    //private void RemoveTarget(Collider target, GameObject source) { RemoveTarget(target.gameObject, source); }

    private void RemoveTarget(Collider target, GameObject source)
    {
        if (!_targetSources.ContainsKey(target.gameObject))
        {
            //In case it would still be in the list
            Targets.Remove(target.gameObject);
            return;
        }

        //Remove the source from target entry
        _targetSources[target.gameObject].Remove(source);

        //If no more sources, remove as target
        if (_targetSources[target.gameObject].Count == 0)
        {
            Targets.Remove(target.gameObject);
            OnTargetExit?.Invoke(target);
            OnTargetsChange?.Invoke();
        }
    }

    private bool ValidTag(string tagStr)
    {
        if (_targetTags.Count == 0) return true;
        return _targetTags.Exists(tag => TagManager.Instance.GetTagValue(tag) == tagStr);
    }
}