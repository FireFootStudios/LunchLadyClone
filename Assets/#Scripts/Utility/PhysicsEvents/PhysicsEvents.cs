using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class PhysicsEvents : MonoBehaviour
{
    [SerializeField, Tooltip("Leave empty to ignore tag checking")] private List<Tag> _targetTags = new List<Tag>();

    [Space]
    [SerializeField] protected bool _disableOnEnter = false;
    [SerializeField] protected bool _disableOnExit = false;
    [SerializeField] protected bool _disableOnStay = false;

    private TagManager _tagManager;
    //private List<Collider> _colliders = new List<Collider>();


    //public List<Collider> Colliders { get { return _colliders; } }

    //These are general events (mainly invoked by child classes)
    public Action<Collider> OnEnter;
    public Action<Collider> OnExit;
    public Action<Collider> OnStay;

    protected void Awake()
    {
        _tagManager = TagManager.Instance;

        //_colliders.AddRange(GetComponents<Collider>());
    }

    //protected virtual void OnDisable()
    //{
    //    if (!_disableOnExit) OnExit?.Invoke(null);
    //}

    protected bool ValidTag(string tagStr)
    {
        if (_targetTags.Count == 0) return true;
        return _targetTags.Exists(tag => _tagManager.GetTagValue(tag) == tagStr);
    }
}