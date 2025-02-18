using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level69", menuName = "ScriptableObjects/LevelAsset", order = 1)]
public sealed class LevelAsset : ScriptableObject
{
    [SerializeField, Tooltip("How should the level be referenced in UI")] private string _levelName = "NotNamed";
    [SerializeField, Tooltip("What is the actual scene name belonging to this level")] private string _sceneName = "AlsoNotNamed...";

    [Space]
    [SerializeField, Tooltip("Is this a playable level (set false for menus)?")] private bool _isPlayableLevel = true;
    //[SerializeField] private bool _createLeaderboard = false;


    [Space]
    [SerializeField] private List<Sprite> _backgroundSpritesLoadingView = new List<Sprite>();
    [SerializeField] private List<Sprite> _backgroundSpritesEndView = new List<Sprite>();

    public string LevelName { get { return _levelName; } }
    public string SceneName { get { return _sceneName; } }
    
    public bool IsPlayableLevel {  get { return _isPlayableLevel; } }
    //public bool CreateLeaderboard { get { return _createLeaderboard; } }


    public List<Sprite> LoadingViewBackgroundSprites { get { return _backgroundSpritesLoadingView; } }
    public List<Sprite> EndViewBackgroundSprites { get { return _backgroundSpritesEndView; } }

}