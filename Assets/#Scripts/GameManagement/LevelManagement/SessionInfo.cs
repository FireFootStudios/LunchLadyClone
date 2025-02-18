using System;
using UnityEngine;


//Runtime wrapper
public sealed class SessionInfo
{
    //public LevelData levelData = null;
     
    public SessionData Data { get; private set; } // Serializable (to save) data

    public bool ValidComplete {  get; set; } // Was this a valid (completed) session?
    public bool HighScoreBeaten { get; set; }
    public bool Played { get; set; }

    public Level Level { get; set; } // What level was session played in
    public GameMode GameMode { get; set; } // What gamemode was session played with

    public SessionInfo(Level level, GameMode gamemode)
    {
        if (level == null || !gamemode) return;

        Level = level;
        GameMode = gamemode;

        this.Data = new SessionData();
        Data.levelSceneName = Level.Asset.SceneName;
        Data.gamemodeID = gamemode.Asset.NameID;
    }

    public SessionInfo(Level level, GameMode gamemode, SessionData existingData)
    {
        if (level == null || !gamemode) return;

        Level = level;
        GameMode = gamemode;

        // Check if data is valid for level and gamemode, if not, we just create a new one
        if (IsValidData(existingData, level, gamemode) && ! existingData.completed)
        {
            this.Data = existingData;
        }
        else
        {
            this.Data = new SessionData();
            Data.levelSceneName = Level.Asset.SceneName;
            Data.gamemodeID = gamemode.Asset.NameID;
        }
    }

    public static bool IsValidData(SessionData data, Level level, GameMode gamemode)
    {
        if (level == null || !gamemode || data == null) return false;

        if (level.Asset.SceneName != data.levelSceneName) return false;
        if (gamemode.Asset.NameID != data.gamemodeID) return false;

        return true;
    }
    // public SessionInfo(SessionData data)
    // { this.Data = data; }

}

[System.Serializable]
public sealed class SessionData
{
    public string levelSceneName = null; // What level was this session played in
    public string gamemodeID = null; // Unique gamemode ID
    public double time = 123456789.0f;

    public int checkpointIndex = -1; // Index of best checkpoint reached

    // Player Position
    public Vector3 playerPos = Vector3.zero;
    public bool validPlayerPos = false; // Indicates wheter the player position has been set already

    public bool completed;

    // Completed only
    public float timeDiff = 0.0f; // Time difference between previous session

    // We need to know for ongoing sessions whether to enable other dialogue
    public bool satanIntroPlayed = false;
}