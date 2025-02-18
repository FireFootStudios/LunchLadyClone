using System;
using System.Collections.Generic;
using UnityEngine;


public sealed class UniqueTargetting : MonoBehaviour
{
    [SerializeField, Tooltip("Targets will be set as override in the linked target system")] private TargetSystem _targetSystem = null;
    [SerializeField] private Spawner _spawner = null;
    [SerializeField] private bool _resetAggroTagsOnRespawn = false;

    [Header("Target On Damage")]
    [SerializeField] private Health _health = null;
    [SerializeField] private int _damageAmount = 2;


    //On dialogue
    //...

    private List<Tag> _originalTags = new List<Tag>();

    private float _trackedDamage = 0.0f;


    private void Awake()
    {
        if (_health) _health.OnDamaged += OnDamaged;
        if (_spawner) _spawner.OnRespawn += OnRespawn;

        //Cache original tags in seperate list
        if (_targetSystem)
            _originalTags.AddRange(_targetSystem.TargetTags);
    }

    private void OnRespawn()
    {
        _trackedDamage = 0.0f;

        //Reset tags again (keep original list)
        if (_resetAggroTagsOnRespawn)
        {
            _targetSystem.TargetTags.Clear();
            _targetSystem.TargetTags.AddRange(_originalTags);
        }
    }

    private void OnDamaged(float amount, GameObject source)
    {
        if (!_targetSystem || !source) return;

        _trackedDamage += amount;
        if (_trackedDamage < _damageAmount) return;

        Tag tag = TagManager.Instance.GetTagID(source.tag);
        if (tag == Tag.Invalid || _targetSystem.TargetTags.Contains(tag)) return;

        _targetSystem.TargetTags.Add(tag);

        //Try set the override target on the linked target system, this will automatically check if it is a valid target or not
        //_targetSystem.OverrideTarget = source;
    }
}