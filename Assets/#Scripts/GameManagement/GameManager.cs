using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Playables;
using System.Threading.Tasks;
using Unity.Netcode;
using System;

public sealed class GameManager : SingletonBaseNetwork<GameManager>
{
    #region Fields
    [SerializeField] private List<GameMode> _gameModes = new List<GameMode>();
    [SerializeField] private GameMode _normalMode = null;

    [Space]
    [SerializeField] private PlayerN _playerNetworkingSpawnTemplate = null;

    [Header("Transitions")]
    [SerializeField] private PlayableAsset _sceneChangeStartTrans = null;
    [SerializeField] private PlayableAsset _sceneChangeEndingTrans = null;
    [SerializeField] private PlayableAsset _sceneChangeEndedTrans = null;

    [Header("SFX")]
    [SerializeField] private SoundSpawnData _onSessionStartSFX = null;
    [SerializeField] private SoundSpawnData _onSceneChangeStartSFX = null;
    [SerializeField] private SoundSpawnData _onSceneChangeEndSFX = null;

    private List<GameState> _gameStates = new List<GameState>();
    //private List<GameObject> _gameLockSources = new List<GameObject>();

    private PlayingState _playingState = null;

    private SceneData _sceneData = null;
    private AsyncOperation _sceneChangeOperation = null;
    private bool _networkSceneLoaded = false;

    private bool _isGameLock = false;
    private GameSnapshot _gameLockSnapshot = default; //Snapshot before game was locked (used to revert back to)
    #endregion

    #region Properties

    public bool Initialized { get; private set; }
    public GameMode CurrentGameMode { get; private set; }
    public GameMode NormalGM { get { return _normalMode; } }

    public List<GameMode> GameModes { get { return _gameModes; } }

    public SceneData SceneData
    {
        get
        {
            if (!_sceneData) _sceneData = FindFirstObjectByType<SceneData>();
            if (!_sceneData)
            {
                Debug.Log("No scene data in current scene, certain behaviour might not work properly and a default one will be created!");

                //create default
                GameObject go = new GameObject("SceneData (default)");
                _sceneData = go.AddComponent<SceneData>();
            }
            return _sceneData;
        }
        set { _sceneData = value; }
    }

    public GameState CurrentState { get; private set; }
    public GameState PrevState { get; private set; }

    public bool SceneChanging { get; private set; }
    public bool GameStateChanging { get; private set; } //True if in middle of a game state change (can be check so to not fire state changes when arleady in one)
    public bool IsGameLock { get { return _isGameLock; } }

    #endregion

    public System.Action<GameState, GameState> OnGameStateChanging; // in between state changes (so after old is exited, but before new is entered)
    public System.Action<GameState> OnGameStateChanged;

    public static System.Action<GameMode> OnGameModeChange;
    public static System.Action<string> OnSceneChangeStart; //Scenechange operation + scenename
    public static System.Action<string> OnSceneChangeFinish; //After scene load AND state switch


    protected override void Awake()
    {
        base.Awake();
        if (_isDestroying) return;

        // Retrieve game states
        _gameStates.AddRange(GetComponentsInChildren<GameState>());
        _playingState = GetState<PlayingState>();

        // Disable all gamestates (should only be enabled when we are in that state)
        foreach (GameState gameState in _gameStates)
            gameState.enabled = false;

        // Disable all gamemodes initially
        foreach (GameMode gameMode in _gameModes)
            gameMode.enabled = false;

        GameMode.OnSessionStart += OnGameSessionStart;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Network scene load complete
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnNetworkSceneLoadComplete;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadCompleteAllClients;
    }

