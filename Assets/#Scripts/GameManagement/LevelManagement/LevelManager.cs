using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelManager : SingletonBase<LevelManager>
{
    [SerializeField] private List<LevelAsset> _levelAssets = new List<LevelAsset>();

    [Space]
    [SerializeField] private LevelAsset _homeLevelAsset = null;
    [SerializeField] private LevelAsset _menuLevelAsset = null;
    [SerializeField] private LevelAsset _testLevelAsset = null;


    //Runtime wrappers for levels containing the level (save) data as well
    private List<Level> _levels = new List<Level>();
    private Level _menuLevel = null;
    private Level _testLevel = null;
    private Level _homeLevel = null;

    //List used for returning the levels in a group 
    private List<Level> _groupLevels = new List<Level>();

    //Are all runtime level wrappers created and synced up with save system?
    public bool Initialized { get; private set; }

    public List<Level> Levels { get { return _levels; } } // Does not include home, menu or test level asset (those can be accesed individually)

    public Level HomeLevel { get { return _homeLevel; } }
    public Level MenuLevel { get { return _menuLevel; } }
    public Level TestLevel { get { return _testLevel; } }


    //Invoked when synced with saveManager.onload
    public Action OnInitialized;

    #region Public Functions

    public Level FindCurrentLevel()
    {
        // In case level wrappers have not been created yet
        InitLevels();

        string activeSceneName = SceneManager.GetActiveScene().name;

        // Check if home or menu first
        if (_homeLevel != null && activeSceneName == _homeLevel.Asset.SceneName) return _homeLevel;
        if (_menuLevel != null && activeSceneName == _menuLevel.Asset.SceneName) return _menuLevel;
     
        // Find level with scenename equal to active scene name
        foreach (Level level in _levels)
        {
            if (level.Asset.SceneName == activeSceneName)
                return level;
        }

        Level prevLevel = null;
        foreach (LevelAsset levelAsset in _levelAssets)
        {
            if (levelAsset.SceneName == activeSceneName)
            {
                Level level = new Level(levelAsset, GameManager.Instance.GameModes);

                // Figure out where to insert level (to maintain order)
                int insertIndex = 0;
                if (prevLevel != null) insertIndex = _levels.IndexOf(level);

                _levels.Insert(insertIndex, level);

                return level;
            }
        }

        // If no level found, return the test level (this is to make sure we always have a valid level + asset to return)
        return _testLevel;
    }

    public Level FindNextLevel()
    {
        Level current = FindCurrentLevel();

        // find level index belonging to next level
        int levelIndex = _levels.IndexOf(current);

        //Is this last level?
        if (levelIndex >= _levels.Count - 1) return null;
        else levelIndex++;

        return _levels[levelIndex];
    }

    public bool IsLevelLocked(LevelAsset levelAsset)
    {
        if (!Initialized || levelAsset == null) return true;

        //Find level containing asset
        Level level = _levels.Find(l => l.Asset == levelAsset);

        //return false if not found
        if (level == null) return false;

        //Get level index
        int levelIndex = _levels.IndexOf(level);

        //if previous level, check if that one has not been completed yet (has a session result)
        if (levelIndex > 0) return !_levels[levelIndex - 1].IsCompleted();
        else return false;
    }

    public void UnlockAll()
    {
        //foreach (Level level in _levels)
        //{
        //    if (level.IsCompleted()) continue;

        //    //Create a 'fake' session result and thus unlock next level
        //    SessionData data = new SessionData()
        //    {
        //        levelSceneName = level.Asset.SceneName,
        //        //gamemodeID = GameManager.Instance.CurrentGameMode.Asset.NameID,
        //        time = 666666.0f,
        //    };

        //    level.CurrentData.bestSessionData = data;
        //}
    }

    public Level GetLevel(string scenename)
    {
        if (!Initialized) return null;

        Level level = null;

        if (_homeLevel != null && _homeLevel.Asset.SceneName == scenename) return _homeLevel;
        else if (_menuLevel != null && _menuLevel.Asset.SceneName == scenename) return _menuLevel;

        //Try find level
        level = Levels.Find(level => level.Asset.SceneName == scenename);

        //Use test level if none found
        if (level == null && _testLevel != null) return _testLevel;

        return level;
    }

    public Level GetLevel(LevelAsset asset)
    {
        if (!Initialized) return null;

        Level level = null;

        if (_homeLevel != null && _homeLevel.Asset == asset) return _homeLevel;
        else if (_menuLevel != null && _menuLevel.Asset == asset) return _menuLevel;
        else if (_testLevel != null && _testLevel.Asset == asset) return _testLevel;
        else level = Levels.Find(level => level.Asset == asset);

        return level;
    }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        if (_isDestroying) return;

        InitLevels();

        GameMode.OnSessionEnd += OnSessionEnd;
    }

    #region PrivateFunctions

    private void InitLevels()
    {
        List<GameMode> gamemodes = GameManager.Instance.GameModes;
        if (gamemodes.Count == 0)
        {
            Debug.LogError("Cant create level wrappers without any gamemodes linked to gamemanager!");
            return;
        }

        // Create a runtime level with empty data for all level assets and gamemodes
        foreach (LevelAsset levelAsset in _levelAssets)
        {
            // Is there a level wrapper for this asset already?
            if (_levels.Exists(l => l.Asset == levelAsset))
            {
                //for (int i = 0; i < levelAsset.leve.Count; i++)
                //{
                //    LevelData level
                //}
                continue;
            }

            Level level = new Level(levelAsset, gamemodes);
            _levels.Add(level);
        }

        // Also create and cache a level for the home, menu and test scenes, make sure not to add these to the levels list
        if (_homeLevelAsset && _homeLevel == null) _homeLevel = new Level(_homeLevelAsset, gamemodes);
        if (_menuLevelAsset && _menuLevel == null) _menuLevel = new Level(_menuLevelAsset, gamemodes);
        if (_testLevelAsset && _testLevel == null) _testLevel = new Level(_testLevelAsset, gamemodes);
    }

    private void OnSessionEnd(SessionInfo info)
    {
        if (info.ValidComplete) info.Level.CurrentData.completedAmount++;
        info.Level.CurrentData.playedDuration += info.Data.time;
    }
    #endregion
}