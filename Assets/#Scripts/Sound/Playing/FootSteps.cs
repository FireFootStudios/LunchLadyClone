using System.Collections.Generic;
using UnityEngine;

public sealed class FootSteps : MonoBehaviour
{
    [SerializeField] private FreeMovement _movement = null;
    [SerializeField] private SurfaceAsset _defaultSurfaceAsset = null;
    [SerializeField, Tooltip("Only useful if player, disable otherwise")] private bool _useSurfaces = false;

    [Header("Stepping"), SerializeField] private SoundSpawnData _stepSpawnData = null;

    [SerializeField] private float _stepFrequency = 1.0f;
    [SerializeField, Tooltip("Max speed at which frequeceny will be scaled")] private float _maxSpeed = 8.0f;
    [SerializeField, Tooltip("For how long will stepping continue after being ungrounded")] private float _maxAirTimeForStep = 0.2f;

    [SerializeField] private List<Ability> _cooldownOnAbilityFire = new List<Ability>();
    [SerializeField] private float _onAbilityFireCD = 0.5f;

    [Space]
    [SerializeField, Tooltip("Extra step SFX spawned ignoring surfaces no matter what setting")] private List<SoundSpawnData> _extraStepSFX = new List<SoundSpawnData>();


    [Header("Landing"), SerializeField] private List<SoundSpawnData> _landSpawnData = new List<SoundSpawnData>();
    [SerializeField, Tooltip("Min air time before playing land sounds on grounded")] private float _minAirTimeForLand = 0.5f;

    [Header("Jumping"), SerializeField]  private List<SoundSpawnData> _jumpSpawnData = new List<SoundSpawnData>();
    [SerializeField] private Ability _jumpAbility = null;

    private SoundManager _soundManager = null;
    private Animator _animator;//optional

    //Used when no overrides for current surfaceType (or no surface type found)
    private Surface _mostRecentSurface = null;

    private float _stepElapsed = 0.0f;


    public Surface Surface { get { return  _movement.CurrentSurface != null ? _movement.CurrentSurface : _mostRecentSurface; } }


    private void Awake()
    {
        //Cache stuff
        _soundManager = SoundManager.Instance;
        _animator = GetComponent<Animator>();

        if (_movement) _movement.OnGrounded += OnGrounded;

        foreach (Ability ability in _cooldownOnAbilityFire)
            ability.OnFire += () => _stepElapsed = -_onAbilityFireCD;

        if (_jumpAbility) _jumpAbility.OnFire += OnJump;
    }

    private void Update()
    {
        //Update most recent surface, as it might have changed witout ever grounding
        if (_movement && _movement.CurrentSurface != null) _mostRecentSurface = _movement.CurrentSurface;

        UpdateStepSounds();
    }

    // Called by potential animation events
    private void Step(AnimationEvent evt)
    {
        // Blend trees will all fire events, this will only allow event if this is highest weighted clip
        if (!IsHeaviestAnimClip(evt.animatorClipInfo.clip)) return;

        Step();
    }

    private void Step()
    {
        // Find surface clips
        if (_useSurfaces)
        {
            SurfaceAsset surfaceAsset = GetSurfaceAsset();
            if (!surfaceAsset) return;

            _stepSpawnData.clips = surfaceAsset.StepClips;
        }

        _stepSpawnData.StartPos = transform.position;
        _soundManager.PlaySound(_stepSpawnData);

        // Extra step SFX
        if (_extraStepSFX.Count > 0)
        {
            foreach (SoundSpawnData spawnData in _extraStepSFX)
            {
                spawnData.StartPos = transform.position;
                _soundManager.PlaySound(spawnData);
            }
        }
    }

    bool IsHeaviestAnimClip(AnimationClip currentClip)
    {
        var currentAnimatorClipInfo = _animator.GetCurrentAnimatorClipInfo(0);
        float highestWeight = 0f;
        AnimationClip highestWeightClip = null;

        // Find the clip with the highest weight
        foreach (var clipInfo in currentAnimatorClipInfo)
        {
            if (clipInfo.weight > highestWeight)
            {
                highestWeight = clipInfo.weight;
                highestWeightClip = clipInfo.clip;
            }
        }

        return highestWeightClip != null && currentClip == highestWeightClip;
    }

    private void UpdateStepSounds()
    {
        //if animator, return (since steps will be played using animation events)
        if (_animator) return;

        //check if walking
        bool isWalking = (_movement.IsGrounded || _movement.IsStepping || _movement.AirTime < _maxAirTimeForStep) /*&& Movement.DesiredMovement != Vector3.zero*/;
        if (!isWalking) return;

        //update timer and check if can play sound (Scale with vel perc but include y velocity in case stepping)
        float speed = Mathf.Clamp(_movement.RB.linearVelocity.magnitude, 0.0f, _maxSpeed);
        _stepElapsed += Time.deltaTime * (speed / _maxSpeed);
        if (_stepElapsed > _stepFrequency)
        {
            Step();
            _stepElapsed -= _stepFrequency;
        }
    }

    private void OnGrounded()
    {
        //Cache as most recent surface
        _mostRecentSurface = _movement.CurrentSurface;

        //play landed
        if (_movement.AirTime < _minAirTimeForLand) return;

        //Swap out clips based on surface?
        if (_useSurfaces)
        {
            SurfaceAsset surfaceAsset = GetSurfaceAsset();
            if (!surfaceAsset) return;

            List<AudioClip> clips = surfaceAsset.LandClips;
            foreach (SoundSpawnData spawnData in _landSpawnData)
                spawnData.clips = clips;
        }

        //Play land sound(s)
        foreach (SoundSpawnData spawnData in _landSpawnData)
        {
            spawnData.StartPos = transform.position;
            _soundManager.PlaySound(spawnData);
        }

        //reset
        _stepElapsed = 0.0f;
    }

    private void OnJump()
    {
        //Swap out clips based on surface?
        if (_useSurfaces)
        {
            SurfaceAsset surfaceAsset = GetSurfaceAsset();
            if (!surfaceAsset) return;

            List<AudioClip> clips = surfaceAsset.JumpClips;
            foreach (SoundSpawnData spawnData in _jumpSpawnData)
                spawnData.clips = clips;
        }

        //Play jump sound(s)
        foreach (SoundSpawnData spawnData in _jumpSpawnData)
        {
            spawnData.StartPos = transform.position;
            _soundManager.PlaySound(spawnData);
        }
    }

    private SurfaceAsset GetSurfaceAsset()
    {
        //If in air
        if (!_movement.IsGrounded)
            return _mostRecentSurface ? _mostRecentSurface.Asset : _defaultSurfaceAsset;

        //If grounded
        return _movement.CurrentSurface ? _movement.CurrentSurface.Asset : _defaultSurfaceAsset;
    }
}