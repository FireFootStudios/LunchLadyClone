using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator = null;
    [SerializeField, Tooltip("Bounds for scaling the move animation speed with the velocity percentage")] private Vector2 _moveAnimSpeedScaleBounds = new Vector2(0.5f, 1.0f);
    [SerializeField] private List<Ability> _ignoreForAction = new List<Ability>();

    [SerializeField] private Vector2 _moveSpeedScaleBounds = new Vector2(0.0f, 8.0f);
    [SerializeField] private float _moveInputSmoothSpeed = 2.0f;

    [Space]
    [SerializeField] private Vector2 _kickAngleBlendBounds = new Vector2(15.0f, 120.0f);

    [Space]
    [SerializeField] private float _minAirTimeForLandAnim = 0.4f;

    private PlayerN _player = null;

    private const string _hasMoveInputStr = "HasMoveInput";
    private const string _velScaleStr = "VelScale";
    private const string _sprintingStr = "IsSprinting";
    private const string _crouchedStr = "IsCrouching";
    private const string _actionLoopingStr = "ActionLooping";
    private const string _moveInputX = "MoveInputX";
    private const string _moveInputY = "MoveInputY";
    private const string _downStr = "IsDown";
    private const string _kickingStr = "IsKicking";
    //private const string _kickTrigger = "Kic";
    private const string _kickAngleStr = "KickAngle";


    private const string _actionStr = "Action";
    private const string _cancelActionStr = "CancelAction";


    private AnimatorOverrideController _animOverride = null;
    private List<KeyValuePair<AnimationClip, AnimationClip>> _overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
    private KeyValuePair<AnimationClip, AnimationClip> _defaultActionOverride;

    private const string _actionClipKeyName = "Armature|Grab";//dont change this (its the name of the animation linked in the base animator controller)
    private int _actionOverrideIndex = -1;

    // Current chains
    private List<AnimationClip> _actionChains = null;
    private int _chainCount = 0;
    private bool _chainProcessed = false;

    private Ability _lastActionAbility = null;
    private Kick _playerKick = null;

    private Vector2 _currentMoveInput = Vector2.zero;


    private void Awake()
    {
        _player = GetComponent<PlayerN>();

        //InitOverrides();

        _player.Health.OnDeath += OnDeath;
        _player.Health.OnRevive += OnRevive;

        _playerKick = _player.GetComponentInChildren<Kick>();
        if (_playerKick) _playerKick.OnKickHitOrMiss += OnKickHitOrMiss;
    }

    private void OnKickHitOrMiss()
    {
        _animator.SetTrigger(_kickingStr);

        Vector3 kickDir = _playerKick.KickDir;

        // Calculate the angle between the kick direction and the vertical axis (up direction)
        float angle = Vector3.Angle(kickDir, Vector3.down);

        // Normalize the angle [0,1]
        float normalizedAngle = Mathf.InverseLerp(_kickAngleBlendBounds.x, _kickAngleBlendBounds.y, angle);
        Debug.Log("Kick angle: " + normalizedAngle);
        _animator.SetFloat(_kickAngleStr, normalizedAngle);
    }

    private void OnEnable()
    {
        if (!_player.IsOwner) return;

        _animator.ResetTrigger(_actionStr);
    }

    private void Start()
    {
        //InitEvents();
    }

    private void Update()
    {
        UpdateVars();
    }

    /*private void InitOverrides()
    {
        //setup for overriding
        List<KeyValuePair<AnimationClip, AnimationClip>> defaultOverrides = null;
        if (_animator.runtimeAnimatorController is AnimatorOverrideController)
        {
            defaultOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            (_animator.runtimeAnimatorController as AnimatorOverrideController).GetOverrides(defaultOverrides);
        }

        _animOverride = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _animOverride;

        //set already existing overrides if there
        _animOverride.GetOverrides(_overrides);
        if (defaultOverrides != null)
        {
            for (int i = 0; i < defaultOverrides.Count; i++)
            {
                KeyValuePair<AnimationClip, AnimationClip> overide = defaultOverrides[i];
                _overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overide.Key, overide.Value);
            }
        }
        _animOverride.ApplyOverrides(_overrides);

        //find and save default action override
        _actionOverrideIndex = _overrides.FindIndex(overide => overide.Key.name.Equals(_actionClipKeyName));
        _defaultActionOverride = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[_actionOverrideIndex].Key, _overrides[_actionOverrideIndex].Value);
    }

    private void InitEvents()
    {
        _player.AbilityManager.OnFire += (Ability ability) =>
        {
            if (_ignoreForAction.Contains(ability)) return;

            if (_animOverride && _overrides != null)
            {
                AnimationClip animClipOverride = ability.BaseAnimationOverride;

                //if override is not null and !+ current action anim, set it as new override
                if (animClipOverride && _overrides[_actionOverrideIndex].Value != animClipOverride)
                {
                    _overrides[_actionOverrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[_actionOverrideIndex].Key, animClipOverride);
                    _animOverride.ApplyOverrides(_overrides);
                }
                //if override is null, set current override back to default override
                else if (!animClipOverride && _overrides[_actionOverrideIndex].Value != _defaultActionOverride.Value)
                {
                    _overrides[_actionOverrideIndex] = _defaultActionOverride;
                    _animOverride.ApplyOverrides(_overrides);
                }
            }

            _animator.ResetTrigger(_cancelActionStr);
            _animator.SetTrigger(_actionStr);

            _animator.SetBool(_actionLoopingStr, ability.IsLooping);

            //Cache this for cancelling
            _lastActionAbility = ability;
            //chain stuff
            //_actionChains = actionChains;
            //_chainCount = 0;
            //if (_actionChains != null && _actionChains.Count > 0) _animator.SetBool(_noMoreChainStr, false);
            //else _animator.SetBool(_noMoreChainStr, true);
        };
    

        _player.AbilityManager.OnAbilityCancelled += (Ability ability) =>
        {
            if (_ignoreForAction.Contains(ability)) return;

            //TODO: check if actually in action state AND check if this we are in the actual animation of this ability (or the defualt one if that is used)
            if (ability != _lastActionAbility) return;

            _animator.ResetTrigger(_actionStr);
            _animator.SetTrigger(_cancelActionStr);
        };

        _player.AbilityManager.OnAbilityEnded += (Ability ability) =>
        {
            if (_ignoreForAction.Contains(ability)) return;

            if (ability != _lastActionAbility) return;

            //In case pref ability was set to looping in animator, set this to false once finished
            _animator.SetBool(_actionLoopingStr, false);

            //Cancel animation?
            if (!ability.KeepAnimAlive) _animator.SetTrigger(_cancelActionStr);
        };

        _player.Movement.OnGrounded += OnGrounded;
    }

    */
    private void UpdateVars()
    {
        if (!_player.IsOwner) return;

        // MOVE INPUT
        _animator.SetBool(_hasMoveInputStr, _player.HasMoveInput == true && _player.Movement.CurrentMoveSpeed > 0.25f);
        _animator.SetBool(_sprintingStr, _player.IsSprinting);
        //_animator.SetBool(_kickingStr, _player.KickAbility && _player.KickAbility.IsFiring);
        _animator.SetBool(_crouchedStr, _player.CrouchAbility && _player.CrouchAbility.IsFiring);

        // Calculate move input values (localized to player and scaled to maxspeed)
        Vector3 targetMoveInput = Vector3.zero;
        if (_player.Movement.MaxSpeedAdjusted > 0.0f)
            targetMoveInput = _player.transform.InverseTransformVector(_player.Movement.CurrentMoveVelocity) / _player.Movement.MaxSpeedAdjusted;

        // Lerp towards target move input values for smooth transitions
        _currentMoveInput = Vector2.Lerp(_currentMoveInput, new Vector2(targetMoveInput.x, targetMoveInput.z), Time.deltaTime * _moveInputSmoothSpeed);

        _animator.SetFloat(_moveInputX, _currentMoveInput.x);
        _animator.SetFloat(_moveInputY, _currentMoveInput.y);

        //VELOCITY SCALE
        //Move mod -> Use velocity percontage to calculate final move speed mod (scale between bounds)
        //_animator.SetFloat(_velScaleStr, Mathf.Lerp(_moveAnimSpeedScaleBounds.x, _moveAnimSpeedScaleBounds.y, _player.Movement.VelocityPercentage));
    }

    private void OnDeath()
    {
        _animator.SetBool(_downStr, true);
    }

    private void OnRevive()
    {
        _animator.SetBool(_downStr, false);
    }
}