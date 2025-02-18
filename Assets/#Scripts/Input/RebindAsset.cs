using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "RebindX", menuName = "ScriptableObjects/RebindAsset", order = 1)]
public sealed class RebindAsset : ScriptableObject
{
    [SerializeField] private string _displayName = null;
    [Space]
    [SerializeField] private InputActionReference _inputActionRef = null;
    [SerializeField] private bool _excludeMouse = true;

    [Space]
    [SerializeField, Range(0, 6)] private int _desiredBinding = 0;
    [SerializeField, ReadOnly(true)] private InputBinding _selectedInputBinding = default;

    private int _selectedBindingIndex = 0;

    public string DisplayName { get { return _displayName; } }
    public string ActionName { get { return _inputActionRef != null ? _inputActionRef.action.name : null; } }
    public bool IsValid { get { return _inputActionRef != null && _selectedBindingIndex < _inputActionRef.action.bindings.Count; } }

    public InputActionReference InputActionRef { get {  return _inputActionRef; } }
    public bool ExcludeMouse { get { return _excludeMouse; } }
    public int BindingIndex {  get { return _selectedBindingIndex; } }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateBindingIndex();
    }
#endif

    private void Awake()
    {
        UpdateBindingIndex();
    }

    private void UpdateBindingIndex()
    {
        if (_inputActionRef == null) return;

        //If binding exists, set as selected 
        if (_inputActionRef.action.bindings.Count > _desiredBinding)
        {
            _selectedBindingIndex = _desiredBinding;
            _selectedInputBinding = _inputActionRef.action.bindings[_selectedBindingIndex];
        }
        else _desiredBinding = _selectedBindingIndex;
    }
}