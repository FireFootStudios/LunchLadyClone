using System;
using UnityEngine;

public sealed class PlayingState : GameState
{
    public static bool IsPaused { get; private set; }

    public static Action<bool> OnPauseChange; // IsPaused

    protected override void Awake()
    {
        base.Awake();
    }

    public void SetPause(bool pause)
    {
        if (!this.isActiveAndEnabled) return;
        if (_gameManager.IsGameLock) return;

        IsPaused = pause;

        // Add time scale mod (indefinite), or remove time scale mod
        if (IsPaused) TimeScaleManager.Instance.AddTimeScaleMod(0.0f, this);
        else TimeScaleManager.Instance.RemoveTimeScaleMod(this);

        // Pause audio
        AudioListener.pause = IsPaused;

        // Pause physics (This is necessary so that physics doesnt accumulate during pause such as ragdolls but also collision solving)
        //Physics.simulationMode = IsPaused ? SimulationMode.Script : SimulationMode.FixedUpdate;

        //Invoke
        OnPauseChange?.Invoke(IsPaused);
        //Debug.Log("Pause Change: " + IsPaused);
    }

#if !UNITY_EDITOR
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) SetPause(true);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SetPause(true);
    }

#endif

    public override void OnEnter()
    {
        base.OnEnter();

        SetPause(false);

        //Will not be execute if already in session, in that case something probably went wrong
        //if (!_gameManager.CurrentGameMode.TryStartSession())
        //{
        //    Debug.Log("Could not start session on entering playing state, this is not ideal");
        //    _gameManager.CurrentGameMode.TryResetSession();
        //}
    }

    public override void OnExit()
    {
        base.OnExit();

        SetPause(false);
    }
}