    private void Init()
    {
        if (Initialized) return;

        // Initial gameMode
        if (_gameModes.Count > 0) SetActiveGameMode(_gameModes[0]);
        else Debug.Log("No gamemodes linked on gameManager, make sure to have atleast 1 gamemode!");

        // Get current level for checking what initial state to enter
        LevelManager levelManager = LevelManager.Instance;
        Level currentLevel = SceneData.CurrentLevel;
        if (currentLevel == null)
        {
            Debug.Log("Failed to start game, make sure a level always exist no matter the scene!");
            return;
        }

        // Figure out initial state (for a build this should always be Home state!)
        if (currentLevel.Asset.IsPlayableLevel) TrySwitchState<PlayingState>();
        else
        {
            if (currentLevel == levelManager.HomeLevel) TrySwitchState<HomeState>();
            else if (currentLevel == levelManager.MenuLevel) TrySwitchState<MainMenuState>();
            else if (currentLevel == levelManager.TestLevel) TrySwitchState<PlayingState>();
        }

        // Call this as initial scene change
        OnSceneChangeFinish?.Invoke(currentLevel.Asset.SceneName);

        Initialized = true;
    }

    private void OnGameSessionStart(GameMode mode)
    {
        SoundManager.Instance.PlaySound(_onSessionStartSFX);
    }

