using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Spawner))]
[RequireComponent(typeof(FreeMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(AbilityManager))]
public sealed class PlayerN : NetworkBehaviour
{
    #region Fields
    [Header("General"), SerializeField] private PlayerCameras _cameraTemplate = null;
    [SerializeField] private Health _health = null;
    [SerializeField] private FreeMovement _movement = null;
    [SerializeField] private GameObject _visuals = null;
    [Space]
    [SerializeField] private bool _ignoreMultiplayer = false;

    [Space]
    [SerializeField] private bool _enableFlyModeBuild = false;
    [SerializeField] private bool _enableScreenshotBuild = false;
    [SerializeField] private bool _enableTPBuild = false;

    [Header("Death")]
    [SerializeField] private float _resetAfterDeathDelay = 3.0f;
    [SerializeField] private float _forceOnDeath = 10.0f;
    [SerializeField] private Transform _forceT = null;


    [Header("Abilities"), SerializeField] private Ability _jumpAbility = null;
    [SerializeField] private Ability _sprintAbility = null;


    private PlayerInput _input = null;
    private PlayerCameras _playerCameras = null;
    private AbilityManager _abilityManager = null;
    private bool _sprintInput = false;
    private bool _sprintIsToggle = false; //setting

    private PlayingState _playingState = null;
    private GameManager _gameManager = null;
    private InputManager _inputManager = null;

    //Cached from input manager
    private Controls _controls = null;

    #endregion

    #region Properties
    //public float LookSensitivity { get { return InputManager.ControllerMode ? _lookSensitivityController * _controllerSensMult : _lookSensitivity; } }

    public Health Health { get { return _health; } }
    public FreeMovement Movement { get { return _movement; } }
    public AbilityManager AbilityManager { get { return _abilityManager; } }
    public Spawner Spawner { get; private set; }

    public bool HasMoveInput { get; private set; }

    public bool DisableInput { get; set; }
    public bool DisableMoveInput { get; set; }
    public bool IsSprinting { get { return _sprintAbility && _sprintAbility.IsFiring; } }

    private Vector2 MoveInputVec { get; set; }

    public PlayerCameras PlayerCameras { get { return _playerCameras; } }

    public Ability JumpAbility { get { return _jumpAbility; } }
    public Ability SprintAbility { get { return _sprintAbility; } }

    public Action<bool> OnMoveInputChange;

    #endregion

    #region InputFunctions
    public void MoveInput(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        MoveInputVec = context.ReadValue<Vector2>();
        if (context.canceled) MoveInputVec = Vector2.zero;

        if (context.performed && !HasMoveInput)
        {
            HasMoveInput = true;
            OnMoveInputChange?.Invoke(HasMoveInput);
        }
        else if (context.canceled && HasMoveInput)
        {
            HasMoveInput = false;
            OnMoveInputChange?.Invoke(HasMoveInput);
        }
    }

    public void LookInputMouse(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (DisableInput) return;

        _playerCameras.RotateVecDelta += context.ReadValue<Vector2>();
    }

    public void LookInputGamepad(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (DisableInput) return;

        //LookGamepadInputVec = context.ReadValue<Vector2>();
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;
        if (!context.performed || DisableInput || DisableMoveInput) return;

        _abilityManager.TryUseAbilityInputBuffer(_jumpAbility);
    }

    public void SprintInput(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!_sprintAbility) return;

        if (context.performed)
        {
            _sprintInput = true;

            //If sprint is toggle we either use or cancel ability based on current state
            if (_sprintIsToggle && _sprintAbility.IsFiring) _sprintAbility.Cancel();
            else _abilityManager.TryUseAbility(_sprintAbility);
        }
        else if (context.canceled)
        {
            _sprintInput = false;
            if (!_sprintIsToggle) _sprintAbility.Cancel();
        }
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed || !_playingState.enabled) return;
        if (_gameManager.IsGameLock) return;

        //Pause HUD will take care of unpausing
        //if (!PlayingState.IsPaused) _playingState.SetPause(true);
    }

    public void RestartLevel(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed || !_playingState.enabled) return;

        //Try end session -> if possible, we will automatically restart (since we are ending on a uncompleted session)
        if (_gameManager.CanChangeState<EndState>() && _gameManager.CurrentGameMode.TryEndSession(false))
            _gameManager.TrySwitchState<EndState>();
    }

    public void ToggleFlyMode(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed || DisableMoveInput) return;
        if (!Application.isEditor && !_enableFlyModeBuild) return;

        MoveID currentMoveID = Movement.GetMappedMoveID(MoveType.air);
        MoveID targetMoveID = currentMoveID == MoveID.falling ? MoveID.flying : MoveID.falling;
        Movement.ChangeMoveDataForType(MoveType.air, targetMoveID);

        //small upwards force
        if (targetMoveID == MoveID.flying && Movement.IsGrounded) Movement.RB.AddForce(Vector3.up * 5.0f, ForceMode.VelocityChange);
    }

    public void TakeScreenshot(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableScreenshotBuild) return;

        ScreenshotManager.Instance.TakeScreenshot();
    }

    public void TPP1(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableTPBuild) return;

        TPManager.Instance.TryTPToPoint(0);
    }

    public void TPP2(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableTPBuild) return;

        TPManager.Instance.TryTPToPoint(1);
    }

    public void TPP3(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableTPBuild) return;

        TPManager.Instance.TryTPToPoint(2);
    }

    public void TPP4(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableTPBuild) return;

        TPManager.Instance.TryTPToPoint(3);
    }

    public void TPP5(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;
        if (!Application.isEditor && !_enableTPBuild) return;

        TPManager.Instance.TryTPToPoint(4);
    }

    public void SkipSong(InputAction.CallbackContext context)
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!context.performed) return;

        //MusicPlayer.Instance.TrySkipSong();
    }

    //public void FlyInput(InputAction.CallbackContext context)
    //{
    //    //if (!Application.isEditor) return;

    //    FlyInputAmount = context.ReadValue<float>();
    //}
    #endregion

    #region PublicFunctions

    public void Parent(Transform parent)
    {
        if (parent && transform.parent == parent) return;
        if (!parent && !transform.parent) return;

        transform.parent = parent;

        //this is important as the parent might have been affecting the rotation (local <-> world)
        //PlayerEulerAngles = transform.localEulerAngles;

        //Physics.SyncTransforms();
    }

    //public void SetRotation(float x, float y)
    //{
    //    PlayerEulerAngles = new Vector3(0.0f, y, 0.0f);
    //    CameraLocalEulerAngles = new Vector3(x, 0.0f, 0.0f);
    //    transform.eulerAngles = PlayerEulerAngles;
    //    //MainCamera.transform.localEulerAngles = CameraLocalEulerAngles;

    //    Physics.SyncTransforms();
    //}
    #endregion

    #region Initialization
    private void Awake()
    {
        _gameManager = GameManager.Instance;
        
        // Cache comps
        _input = GetComponent<PlayerInput>();
        _abilityManager = GetComponent<AbilityManager>();
        Spawner = GetComponent<Spawner>();

        // Cache playing state
        _playingState = GameManager.Instance.GetComponent<PlayingState>();
        PlayingState.OnPauseChange += OnPauseChange;

        // Spawner
        Spawner.OnRespawn += OnRespawn;

        // End game on death
        Health.OnDeath += OnDeath;

        // Check if game is locked on Awake, in that case we need to start with input disabled!
        if (_gameManager.IsGameLock) DisableInput = true;

        // In case testing
        OwnerOnly();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OwnerOnly();
    }

    private void OwnerOnly()
    {
        if (!_ignoreMultiplayer && !IsOwner) return;

        // Firstly, create camera from template and link ourselves, ONLY IF OWNER AS A CAMERA SHOULD ONLY EXIST PER OWNING PLAYER
        if (_cameraTemplate)
        {
            _playerCameras = Instantiate(_cameraTemplate);
            _playerCameras.Init(this);
        }

        // Disable visuals if we are the owner
        if (_visuals) _visuals.gameObject.SetActive(false);

        // Init input events
        InitInput();
    }

    private void InitInput()
    {
        _inputManager = InputManager.Instance;
        _controls = _inputManager.Controls;

        // Move
        _controls.Player.Move.performed += MoveInput;
        _controls.Player.Move.canceled += MoveInput;

        // Look
        _controls.Player.LookMouse.performed += LookInputMouse;
        _controls.Player.LookMouse.canceled += LookInputMouse;

        _controls.Player.LookGamepad.performed += LookInputGamepad;
        _controls.Player.LookGamepad.canceled += LookInputGamepad;

        // Jump 
        _controls.Player.Jump.performed += JumpInput;

        // Sprint
        _controls.Player.Sprint.performed += SprintInput;
        _controls.Player.Sprint.canceled += SprintInput;

        // Toggle Pause
        _controls.Player.TogglePauseMenu.performed += TogglePause;

        // Restart level
        _controls.Player.QuickRestart.performed += RestartLevel;

        // Toggle Fly Mode
        _controls.Player.ToggleFlyMode.performed += ToggleFlyMode;

        // Screenshot
        _controls.Player.TakeScreenshot.performed += TakeScreenshot;

        // TP
        _controls.Player.TP1.performed += TPP1;
        _controls.Player.TP2.performed += TPP2;
        _controls.Player.TP3.performed += TPP3;
        _controls.Player.TP4.performed += TPP4;
        _controls.Player.TP5.performed += TPP5;

        // Skip song
        _controls.Player.SkipSong.performed += SkipSong;
    }

    private void CleanupInput()
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (_controls == null) return;

        //Move
        _controls.Player.Move.performed -= MoveInput;
        _controls.Player.Move.canceled -= MoveInput;

        //Look
        _controls.Player.LookMouse.performed -= LookInputMouse;
        _controls.Player.LookMouse.canceled -= LookInputMouse;

        _controls.Player.LookGamepad.performed -= LookInputGamepad;
        _controls.Player.LookGamepad.canceled -= LookInputGamepad;

        //Jump 
        _controls.Player.Jump.performed -= JumpInput;

        //Sprint
        _controls.Player.Sprint.performed -= SprintInput;
        _controls.Player.Sprint.canceled -= SprintInput;

        //Toggle Pause
        _controls.Player.TogglePauseMenu.performed -= TogglePause;

        //Restart level
        _controls.Player.QuickRestart.performed -= RestartLevel;

        //Toggle Fly Mode
        _controls.Player.ToggleFlyMode.performed -= ToggleFlyMode;

        //Screenshot
        _controls.Player.TakeScreenshot.performed -= TakeScreenshot;

        //TP
        _controls.Player.TP1.performed -= TPP1;
        _controls.Player.TP2.performed -= TPP2;
        _controls.Player.TP3.performed -= TPP3;
        _controls.Player.TP4.performed -= TPP4;
        _controls.Player.TP5.performed -= TPP5;

        // Skip song
        _controls.Player.SkipSong.performed -= SkipSong;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        CleanupInput();

        PlayingState.OnPauseChange -= OnPauseChange;
    }

    private void OnPauseChange(bool isPaused)
    {
        DisableInput = isPaused || Health.IsDead || _gameManager.IsGameLock;
    }

    private void OnDeath()
    {
        Movement.Stop();
        DisableInput = true;
        //Movement.RB.constraints = RigidbodyConstraints.None;
        // Movement.RB.useGravity = true;

        // Vector2 randomForce = _forceOnDeath * UnityEngine.Random.insideUnitCircle;
        //Vector3 force = new Vector3(randomForce.x, 0.0f, randomForce.y);
        // Movement.RB.AddForce(force, ForceMode.VelocityChange);
        //Movement.RB.AddForceAtPosition(force, _forceT ? _forceT.position : transform.position, ForceMode.VelocityChange);
        //Invoke("ResetAfterDeath", _resetAfterDeathDelay);
    }

    private void ResetAfterDeath()
    {
        // CHECK IF PLAYER IS STILL DEAD FIRST (might have forced respawn already)
        if (!Health.IsDead) return;

        // Try end session-> if possible, we will automatically restart(since we are ending on a uncompleted session)
        if (_gameManager.CanChangeState<EndState>() && _gameManager.CurrentGameMode.TryEndSession(false))
            _gameManager.TrySwitchState<EndState>();
    }

    private void OnRespawn()
    {
        //Unparent??
        //Parent(null);

        DisableInput = false;

        // Constraints should be set by preplay state?
        //if (_gameManager.CurrentState == _playingState) Movement.RB.constraints = RigidbodyConstraints.FreezeRotation;
     
        //Movement.RB.useGravity = false;
        Movement.ClearModifiers();
    }


    #endregion

    #region Updates
    private void Update()
    {
        if (!IsOwner && !_ignoreMultiplayer) return;

        if (!DisableInput && !DisableMoveInput)
        {
            // Update desired movement with input vecs
            Movement.DesiredMovement = MoveInputVec.x * _playerCameras.transform.right + MoveInputVec.y * _playerCameras.transform.forward;

            // Fly input
            if ((Application.isEditor || _enableFlyModeBuild) && Movement.GetMappedMoveID(MoveType.air) == MoveID.flying)
            {
                float flyInput = _controls.Player.MoveUpDown.ReadValue<float>();
                Movement.DesiredMovement += flyInput * Vector3.up;
            }

            // Update sprinting with input (sprint might have been disabled but while it is still pressed we keep trying to use it again)
            if (_sprintInput && !_sprintIsToggle && !IsSprinting) _abilityManager.TryUseAbility(_sprintAbility);
        }
    }
    #endregion
}