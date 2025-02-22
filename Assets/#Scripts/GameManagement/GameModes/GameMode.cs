using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class GameMode : NetworkBehaviour
{
    [SerializeField] private GameModeAsset _asset = null;


    //public List<LeaderboardAsset> LinkedLeaderboards {  get { return _linkedLeaderboards; } }
    //public string LeaderboardString { get { return _leaderboardString; } }
    //public bool CreateLeaderboard { get { return _createLeaderboard; } }
    //public bool UseLeaderboard { get { return _useLeaderboard; } }

    protected GameManager _gameManager = null;
    protected PlayingState _playingState = null;
    protected EndState _levelEndState = null;

    // Objects can add themselves to to this list so that player position is not saved on there
    private static List<Collider> _ignoreForPlayerPosColliders = new List<Collider>();


    public GameModeAsset Asset { get { return _asset; } }

    public double Timer { get; private set; }

    // Indicates whether the gamemode has been started and has not finished!
    public bool IsActiveAndPlaying { get; private set; }
    public bool IsPaused { get; private set; }

    // Indicates whether there is an active session (so a level is loaded and being played)
    public bool InSession { get; private set; }

    // Last produced session result 
    public SessionInfo CurrentSessionInfo { get; private set; }
    public SessionInfo PrevSessionInfo { get; private set; }



    public static Action<GameMode> OnGameModeStart;
    public static Action<GameMode> OnSessionStart;
    public static Action<SessionInfo> OnSessionEnd;


    protected virtual void Awake()
    {
        _gameManager = GameManager.Instance;
        _playingState = _gameManager.GetState<PlayingState>();
        _levelEndState = _gameManager.GetState<EndState>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void OnEnable()
    {
        _gameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (_gameManager) _gameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState current)
    {
    }

    protected virtual void Update()
    {
        if (!IsActiveAndPlaying || !InSession || IsPaused) return;

        Timer += Time.deltaTime;

        if (CurrentSessionInfo == null || CurrentSessionInfo.Data == null) return;

        // Update ongoing session data
        CurrentSessionInfo.Data.time = Timer;
    }

    #region Game

    public bool TryStartGame()
    {
        if (IsActiveAndPlaying) return false;
        if (!CanStartGame()) return false;
        if (!IsHost) return false;

        IsActiveAndPlaying = true;
        StartGame();

        OnGameModeStart?.Invoke(this);
        return true;
    }

    public bool TryEndGame()
    {
        if (!IsActiveAndPlaying) return false;
        if (!IsHost) return false;

        IsActiveAndPlaying = false;

        // End possible running session first
        TryEndSession(false);
        EndGame();

        return true;
    }

    // By default can always start a new game (override this if some kind of setup is required before starting a game (aka there are requirements))
    public abstract bool CanStartGame();

    // Not same as reset sesion, this will reset the whole gamemode, any of its remaining data and end any active session first too!
    public virtual void ResetGame()
    {
        if (!IsHost) return;

        TryEndSession(false);

        IsPaused = false;
    }

    public virtual bool TrySetPause(bool pause)
    {
        if (!IsHost) return false;

        if (IsPaused == pause) return false;

        IsPaused = pause;

        if (IsPaused) PauseGame();
        else ResumeGame();
        return true;
    }

    protected virtual void StartGame() { }

    protected virtual void EndGame() { }

    protected virtual void PauseGame()
    {
    }

    protected virtual void ResumeGame()
    {
    }

    // This should only be checked when ending a session, the game will be ended if this returns true after a session was ended
    // This function merely serves as an optional extending of the gamemode duration to 1 or more sessions (such as a hotseat mode for example)
    public virtual bool IsGameFinished() { return false; } //By default false (so game is not ended on session end)
    #endregion

    #region Session

    // Host only, will notify all clients of starting session
    public bool TryStartSessionNetwork()
    {
        if (!IsHost) return false;

        StartSessionClientRPC();
        return true;
    }
    
    // Notify any clients that the session has started
    [ClientRpc]
    private void StartSessionClientRPC()
    {
        TryStartSession();
    }

    // Host only, will notify all clients of starting session
    public bool TryEndSessionNetwork(bool validComplete)
    {
        if (!IsHost) return false;

        EndSessionClientRPC(validComplete);
        return true;
    }

    // Notify any clients that the session has started
    [ClientRpc]
    private void EndSessionClientRPC(bool validComplete)
    {
        TryEndSession(validComplete);
    }

    // Will return false (and do nothing) if already in session
    public bool TryStartSession()
    {
        // Has gamemode been started, if not try starting it
        if (!IsActiveAndPlaying && !TryStartGame()) return false;

        // See if we are allowed to start a new session
        if (!CanStartSession()) return false;

        // If already in session return false
        if (InSession) return false;

        // If no session info exists, or it has been played, reset, as we want to start fresh (this should rarely happen and mostly in editor only if starting directly from a level)
        if (CurrentSessionInfo == null || CurrentSessionInfo.Played)
            ResetSession();

        //Create new session info
        //SetupSessionInfo();

        InSession = true;
        StartSession();

        OnSessionStart?.Invoke(this);

        return true;
    }


    // If in session, this will first check the same requirements for starting one
    // If not in session, reset is always allowed as long as the gamemode is active
    public bool TryResetSession(bool forceFullReset = false)
    {
        if (!IsHost) return false;
        if (!forceFullReset && !IsActiveAndPlaying) return false;

        // If in session, see if we are allowed to start the session (since we would be restarting)
        if (!forceFullReset && InSession && !CanStartSession()) return false;

        // Reset the session
        ResetSession(forceFullReset);

        return true;
    }

    // Session result can be null, which means the session was abrubtly stopped aka the level wasnt completed
    public bool TryEndSession(bool validComplete)
    {
        if (!InSession) return false;

        // Update session info first
        FinalizeSessionInfo(validComplete);

        // Move current to prev
        PrevSessionInfo = CurrentSessionInfo;
        CurrentSessionInfo = null;

        InSession = false;
        EndSession(PrevSessionInfo);

        OnSessionEnd?.Invoke(PrevSessionInfo);

        // After a session has ended, we will update the gamemode state, as it might end along with this session
        // We have to be careful because this function might be called as a result of ending the gamemode forcefully
        if (IsActiveAndPlaying && IsGameFinished()) TryEndGame();

        return true;
    }

    public virtual bool CanStartSession() { return true; }

    // Initiates and kicks off a new session
    protected virtual void StartSession()
    {
        // Once session starts, session info is now no longer 'fresh'
        CurrentSessionInfo.Played = true;
    }

    // Stops running session
    protected virtual void EndSession(SessionInfo info) 
    {
    }

    // Sets values to default, called beforing starting a new session automatically
    protected virtual void ResetSession(bool forceFullReset = false)
    {
        // If not valid checkpoint, reset the timer
        if (forceFullReset)
        {
            Timer = 0.0f;
            
            // Reset spawners (including player)
            SpawnManager.Reset();
        }
        else
        {
            // Reset player only
            //if (_gameManager.SceneData.Player)
            //{
            //    _gameManager.SceneData.Player.Spawner.Resett();
            //    Physics.SyncTransforms();
            //}
        }

        // Setup new session
        SetupSessionInfo();
    }

    // Creates a new session info with fresh data
    private void SetupSessionInfo()
    {
        Level currentLevel = GameManager.Instance.SceneData.CurrentLevel;
        if (currentLevel == null) return;

        // Create a new session info for current level and gamemode, we pass optional start data, if null we will have a fresh data object
        CurrentSessionInfo = new SessionInfo(currentLevel, this);

        // Set this as the new ongoing session data for current gamemode and level, this can be used to start from in menu
        currentLevel.CurrentData.ongoingSessionData = CurrentSessionInfo.Data;
    }

    // Only call when ending a session :)
    private void FinalizeSessionInfo(bool validComplete)
    {
        if (CurrentSessionInfo == null) return;

        // Very important to set whether the session was actually completed to indicate its validness
        CurrentSessionInfo.ValidComplete = validComplete;
        CurrentSessionInfo.Data.completed = validComplete;

        // Set time on data
        CurrentSessionInfo.Data.time = _gameManager.CurrentGameMode.Timer;

        // Can highscores be beaten for this gamemode
        if (_asset.IsLegitimate && validComplete)
        {
            // Best (saved) session data
            SessionData currentBest = CurrentSessionInfo.Level.CurrentData.bestSessionData;
        }

        // If valid complete, remove as ongoing as it is now finished, else update the data cuz c# does weird stuff sometimes (actually might have to do with starting from a level in editor before stuff is properly loaded)
        if (validComplete) CurrentSessionInfo.Level.CurrentData.ongoingSessionData = null;
        else CurrentSessionInfo.Level.CurrentData.ongoingSessionData = CurrentSessionInfo.Data;
    }
    #endregion
}