using System;
using System.Threading.Tasks;
using UnityEngine;

//This should be nr 1 in script execution order
public enum GameLoadState { invalid, loading, loadFailed, loadSuccess }
public enum GameLoadStep { none,/* saveOKCloud, saveOKLocal, saveNew, saveFail, steamConnOK, steamConnFail, findLeaderboardsOK, findLeaderboardsFail*/ }

public sealed class GameLoader : SingletonBase<GameLoader>
{
    [SerializeField] private GameManager _gameManagerTemplate = null;
    [SerializeField] private float _interStepDelay = 0.25f;
    //[SerializeField] private float _maxStepDuration = 15.0f; 

    private bool _gameManagerCreated = false;

    private GameLoadState _loadState = 0;


    public bool IsLoading { get { return _loadState == GameLoadState.loading; } }
    public bool HasLoaded { get { return _loadState == GameLoadState.loadSuccess || _loadState == GameLoadState.loadFailed; } } // True if any load ended (succesfully or not)
    public bool HasLoadedSucces { get { return _loadState == GameLoadState.loadSuccess; } } //only True if succesfully loaded

    public float LoadElapsed { get; private set; }


    public Action<GameLoadState> OnStateChange;
    public Action<GameLoadStep, string> OnStepFinish; 

    public bool StartAllowed()
    {
        // If currently loading or not yet attempted to load return false
        if (IsLoading || !HasLoaded) return false;

        return true;
    }

    public void TryLoadGame()
    {
        if (_loadState == GameLoadState.loading) return;

        LoadGameAsync();
        //StartCoroutine(LoadGame());
    }

    protected override void Awake()
    {
        base.Awake();
        if (_isDestroying) return;

        // Very important to be called first thing in Awake before anything else is called
        CreateGameManager();
    }

    private void Start()
    {
        TryLoadGame();
    }

    //private void Update()
    //{
    //    //UpdateLoading();
    //}

    //private void UpdateLoading()
    //{
    //    if(!IsLoading) return;

    //    LoadElapsed += Time.deltaTime;
    //    if (LoadElapsed > _maxStepDuration)
    //    {
    //        LoadElapsed = 0.0f;
    //        StopAllCoroutines();
    //        SetLoadState(GameLoadState.loadFailed, "Loading timed out!");
    //    }
    //}

    private void CreateGameManager()
    {
        if (_gameManagerCreated) return;

        if (!_gameManagerTemplate)
        {
            Debug.LogError("PLEASE SET A GAMEMANAGER TEMPLATE OR THE GAME WILL BE BUGGED AF!");
            return;
        }

        //Create gamemanager from template
        Instantiate(_gameManagerTemplate);
        _gameManagerCreated = true;
    }

    private async void LoadGameAsync()
    {
        // This initial delay is so to not start before any events to this script have been registered!
        await Task.Delay(TimeSpan.FromSeconds(_interStepDelay));

        // Check if not yet loading
        if (IsLoading) return;


        // 0 BEGIN
        SetLoadState(GameLoadState.loading, "Started Game load");
        LoadElapsed = 0.0f;
        await Task.Delay(TimeSpan.FromSeconds(_interStepDelay));


        // 1 ...


        // End
        SetLoadState(GameLoadState.loadSuccess, "Game loaded OK");
    }

    private void SetLoadState(GameLoadState state, string message)
    {
        _loadState = state;

        OnStateChange?.Invoke(state);
        Debug.Log("Game Loader: " + message);
    }

    private void FinishLoadStep(GameLoadStep step, string message)
    {
        OnStepFinish?.Invoke(step, message);
        Debug.Log("Finished game loading step " + step + ": " + message);
    }
}