    private void OnNetworkSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log("Client " + clientId + "Finished loading scene!");
    }

    private void OnNetworkSceneLoadCompleteAllClients(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _networkSceneLoaded = true;
        Debug.Log("All clients loaded scene!");
    }

    private void Start()
    {
        Init();
    }

    #region GameModeManagement

    public GameMode GetGameMode(GameModeAsset asset)
    {
        if (!asset) return null;

        return _gameModes.Find(gm => gm.Asset == asset);
    }

    public void SetActiveGameMode<T>() where T : GameMode
    {
        GameMode gameMode = GetGameMode<T>();
        SetActiveGameMode(gameMode);
    }

    public T GetGameMode<T>() where T : GameMode
    {
        if (_gameModes.Count == 0) return null;

        foreach (GameMode gameMode in _gameModes)
            if (gameMode is T)
                return gameMode as T;

        return null;
    }

    public bool SetActiveGameMode(GameMode gamemode)
    {
        if (!gamemode || gamemode == CurrentGameMode) return false;

        // Disable prev if any
        if (CurrentGameMode)
        {
            CurrentGameMode.TryEndGame();
            CurrentGameMode.ResetGame();
            CurrentGameMode.enabled = false;
        }

        // Set as current
        gamemode.ResetGame();
        CurrentGameMode = gamemode;

        // Enable & invoke
        CurrentGameMode.enabled = true;
        OnGameModeChange?.Invoke(gamemode);

        Debug.Log("Active gamemode switched to " + CurrentGameMode.Asset.DisplayName);

        return true;
    }

    //Called in between state changes to update gamemode and/or target state accordingly 
    private void ManageActiveGameMode(GameState _, ref GameState targetState)
    {
        //if (targetState is EndState)
        //{
        //    //This part handles the skipping of end state when a session was invalid (so player dies or reset themselves)
        //    //Check if session info is invalid and if we can keep playing (also check if gamemode still running), set target state to preplay instead
        //    if (CurrentGameMode.IsActiveAndPlaying && !CurrentGameMode.PrevSessionInfo.ValidComplete && CurrentGameMode.CanStartSession())
        //        targetState = GetState<PrePlayingState>();
        //}
        ////If enter preplay and game mode hasnt been started (or has finished since last start)
        //else if (targetState is PrePlayingState)
        //{
        //    //If not started yet
        //    CurrentGameMode.TryStartGame();

        //    //If gamemode skips the replay state, set next state to playing state instead...
        //    if (CurrentGameMode.Asset.SkipReplayState) targetState = GetState<PlayingState>();
        //}
        //if (targetState is PlayingState)
        //{
        //    //If not started yet
        //    CurrentGameMode.TryStartGame();
        //}
        //If entering a state other than the above (menu/levelSelect):
        //else
        //{
        //    //End game (can still be going), this would be a forced quit like a menu button
        //    if (CurrentGameMode && CurrentGameMode.IsActiveAndPlaying) CurrentGameMode.TryEndGame();
        //    if (CurrentGameMode) CurrentGameMode.ResetGame();
        //}
    }

    #endregion

    #region StateManagement

    public bool IsPlayingState()
    {
        return CurrentState == _playingState;
    }

    public bool CanSceneChange()
    {
        if (_isGameLock) return false;
        if (SceneChanging) return false;

        return _sceneChangeOperation == null || _sceneChangeOperation.isDone;
    }

    public async Task<bool> TrySceneChangeAsync<T>(string sceneName, bool allowReEnter = false, bool skipStartTrans = false) where T : GameState
    {
        return await TrySceneChangeAsync(sceneName, GetState<T>(), allowReEnter, skipStartTrans);
    }

    public async Task<bool> TrySceneChangeAsync(string scenename, GameState state = null, bool allowReEnter = false, bool skipStartTrans = false)
    {
        if (!CanSceneChange()) return false;

        // Check if can change state if one is passed
        if (state && !CanChangeState(state, allowReEnter)) return false;

        // Async scene change
        await ChangeSceneAsync(scenename, state, allowReEnter, skipStartTrans);

        return true;
    }

    public async Task<bool> TrySceneChangeNetworkAsync<T>(string sceneName, bool allowReEnter = false, bool skipStartTrans = false) where T : GameState
    {
        return await TrySceneChangeNetworkAsync(sceneName, GetState<T>(), allowReEnter, skipStartTrans);
    }

    public async Task<bool> TrySceneChangeNetworkAsync(string scenename, GameState state = null, bool allowReEnter = false, bool skipStartTrans = false)
    {
        if (!CanSceneChange()) return false;

        if (NetworkManager.Singleton.SceneManager == null) return false;

        // Check if can change state if one is passed
        if (state && !CanChangeState(state, allowReEnter)) return false;

        // Async scene change
        await ChangeSceneNetworkAsync(scenename, state, allowReEnter, skipStartTrans);

        return true;
    }

    public async Task ChangeSceneAsync(string scenename, GameState state = null, bool allowReEnter = false, bool skipStartTrans = false)
    {
        // Make sure to indicate we are changing scenes here 
        SceneChanging = true;

        // If playing state, make sure to unpause as otherwise the loading will be stuck DO THIS BEFORE GAME LOCK :D
        if (CurrentState == _playingState) _playingState.SetPause(false);

        // Start game lock
        StartGameLock(true, true, true);

        // SFX
        SoundManager.Instance.PlaySound(_onSceneChangeStartSFX);

        // Start transition (wait for finish)
        if (_sceneChangeStartTrans && !skipStartTrans)
        {
            //TransitionManager.Instance.DoTransition(_sceneChangeStartTrans, out PlayableDirector director);
            //if (director) yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        // Invoke before load new scene (which is assumed to be called the same frame as this function)
        OnSceneChangeStart?.Invoke(scenename);

        // Start scene change
        await SceneManager.LoadSceneAsync(scenename, LoadSceneMode.Single);

        // Wait till done
        //yield return new WaitUntil(() => _sceneChangeOperation.isDone);

        // SFX
        SoundManager.Instance.PlaySound(_onSceneChangeEndSFX);

        // Ending transition
        if (_sceneChangeEndingTrans)
        {
            //TransitionManager.Instance.DoTransition(_sceneChangeEndingTrans, out PlayableDirector director);
            //if (director) yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        // End game lock
        EndGameLock(false);

        // Optionally change state
        if (state) SwitchState(state, allowReEnter);

        // Force a hard reset, this is needed to tell the current gamemode we are in a totally new scene (as example beginner mode only resets partially otherwise sincee it uses checkpoints)
        //if (CurrentGameMode) CurrentGameMode.TryResetSession(true);

        // End
        //_sceneChangeOperation = null;
        SceneChanging = false;
        OnSceneChangeFinish?.Invoke(scenename);

        // Ended transition
        //TransitionManager.Instance.DoTransition(_sceneChangeEndedTrans, out _);

        //yield return null;
    }

    // Use the network scenemanager
    public async Task ChangeSceneNetworkAsync(string scenename, GameState state = null, bool allowReEnter = false, bool skipStartTrans = false)
    {
        // Make sure to indicate we are changing scenes here 
        SceneChanging = true;

        // Will be set to true trough callback
        _networkSceneLoaded = false;

        // If playing state, make sure to unpause as otherwise the loading will be stuck DO THIS BEFORE GAME LOCK :D
        if (CurrentState == _playingState) _playingState.SetPause(false);

        // Start game lock
        StartGameLock(true, true, true);

        // SFX
        SoundManager.Instance.PlaySound(_onSceneChangeStartSFX);

        // Start transition (wait for finish)
        if (_sceneChangeStartTrans && !skipStartTrans)
        {
            //TransitionManager.Instance.DoTransition(_sceneChangeStartTrans, out PlayableDirector director);
            //if (director) yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        // Invoke before load new scene (which is assumed to be called the same frame as this function)
        OnSceneChangeStart?.Invoke(scenename);

        // Start scene change
        NetworkManager.Singleton.SceneManager.LoadScene(scenename, LoadSceneMode.Single);
       

        // wait untill it is loaded
        while (!_networkSceneLoaded)
            await Awaitable.NextFrameAsync();

        // Wait till done
        //yield return new WaitUntil(() => _sceneChangeOperation.isDone);

        // SFX
        SoundManager.Instance.PlaySound(_onSceneChangeEndSFX);

        // Ending transition
        if (_sceneChangeEndingTrans)
        {
            //TransitionManager.Instance.DoTransition(_sceneChangeEndingTrans, out PlayableDirector director);
            //if (director) yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        // End game lock
        EndGameLock(false);

        // Optionally change state
        if (state) SwitchState(state, allowReEnter);

        // Force a hard reset, this is needed to tell the current gamemode we are in a totally new scene (as example beginner mode only resets partially otherwise sincee it uses checkpoints)
        //if (CurrentGameMode) CurrentGameMode.TryResetSession(true);

        // End
        //_sceneChangeOperation = null;
        SceneChanging = false;
        OnSceneChangeFinish?.Invoke(scenename);

        // Spawn in players
        if (_playerNetworkingSpawnTemplate) 
        {
            List<Transform> spawnTs = SceneData.PlayerSpawnTs;

            var clientIDs = NetworkManager.Singleton.ConnectedClientsIds;
            for (int i = 0; i < clientIDs.Count; i++)
            {
                ulong clientID = clientIDs[i];
                Transform spawnT = spawnTs[i];

                PlayerN player = null;
                if (spawnT) player = Instantiate(_playerNetworkingSpawnTemplate, spawnT);
                else player = Instantiate(_playerNetworkingSpawnTemplate);

                if (player.TryGetComponent(out NetworkObject networkObject)) networkObject.SpawnAsPlayerObject(clientID, true);
                else Debug.LogError("Player networking template has not network object component!");
            }
        }
        else Debug.LogError("No player template to spawn players with!");

        // Ended transition
        //TransitionManager.Instance.DoTransition(_sceneChangeEndedTrans, out _);

        //yield return null;
    }

    public bool CanChangeState<T>(bool allowReEnter = false) where T : GameState
    {
        return CanChangeState(GetState<T>(), allowReEnter);
    }

    public bool CanChangeState(GameState state, bool allowReEnter = false)
    {
        if (!state) return false;
        if (_isGameLock) return false;
        if (GameStateChanging) return false;
        if (CurrentState && !allowReEnter && CurrentState == state) return false;

        return true;
    }

    public bool SwitchState(GameState state, bool allowReEnter = false)
    {
        if (!CanChangeState(state, allowReEnter)) return false;

        GameStateChanging = true;

        //if current state, exit and disable
        if (CurrentState)
        {
            CurrentState.OnExit();
            CurrentState.enabled = false;
            PrevState = CurrentState;
        }

        //Call in between state changes (so gamemode is updated before any states and UI will update), it is possible for the gamemode to change the target state
        ManageActiveGameMode(PrevState, ref state);
        OnGameStateChanging?.Invoke(PrevState, state);

        //set new state, enter and enable
        CurrentState = state;
        CurrentState.enabled = true;
        CurrentState.OnEnter();

        OnGameStateChanged?.Invoke(CurrentState);
        GameStateChanging = false;

        return true;
    }

    public bool TrySwitchState<T>(bool allowReEnter = false) where T : GameState
    {
        return SwitchState(GetState<T>(), allowReEnter);
    }

    public T GetState<T>() where T : GameState
    {
        foreach (GameState state in _gameStates)
        {
            if (state is T)
                return state as T;
        }

        return null;
    }
    #endregion

    #region GameLock

    //public void AddGameLockSource(GameObject source)
    //{
    //    if (_gameLockSources.Contains(source)) return;
    //    //if (GameStateChanging) return;

    //    _gameLockSources.Add(source);

    //    //If already locked this will do nothing
    //    StartGameLock();
    //}

    //public void RemoveGameLockSource(GameObject source)
    //{
    //    if(!_gameLockSources.Contains(source)) return;

    //    _gameLockSources.Remove(source);

    //    //Will only actually end the game lock if there are no mroe sources left (aka this was the last one)
    //    EndGameLock();
    //}

    public struct GameSnapshot
    {
        public bool wasPlayer; // Was there player before?
        public bool disablePlayerInput;
        public bool playerRBKinematic;
        public RigidbodyConstraints rbConstraints;
        public bool gamemodePauseState;
        public bool hudActiveSelf;
    }

    public void StartGameLock(bool disablePlayerInput, bool pauseGM, bool disableHUD)
    {
        if (_isGameLock) return;
        if (!Initialized) return;

        _isGameLock = true;

        //Reset snapshot to default, we will only set the values we change to true so we can revert them back later
        _gameLockSnapshot = default;

        //Disable player input
        PlayerN player = SceneData.Player;
        if (player && disablePlayerInput)
        {
            //Cache player input state before changing
            _gameLockSnapshot.disablePlayerInput = player.DisableInput;
            //_gameLockSnapshot.playerRBKinematic = player.Movement.RB.isKinematic;
            
            player.DisableInput = true;
            player.Health.Data.canBeDamaged = false;

            //If we set player non kinematic it fucks with physics/trigger stuff
            //player.Movement.RB.isKinematic = true;

            _gameLockSnapshot.rbConstraints = player.Movement.RB.constraints;
            player.Movement.RB.constraints = RigidbodyConstraints.FreezeAll;

            //Cancel any ongoing abilities + stop movement
            player.AbilityManager.Cancel();
            //player.Movement.Stop();
        }

        _gameLockSnapshot.wasPlayer = player != null;

        //Pause gamemode
        if (CurrentGameMode && pauseGM)
        {
            _gameLockSnapshot.gamemodePauseState = CurrentGameMode.IsPaused;
            CurrentGameMode.TrySetPause(true);
        }

        //Hide/disalbe UI?
        if (disableHUD)
        {
            _gameLockSnapshot.hudActiveSelf = CurrentState.HUD.gameObject.activeSelf;
            CurrentState.HUD.gameObject.SetActive(false);
        }

        //Set player rb to kinematic?
    }

    public void EndGameLock(bool revertHUD)
    {
        if (!_isGameLock) return;

        _isGameLock = false;

        // Set disable player input to previous state
        PlayerN player = SceneData.Player;
        if (player && _gameLockSnapshot.wasPlayer)
        {
            player.DisableInput = _gameLockSnapshot.disablePlayerInput;
            player.Movement.IsStopped = false;
            player.Health.Data.canBeDamaged = true;

            //player.Movement.RB.isKinematic = _gameLockSnapshot.playerRBKinematic;
            player.Movement.RB.constraints = _gameLockSnapshot.rbConstraints;
        }

        // Set gamemode pause state
        if (CurrentGameMode) CurrentGameMode.TrySetPause(_gameLockSnapshot.gamemodePauseState);

        // Revert HUD activeSelf
        if (revertHUD) CurrentState.HUD.gameObject.SetActive(_gameLockSnapshot.hudActiveSelf);
    }

    #endregion
}