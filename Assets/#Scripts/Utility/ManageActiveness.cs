using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public sealed class ManageActiveness : MonoBehaviour
{
    [SerializeField] private TriggerEvents _triggerEvents = null;
    [Space]
    [SerializeField] private List<GameObject> _managedGameobjects = new List<GameObject>();
    [SerializeField] private List<ParticleSystem> _particles = new List<ParticleSystem>();
    [SerializeField] private List<ParticleSystem> _particlesNoChildPlay = new List<ParticleSystem>();

    [Space]
    [SerializeField] private bool _searchCharacters = true;
    [SerializeField, ReadOnly(true)] private List<Character> _characters = new List<Character>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_searchCharacters)
        {
            _searchCharacters = false;

            _characters.Clear();
            _characters.AddRange(GetComponentsInChildren<Character>(false));

            //is required for it to be saveable as a prefab?
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        _triggerEvents.OnEnter += OnEnter;
        _triggerEvents.OnExit += OnExit;

        // Characters are automatically updated and added
        _characters.Clear();
        _characters.AddRange(GetComponentsInChildren<Character>(false));

        // Start false
        SetActive(false);
    }

    private void OnEnter(Collider obj)
    {
        SetActive(true);
    }

    private void OnExit(Collider obj)
    {
        SetActive(false);
    }

    private void SetActive(bool active)
    {
        foreach (GameObject go in _managedGameobjects)
            go.SetActive(active);

        foreach (ParticleSystem ps in _particles)
        {
            if (active) ps.Play();
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }


        foreach (ParticleSystem ps in _particlesNoChildPlay)
        {
            if (active) ps.Play(false);
            else ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        foreach (Character character in _characters)
            character.gameObject.SetActive(active);
    }
}