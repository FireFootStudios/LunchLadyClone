using UnityEngine;

[RequireComponent(typeof(PlayerN))]
public sealed class PlayerVelocityFeedback : MonoBehaviour
{
    [SerializeField] private Vector2 _generalVelScaleBounds = new Vector2(1.0f, 30.0f);

    [Header("FOV")]
    [SerializeField] private Vector2 _fovChangeScaleBounds = new Vector2(0.0f, 50.0f);
    [SerializeField] private float _fovChangeSpeed = 5.0f;

    [Header("Wind")]
    [SerializeField] private Vector2 _windVelScaleBounds = new Vector2(10.0f, 50.0f);
    [SerializeField] private SoundSpawnData _windSFX = null;
    [SerializeField] private Vector2 _windVolumeBounds = new Vector2(0.0f, 1.0f);
    [SerializeField] private Vector2 _windPitchBounds = new Vector2(0.8f, 1.5f);
    [SerializeField] private float _windChangeSpeed = 1.0f;

    //camera shake? (falling only)
    //Pitch certain sounds?
    //Blur screen edges?


    private PlayerN _player = null;

    private Sound _windSound = null;


    private void Awake()
    {
        _player = GetComponent<PlayerN>();
    }

    private void OnDisable()
    {
        if (_windSound) _windSound.Stop();
    }

    private void LateUpdate()
    {
        float velPerc = Mathf.InverseLerp(_generalVelScaleBounds.x, _generalVelScaleBounds.y, _player.Movement.RB.linearVelocity.magnitude);

        UpdateFOV(velPerc);
        UpdateWindSFX();
    }

    private void UpdateFOV(float velPerc)
    {
        if (PlayingState.IsPaused) return;
        if (!_player || !_player.PlayerCameras) return;

        Camera playerCamera = _player.PlayerCameras.MainCamera;
        if (!playerCamera) return;

        float targetFOV = _player.PlayerCameras.DefaultFOV;
        if (velPerc > 0.0f || !_player.Movement.IsGrounded)
        {
            targetFOV += Mathf.Lerp(_fovChangeScaleBounds.x, _fovChangeScaleBounds.y, velPerc);
        }

        // Lerp fov
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, _fovChangeSpeed * Time.unscaledDeltaTime);
    }

    private void UpdateWindSFX()
    {
        Vector3 velocity = _player.Movement.RB.linearVelocity;
        float velPerc = Mathf.InverseLerp(_windVelScaleBounds.x, _windVelScaleBounds.y, velocity.magnitude);

        float targetVolume = 0.0f;
        float targetPitch = 1.0f;

        if (velPerc > 0.0f)
        {
            targetVolume = Mathf.Lerp(_windVolumeBounds.x, _windVolumeBounds.y, velPerc);
            targetPitch = Mathf.Lerp(_windPitchBounds.x, _windPitchBounds.y, velPerc);

            if (_windSound == null || _windSound.IsFinished)
            {
                _windSFX.volume = 0.0f;
                _windSFX.pitch = 1.0f;
                SoundManager.Instance.PlayReUseSound(_windSFX, ref _windSound);
            }
        }

        //Lerp
        if (_windSound != null)
        {
            _windSound.AudioSource.volume = Mathf.Lerp(_windSound.AudioSource.volume, targetVolume, _windChangeSpeed * Time.deltaTime);
            _windSound.AudioSource.pitch = Mathf.Lerp(_windSound.AudioSource.pitch, targetPitch, _windChangeSpeed * Time.deltaTime);
        }
    }
}