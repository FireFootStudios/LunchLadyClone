using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerCameras : MonoBehaviour
{
    #region Fields
    [Header("General"), SerializeField] private Camera _mainCamera = null;
    [SerializeField] private Camera _extraCamera = null;

    [Space]
    [SerializeField] private Spawner _spawner = null;
    //[SerializeField] private PlayerN _player = null;
    [SerializeField] public float _playerFollowSmoothSpeed = 0.125f;

    [Header("Rotations"), SerializeField] private float _rotationSpeed = 1f;
    [SerializeField] private float _lookSensitivity = 0.15f;
    [SerializeField] private float _maxAngleUp = 85.0f;
    [SerializeField] private float _maxAngleDown = 85.0f;

    [Header("Tweens")]
    [SerializeField] private ShakeRandomnessMode _shakeRandomnessMode = ShakeRandomnessMode.Full;
    [SerializeField] private Transform _tweenTarget = null;

    [Header("Tween Land")]
    [SerializeField] private Vector3 _landPunchStrengthMin = Vector3.zero;
    [SerializeField] private Vector3 _landPunchStrengthMax = Vector3.zero;
    [SerializeField, Tooltip("Bounds used to lerp between min and max Punch strength based on land velocity mag")] private Vector2 _landPunchScaleBounds = Vector3.zero;
    [Space]
    [SerializeField] private float _landPunchDuration = 0.25f;
    [SerializeField] private int _landPunchVibrato = 10;
    [SerializeField] private Ease _landPunchEase = Ease.Linear;
    [SerializeField] private float _landPunchOvershoot = 0.5f;
    [SerializeField] private float _landElasticity = 1.0f;

    [Header("Tween Damage")]
    [SerializeField] private Vector3 _dmgShakeStrengthMin = Vector3.zero;
    [SerializeField] private Vector3 _dmgShakeStrengthMax = Vector3.zero;
    [SerializeField, Tooltip("Bounds used to lerp between min and max shake strength based on max health")] private Vector2 _dmgShakeScaleBounds = Vector3.zero;
    [Space]
    [SerializeField] private float _dmgShakeDuration = 0.25f;
    [SerializeField] private int _dmgShakeVibrato = 10;
    [SerializeField] private Ease _dmgShakeEase = Ease.Linear;
    [SerializeField] private float _dmgShakeOvershoot = 0.5f;
    [SerializeField] private bool _dmgShakeFadeOut = false;

    [Header("Tween Jump")]
    [SerializeField] private float _JumpPunchDuration = 0.25f;
    [SerializeField] private Vector3 _JumpPunchStrength = Vector3.zero;
    [SerializeField] private int _JumpPunchVibrato = 10;
    [SerializeField] private Ease _JumpPunchEase = Ease.Linear;
    [SerializeField] private float _JumpPunchOvershoot = 0.5f;

    [Header("Tween Kick General")]
    [SerializeField] private Vector3 _kickTargetOffset = Vector3.zero;
    [SerializeField] private float _kickSmoothIn = 0.25f;
    [SerializeField] private float _kickSmoothOut = 0.25f;

    [Header("Tween Kick hit")]
    [SerializeField] private Vector3 _kickHitStrength = Vector3.zero;
    [Space]
    [SerializeField] private float _kickHitShakeDuration = 0.25f;
    [SerializeField] private int _kickHitShakeVibrato = 10;
    [SerializeField] private Ease _kickHitShakeEase = Ease.Linear;
    [SerializeField] private float _kickHitShakeOvershoot = 0.5f;
    [SerializeField] private bool _kickHitShakeFadeOut = false;


    private GameManager _gameManager = null;
    private PlayerN _player = null;

    private Tween _currentTween = null;
    private Coroutine _offsetCo = null;

    // Synced with settings
    private bool _invertX = false;
    private bool _invertY = false;
    private bool _useScreenShake = true;

    private Vector3 _velocity = Vector3.zero;

    private Kick _playerKick = null;


    #endregion

    public PlayerN Player { get { return _player; }}
    public Camera MainCamera { get { return _mainCamera; } }
    public Spawner Spawner { get { return _spawner; } }

    public float DefaultFOV { get; private set; }
    public Vector3 LocalEulerAngles { get; private set; }
    //public Vector3 PlayerEulerAngles { get; private set; }

    //Rotate vec using delta (frame independent input)
    public Vector2 RotateVecDelta { get; set; }



    public void Init(PlayerN player)
    {
        // Return if player for now
        if (!player || _player) return;

        _player = player;

        _playerKick = _player.GetComponentInChildren<Kick>();
        if (_playerKick)
        {
            _playerKick.OnKickHitOrMiss += OnKickHitOrMiss;
            _playerKick.OnHit += OnKickHit;
        } 

        // Jump
        if (_player.JumpAbility) _player.JumpAbility.OnFire += OnPlayerJump;

        // Movement
        _player.Movement.OnGrounded += OnPlayerGrounded;

        // Health
        _player.Health.OnDamaged += OnPlayerDamaged;
    }

    // Auto goes back to default
    public void DoOffsetCoPunch(Vector3 targetOffset, float smoothIn, float smoothOut, float maxSpeed = 100.0f, bool resetFirst = true)
    {
        if (!_player) return;

        if (_offsetCo != null) StopCoroutine(_offsetCo);
        _offsetCo = StartCoroutine(OffsetCoPunch(targetOffset, smoothIn, smoothOut, maxSpeed, resetFirst));
    }

    public void DoOffsetCo(Vector3 targetOffset, float smoothIn, float maxSpeed = 100.0f, bool resetFirst = true)
    {
        if (!_player) return;

        if (_offsetCo != null) StopCoroutine(_offsetCo);
        _offsetCo = StartCoroutine(OffsetCo(targetOffset, smoothIn, maxSpeed, resetFirst));
    }

    private void Awake()
    {
        _gameManager = GameManager.Instance;

        // Cache default fov of main camera
        DefaultFOV = _mainCamera.fieldOfView;

        // Spawner
        if (_spawner) _spawner.OnRespawn += OnRespawn;

        // Init euler angles with start angles
        LocalEulerAngles = transform.localEulerAngles;
    }

    #region Updates
    private void Update()
    {
        UpdateRotation();
        SyncPlayerRotation();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdateRotation()
    {
        if (!_player) return;

        // If player input is disabled, the camera will follow the players rotation
        if (_player.DisableInput && Time.timeScale > 0.0f)
        {
            LocalEulerAngles = _player.transform.localEulerAngles;
            transform.localEulerAngles = LocalEulerAngles;
            return;
        }

        // We do not multiply by any time shenanigans here as we are using a input vec which is a delta type input (meaning its already frame independant)
        Vector2 inputVecScaled = (_lookSensitivity * _rotationSpeed * RotateVecDelta * _player.Movement.RotationMultiplier);

        // Invert X/Y
        if (_invertX) inputVecScaled.x *= -1.0f;
        if (_invertY) inputVecScaled.y *= -1.0f;

        // Calculate new Y angle
        float angleY = LocalEulerAngles.y + inputVecScaled.x;

        // Calculate new X angle
        float angleX = LocalEulerAngles.x + -inputVecScaled.y;

        // For some reason euler angles are interpreted different (range wise) from when i calculate them (the same way the angles appear in the editor) and when unity does, basicly unity does + 360...
        if (angleX > 180.0f) angleX -= 360.0f;
        else if (angleX < -180.0f) angleX += 360.0f;

        // Clamp X rotation
        angleX = Mathf.Clamp(angleX, -_maxAngleUp, _maxAngleDown);

        LocalEulerAngles = new Vector3(angleX, angleY, 0.0f);
        transform.localEulerAngles = LocalEulerAngles;

        // Reset delta vec as it is accumulation of deltas!
        RotateVecDelta = Vector2.zero;
    }

    private void SyncPlayerRotation()
    {
        if (!_player || _player.DisableInput) return;
        if (!_player.Movement || !_player.Movement.RB) return;

        // Create a new rotation only on the y-axis
        Quaternion targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        //Set player RB rotation
        _player.Movement.RB.MoveRotation(targetRotation);
    }

    private void UpdatePosition()
    {
        if (!_player) return;

        // Localize our local spawn poss offset to the player
        Vector3 localizedOffset = _player.transform.TransformPoint(_spawner.SpawnInfo.localPos);
        Vector3 desiredPos = localizedOffset + _tweenTarget.localPosition;

        // Smoothly interpolate between current camera position and desired position
        //Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPos, _playerFollowSmoothSpeed);

        // Smoothly interpolate camera position using Vector3.SmoothDamp
        if (_playerFollowSmoothSpeed > 0.0f) transform.localPosition = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, _playerFollowSmoothSpeed);
        else transform.localPosition = desiredPos;
    }
    #endregion


    private IEnumerator OffsetCoPunch(Vector3 targetOffset, float smoothIn, float smoothOut, float maxSpeed = 100.0f, bool resetFirst = true)
    {
        // Smooth camera transition
        Vector3 velocity = Vector3.zero;
        SpawnInfo spawnInfo = _spawner.SpawnInfo;
        Vector3 defaultOffset = _player.DefaultCameraOffset;

        // Reset
        if (resetFirst)
            spawnInfo.localPos = defaultOffset;

        // To target pos
        while (!Utils.AreVectorsEqual(spawnInfo.localPos, targetOffset))
        {
            spawnInfo.localPos = Vector3.SmoothDamp(spawnInfo.localPos, targetOffset, ref velocity, smoothIn, maxSpeed);
            yield return null;
        }

        // Back to default
        while (!Utils.AreVectorsEqual(spawnInfo.localPos, defaultOffset))
        {
            spawnInfo.localPos = Vector3.SmoothDamp(spawnInfo.localPos, defaultOffset, ref velocity, smoothOut, maxSpeed);
            yield return null;
        }

        spawnInfo.localPos = defaultOffset;
        yield return null;
    }

    private IEnumerator OffsetCo(Vector3 targetOffset, float smooth, float maxSpeed = 100.0f, bool resetFirst = true)
    {
        // Smooth camera transition
        Vector3 velocity = Vector3.zero;
        SpawnInfo spawnInfo = _spawner.SpawnInfo;
        Vector3 defaultOffset = _player.DefaultCameraOffset;

        // Reset
        if (resetFirst)
            spawnInfo.localPos = defaultOffset;

        // To target pos
        while (!Utils.AreVectorsEqual(spawnInfo.localPos, targetOffset))
        {
            spawnInfo.localPos = Vector3.SmoothDamp(spawnInfo.localPos, targetOffset, ref velocity, smooth, maxSpeed);
            yield return null;
        }
        spawnInfo.localPos = targetOffset;
        yield return null;
    }

    private void OnPlayerGrounded()
    {
        if (!_player) return;
        if (!(_landPunchDuration > 0.0f)) return;

        //Scale shake strength with kick force
        Collision groundedColl = _player.Movement.GroundedCollision;
        if (groundedColl == null) return;
        
        ResetCamera();

        float scalePerc = Mathf.InverseLerp(_landPunchScaleBounds.x, _landPunchScaleBounds.y, groundedColl.relativeVelocity.magnitude);
        Vector3 punchStrength = Vector3.Lerp(_landPunchStrengthMin, _landPunchStrengthMax, scalePerc);

        _currentTween = _tweenTarget.DOPunchPosition(punchStrength, _landPunchDuration, _landPunchVibrato, _landElasticity, false).SetEase(_landPunchEase, _landPunchOvershoot);
    }

    private void OnKickHitOrMiss()
    {
        if (!_player || !_playerKick) return;

        DoOffsetCoPunch(_kickTargetOffset, _kickSmoothIn, _kickSmoothOut);
    }

    private void OnKickHit(RaycastHit arg1, GameObject arg2, float arg3)
    {
        if (!_useScreenShake) return;

        ResetCamera();

        _currentTween = _mainCamera.DOShakePosition(_kickHitShakeDuration, _kickHitStrength, _kickHitShakeVibrato, 90, _kickHitShakeFadeOut, _shakeRandomnessMode)
            .SetEase(_kickHitShakeEase, _kickHitShakeOvershoot);
    }

    private void OnPlayerDamaged(float amount, GameObject source)
    {
        if (!_useScreenShake) return;

        ResetCamera();

        float scalePerc = Mathf.InverseLerp(_dmgShakeStrengthMin.x, _dmgShakeScaleBounds.y, amount);
        Vector3 shakeStrength = Vector3.Lerp(_dmgShakeStrengthMin, _dmgShakeStrengthMax, scalePerc);
        //shakeStrength = _tweenTarget.InverseTransformVector(shakeStrength);

        _currentTween = _mainCamera.DOShakePosition(_dmgShakeDuration, shakeStrength, _dmgShakeVibrato, 90, _dmgShakeFadeOut, _shakeRandomnessMode)
            .SetEase(_dmgShakeEase, _dmgShakeOvershoot);
    }

    private void OnPlayerJump()
    {
        if (!(_JumpPunchDuration > 0.0f)) return;

        ResetCamera();

        _currentTween = _tweenTarget.DOPunchPosition(_JumpPunchStrength, _JumpPunchDuration, _JumpPunchVibrato, 1.0f, false).SetEase(_JumpPunchEase, _JumpPunchOvershoot);
    }

    private void OnRespawn()
    {
        Resett();
    }

    private void Resett()
    {
        //sync the cached palyer eulerangles with potential new angles after setting them
        LocalEulerAngles = transform.localEulerAngles;
        
        ResetCamera();
    }

    private void ResetCamera()
    {
        //Kill tweens
        if (_currentTween != null) DOTween.Kill(_currentTween);

        //Reset local pos of camera
        _tweenTarget.localPosition = Vector3.zero;

        if (_mainCamera) _mainCamera.transform.localPosition = Vector3.zero;
    }
}