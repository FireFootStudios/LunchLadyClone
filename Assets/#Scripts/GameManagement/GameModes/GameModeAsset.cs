using UnityEngine;

[CreateAssetMenu(fileName = "GameModeAssetX", menuName = "ScriptableObjects/GameModeAsset", order = 1)]
public sealed class GameModeAsset : ScriptableObject
{
    [SerializeField, Tooltip("Name for displaying")] private string _name = "";
    [SerializeField] private string _nameID = null;

    [Space]
    [SerializeField, Tooltip("If false, features like leaderboards and scores will not work for this gamemode")] private bool _isLegitimate = false;
    [SerializeField] private bool _skipPreplayState = false;
    [SerializeField] private bool _disableIntroMessage = false;
    [SerializeField] private bool _disableCutScenes = false;
    [SerializeField] private bool _saveOnSessionEnd = false;

    [Space]
    [SerializeField] private bool _allowCheckpoints = false;
    [SerializeField, Tooltip("If true, only the highest position will be saved for a session")] private bool _saveHighestPointOnly = true;



    public string DisplayName { get { return _name; } }
    public string NameID {  get { return _nameID; } }

    public bool IsLegitimate { get { return _isLegitimate; } }
    public bool SkipReplayState { get { return _skipPreplayState; } }
    public bool DisableIntroMessage { get { return _disableIntroMessage; } }
    public bool AllowCheckpoints { get { return _allowCheckpoints; } }
    public bool DisableCutScenes { get { return _disableCutScenes; } }
    public bool SaveHighestPointOnly { get { return _saveHighestPointOnly; } }
    public bool SaveOnSessionEnd { get { return _saveOnSessionEnd; } }
}