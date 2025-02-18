using System.Collections.Generic;
using UnityEngine;

public sealed class Level
{
    private LevelAsset _levelAsset = null;
    private List<LevelData> _levelData = new List<LevelData>();


    // This data can be default/out of sync with save file (so make sure to check OnLoaded)
    // Returns the current level data for active gamemode
    public LevelData CurrentData { get { return GetData(GameManager.Instance.CurrentGameMode.Asset.NameID); } }

    public List<LevelData> LevelData { get { return _levelData; } set { _levelData = value; } }
    public LevelAsset Asset { get { return _levelAsset; } }



    public Level(LevelAsset levelAsset, List<GameMode> gamemodes)
    {
        _levelAsset = levelAsset;

        // Create a level data for each gamemode passed
        foreach (GameMode mode in gamemodes)
        {
            LevelData levelData = new LevelData(_levelAsset.SceneName, mode.Asset.NameID);
            LevelData.Add(levelData);
        }
    }


    public LevelData GetData(GameModeAsset asset, bool createIfNone = true)
    {
        return GetData(asset.NameID, createIfNone);
    }

    public LevelData GetData(string gamemodeID, bool createIfNone = true)
    {
        if (string.IsNullOrEmpty(gamemodeID)) return null;

        LevelData levelData = _levelData.Find(d => d.gamemodeID == gamemodeID);
        if (levelData == null)
        {
            if (createIfNone)
            {
                levelData = new LevelData(_levelAsset.SceneName, gamemodeID);
                LevelData.Add(levelData);
            }
            else return null;
        }

        return levelData;
    }

    public bool IsCompleted(string gamemodeID)
    {
        LevelData data = GetData(gamemodeID);
        if (data == null) return false;
        if (data.bestSessionData == null) return false;
        return data.bestSessionData.levelSceneName == _levelAsset.SceneName;
    }

    public bool IsCompleted()
    {
        return IsCompleted(GameManager.Instance.CurrentGameMode.Asset.NameID);
    }
}


// Runtime level data (this data is saved)
[System.Serializable]
public sealed class LevelData
{
    public string levelSceneName = "";
    public string gamemodeID = "";
    public int completedAmount = 0;
    public double playedDuration = 0.0f;

    public SessionData bestSessionData = null;
    public SessionData ongoingSessionData = null;
   

    public LevelData(string sceneName, string gamemodeID)
    {
        this.levelSceneName = sceneName;
        this.gamemodeID = gamemodeID;
    }
}