using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputManager : SingletonBase<InputManager>
{
    [SerializeField] private List<RebindAsset> _rebindAssets = new List<RebindAsset>();
    [Space]
    [SerializeField] private bool _disableControllerMode = true;

    private Controls _controls = null;

    private GameManager _gameManager = null;
    private PlayingState _playingState = null;


    private List<Rebind> _rebinds = new List<Rebind>();

    //Is a gamepad device connected right now?
    public static bool ControllerMode { get; private set; }
    public bool IsRebinding { get; private set; }
    public float LastRebindElapsed { get; private set; }

    //For controller and UI mainly, could use for other stuff too
    public Controls Controls
    {
        get
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Enable();
            }
            return _controls;
        }
    }


    public static Action<bool> OnControllerModeChange;
    public static Action<Rebind> OnRebindOperationEnd;


    public Rebind GetRebind(RebindAsset asset)
    {
        if (asset == null || !asset.IsValid) return null;

        Rebind rebind = _rebinds.Find(r => r.Asset == asset);
        return rebind;
    }

    public bool TryRebind(RebindAsset rebindAsset, out Rebind rebind)
    {
        rebind = null;

        if (!rebindAsset || !rebindAsset.IsValid) return false;

        rebind = GetRebind(rebindAsset);
        if (rebind == null)
        {
            //Search for the action on the runtime controls!
            InputAction inputAction = _controls.FindAction(rebindAsset.ActionName);
            rebind = new Rebind(rebindAsset, inputAction);
            _rebinds.Add(rebind);
        }

        //if (inputAction == null || inputAction.bindings.Count <= rebindAsset.BindingIndex)
        //{
        //    Debug.LogError("Rebind failed");
        //    return false;
        //}

        return PerformRebind(rebind);
    }

    public void RemoveRebind(RebindAsset rebindAsset)
    {
        if (!rebindAsset || !rebindAsset.IsValid) return;

        Rebind rebind = GetRebind(rebindAsset);
        if (rebind == null) return;

        rebind.InputAction.RemoveBindingOverride(rebindAsset.BindingIndex);
        _rebinds.Remove(rebind);
    }

    protected override void Awake()
    {
        base.Awake();
        if (_isDestroying) return;

        _gameManager = GameManager.Instance;
        _playingState = _gameManager.GetState<PlayingState>();

        //Future device changes
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        //Initial devices
        foreach (InputDevice inputDevice in InputSystem.devices)
            OnDeviceChange(inputDevice, InputDeviceChange.Added);
    }

    private void Update()
    {
        LastRebindElapsed += Time.unscaledDeltaTime;

        UpdateCursorMode();
    }

    private void UpdateCursorMode()
    {
        GameState currentGameState = _gameManager.CurrentState;

        //Update cursor visibility
        bool hidCursor = ControllerMode /*|| currentGameState is PrePlayingState*/ || (currentGameState == _playingState && !PlayingState.IsPaused);
        Cursor.lockState = hidCursor ? CursorLockMode.Locked : CursorLockMode.Confined;
        Cursor.visible = !hidCursor;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not Gamepad) return;
        if (_disableControllerMode) return;

        bool prevState = ControllerMode;

        if (change == InputDeviceChange.Added)
        {
            ControllerMode = true;
            Debug.Log("Controller Mode Enabled: " + device.displayName);
        }
        else if (change == InputDeviceChange.Removed)
        {
            ControllerMode = false;
            Debug.Log("Controller Mode Disabled: " + device.displayName);
        }

        //Did controller mode change?
        if (prevState != ControllerMode)
            OnControllerModeChange?.Invoke(ControllerMode);
    }

    private bool PerformRebind(Rebind rebind)
    {
        if (rebind == null || !rebind.IsValid) return false;

        IsRebinding = true;

        //Get action from rebind asset
        rebind.InputAction.Disable();

        //Perform rebind operation for binding
        var rebindOp = rebind.InputAction.PerformInteractiveRebinding(rebind.Data.bindingIndex);

        //Handle completion
        rebindOp.OnComplete(op =>
        {
            //Save the override path value on data (for saving/loading)
            rebind.Data.overridePath = rebind.InputAction.bindings[rebind.Data.bindingIndex].overridePath;

            IsRebinding = false;
            LastRebindElapsed = 0.0f;
            OnRebindOperationEnd?.Invoke(rebind);

            rebind.InputAction.Enable();
            op.Dispose();
        });

        //Handle cancelation
        rebindOp.OnCancel(op =>
        {
            //TODO: Handle cancel (revert back to previous or no rebind)
            //I think we can check the override path value, if this is not set we can delete the revind and remove, else we dont need to do anything
            IsRebinding = false;
            LastRebindElapsed = 0.0f;
            OnRebindOperationEnd?.Invoke(rebind);

            rebind.InputAction.Enable();
            op.Dispose();
        });

        //Exclude stuff
        rebindOp.WithCancelingThrough("<Keyboard>/escape");

        //This is so that on pressing a key that is excluded Unity doesnt set 'AnyKey' instead of just canceling the op...
        //rebindOp.WithControlsExcluding("<keyboard>/anyKey");

        foreach (RebindAsset rebindAsset in _rebindAssets)
        {
            if (!rebindAsset || !rebindAsset.IsValid) continue;

            //Get runtime action (for actual current binding)
            InputAction inputAction = _controls.FindAction(rebindAsset.ActionName);
            if (inputAction == null) continue;

            //We do allow setting same button tho
            if (inputAction == rebind.InputAction && rebind.Asset.BindingIndex == rebindAsset.BindingIndex) continue;

            //Exclude binding paths
            string path = inputAction.bindings[rebindAsset.BindingIndex].effectivePath;

            //Cannot call this more than once as it will overwrite the previous call..
            //rebindOp.WithCancelingThrough(path);

            //This leave a 'AnyKey' value and still completes the rebind even tho we want to exclude these keys XD
            rebindOp.WithControlsExcluding(path);
        }

        //Exclude more stuff
        rebindOp.WithControlsExcluding("<keyboard>/anyKey");
        rebindOp.WithControlsExcluding("<Mouse>/press");

        rebindOp.Start();
        return true;
    }
}

//Runtime wrapper
public sealed class Rebind
{
    public RebindAsset Asset {  get; private set; }
    public RebindData Data { get; private set; }

    //Runtime Action used for rebinding
    public InputAction InputAction { get; set; }

    public bool IsValid { get { return Asset != null && Data != null && InputAction != null; } }
    public bool HasOverride { get { return Data != null ? !string.IsNullOrEmpty(Data.overridePath) : false; } }

    public Rebind(RebindAsset asset, RebindData data, InputAction inputAction)
    {
        Asset = asset;
        Data = data;
        InputAction = inputAction;
    }

    public Rebind(RebindAsset asset, InputAction inputAction)
    {
        Asset = asset;
        InputAction = inputAction;
        Data = new RebindData()
        {
            actionName = InputAction.name,
            bindingIndex = asset.BindingIndex,
        };
    }
}

//Save data for rebinds
[System.Serializable]
public sealed class RebindData
{
    //Data to ID the rebind
    public string actionName;
    public int bindingIndex;

    //Actual value of the rebind
    public string overridePath;
}