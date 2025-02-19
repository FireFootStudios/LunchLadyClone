 using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
public sealed class CharAnimation : MonoBehaviour
{
    #region Fields
    [SerializeField] private bool _disableAnimatorOnDeath = false;
    [SerializeField, Tooltip("Bounds for scaling the move animation speed with the velocity percentage")] private Vector2 _moveAnimSpeedScaleBounds = new Vector2(0.5f, 1.0f);
    [SerializeField] private float _aggroChangeSpeed = 10.0f;

    [Space]
    //[SerializeField] private DialogueSpeaker _dialogueSpeaker = null;
    [SerializeField] private AnimationClip _defaultTalkingClip = null;

    private Character _character = null;
    private Hidden _hiddenState = null;//optional
    private Animator _animator = null;

    private AnimatorOverrideController _animOverride = null;
    private List<KeyValuePair<AnimationClip, AnimationClip>> _overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
    private KeyValuePair<AnimationClip, AnimationClip> _defaultAttackOverride;

    //current chains
    private List<AnimationClip> _attackChains = null;
    private int _chainCount = 0;
    private bool _chainProcessed = false;

    //strings (cached for performance)
    private const string _attackStr = "Attack";
    private const string _dieStr = "Die";
    private const string _cancelAttackStr = "CancelAttack";
    private const string _respawnStr = "Respawn";
    private const string _AggroFloatStr = "AggroFloat";
    private const string _velocityStr = "Velocity";
    private const string _moveModStr = "MoveMod";
    private const string _wantsMoveStr = "WantsMove";
    private const string _noMoreChainStr = "NoMoreChain";
    private const string _attackChainStr = "AttackChain";
    private const string _takeDamageStr = "TakeDamage";
    private const string _isHiddenStr = "IsHidden";
    private const string _hiddenStr = "Hidden";
    private const string _burrowStr = "Burrow";
    private const string _loopAttackStr = "LoopAttack";
    
            
    private const string _attackClipKeyName = "Slash Attack";//dont change this (its the name of the animation linked in the base animator controller)
    private int _attackOverrideIndex = -1;

    private float _aggroFloatCurrent = 0.0f;
    #endregion

    public bool IsInAttackState { get { return _animator.GetCurrentAnimatorStateInfo(0).IsTag(_attackStr); } }
    public bool IsInTransition { get { return _animator.IsInTransition(0); } }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _character = GetComponent<Character>();

        InitOverrides();
    }

    private void OnEnable()
    {
        if (!_animator) return;

        _aggroFloatCurrent = 0.0f;

        _animator.gameObject.SetActive(false);
        _animator.enabled = true;
        _animator.gameObject.SetActive(true);

        _animator.ResetTrigger(_dieStr);
        _animator.ResetTrigger(_respawnStr);
        _animator.ResetTrigger(_attackStr);
        _animator.ResetTrigger(_takeDamageStr);
        _animator.ResetTrigger(_burrowStr);

        //set initial state to hidden if desired (overrides default idle state)
        if (_hiddenState && _hiddenState.StartHidden)
        {
            SetHidden();
        }
    }

    private void OnDisable()
    {
        if (_disableAnimatorOnDeath) _animator.enabled = false;
    }

    private void Start()
    {
        InitEvents();
    }

    private void Update()
    {
        UpdateChain();
        UpdateVars();
    }

    private void InitOverrides()
    {
        if (!_animator || !_animator.runtimeAnimatorController) return;

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

        //find and save default attack override
        _attackOverrideIndex = _overrides.FindIndex(overide => overide.Key.name.Equals(_attackClipKeyName));
        if (_attackOverrideIndex < 0 || _attackOverrideIndex >= _overrides.Count) return;

        _defaultAttackOverride = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[_attackOverrideIndex].Key, _overrides[_attackOverrideIndex].Value);
    }

    private void InitEvents()
    {
        if (!_character || !_animator) return;

        if (_character.AttackBehaviour)
        {
            _character.AttackBehaviour.OnAttack += (animClipOverride, attackChains) =>
            {
                DoAction(animClipOverride, attackChains);
            };
            _character.AttackBehaviour.OnCancelAttack += () =>
            {
                _animator.ResetTrigger(_attackStr);
                _animator.ResetTrigger(_attackChainStr);
                //_animator.ResetTrigger(_takeDamageStr);
                _animator.SetTrigger(_cancelAttackStr);
            };
        }

        if (_character.Health)
        {

            //take damage anim
            _character.Health.OnDamaged += (dmg, source) =>
            {
                _animator.SetTrigger(_takeDamageStr);
            };


            //on death
            _character.Health.OnDeath += () =>
            {
                _animator.SetTrigger(_dieStr);
                _animator.ResetTrigger(_takeDamageStr);
                _animator.ResetTrigger(_attackStr);
            };
        }

        //on revive
        //_character.Health.OnRevived += () =>
        //{
        //    _animator.SetTrigger(_reviveStr);
        //    _animator.ResetTrigger(_takeDamageStr);
        //    _animator.ResetTrigger(_attackStr);

        //    //set this so we dont go in spawning animation (FSM updates frame after actual revive event)
        //    if (_hiddenState && _hiddenState.StartHidden)
        //        _animator.SetBool(_isHiddenStr, true);
        //};
        _character.Spawner.OnRespawn += () =>
        {
            _animator.SetTrigger(_respawnStr);
            _animator.ResetTrigger(_takeDamageStr);
            _animator.ResetTrigger(_attackStr);

            //set this so we dont go in spawning animation (FSM updates frame after actual revive event)
            if (_hiddenState && _hiddenState.StartHidden)
                _animator.SetBool(_isHiddenStr, true);
        };

        //if (_dialogueSpeaker)
        //{
        //    _dialogueSpeaker.OnTalkStart += OnTalkStart;
        //    _dialogueSpeaker.OnTalkEnd += OnTalkEnd;
        //}

        if (_hiddenState)
        {
            _hiddenState.OnBurrow += () =>
            {
                _animator.SetTrigger(_burrowStr);
                _animator.SetBool(_isHiddenStr, true);
            };
            _hiddenState.OnHide += () =>
            {
                _animator.SetBool(_isHiddenStr, true);
            };
            _hiddenState.OnUnHide += () =>
            {
                _animator.SetBool(_isHiddenStr, false);
            };
        }
    }

    private void DoAction(AnimationClip overrideClip, List<AnimationClip> chains)
    {
        if (!_animator) return;

        if (_animOverride && _overrides != null && _attackOverrideIndex >= 0 && _attackOverrideIndex < _overrides.Count)
        {
            //if override is not null and !+ current attack anim, set it as new override
            if (overrideClip && _overrides[_attackOverrideIndex].Value != overrideClip)
            {
                _overrides[_attackOverrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[_attackOverrideIndex].Key, overrideClip);
                _animOverride.ApplyOverrides(_overrides);
            }
            //if override is null, set current override back to default override
            else if (!overrideClip && _overrides[_attackOverrideIndex].Value != _defaultAttackOverride.Value)
            {
                _overrides[_attackOverrideIndex] = _defaultAttackOverride;
                _animOverride.ApplyOverrides(_overrides);
            }

            //chain stuff
            if (chains != null) _attackChains = chains;
            else if (_attackChains != null) _attackChains.Clear();

            _chainCount = 0;
            if (_attackChains != null && _attackChains.Count > 0) _animator.SetBool(_noMoreChainStr, false);
            else _animator.SetBool(_noMoreChainStr, true);
        }

        _animator.ResetTrigger(_cancelAttackStr);
        _animator.ResetTrigger(_attackChainStr);
        _animator.ResetTrigger(_takeDamageStr);
        _animator.SetTrigger(_attackStr);
    }

    //private void OnTalkStart(DialogueEntry entry)
    //{
    //    if (!_animator) return;

    //    AnimationClip overrideClip = entry.talkingAnimationOverride;
    //    DoAction(overrideClip != null ? overrideClip : _defaultTalkingClip, null);

    //    _animator.SetBool(_loopAttackStr, true);
    //}

    //private void OnTalkEnd()
    //{
    //    if (!_animator) return;

    //    _animator.SetTrigger(_cancelAttackStr);
    //    _animator.SetBool(_loopAttackStr, false);
    //}

    private void UpdateVars()
    {
        if (!_character || !_animator) return;

        // Incorperate move anim mod in here as velocity is already multiplied with move animations
        if (_character.Movement)
        {
            // Velocity perc
            _animator.SetFloat(_velocityStr, _character.Movement.VelocityPercentage);

            // Move mod -> Use velocity percontage to calculate final move speed mod (scale between bounds)
            _animator.SetFloat(_moveModStr, Mathf.Lerp(_moveAnimSpeedScaleBounds.x, _moveAnimSpeedScaleBounds.y, _character.Movement.VelocityPercentage));

            // Wants move
            _animator.SetBool(_wantsMoveStr, !_character.Movement.IsStopped && _character.Movement.DesiredMovement != Vector3.zero);
        }
        else if (_character.NavMeshAgent)
        {
            // Velocity perc
            float velPerc = _character.NavMeshAgent.velocity.magnitude / _character.NavMeshAgent.speed;
            _animator.SetFloat(_velocityStr, velPerc);

            // Move mod -> Use velocity percontage to calculate final move speed mod (scale between bounds)
            _animator.SetFloat(_moveModStr, Mathf.Lerp(_moveAnimSpeedScaleBounds.x, _moveAnimSpeedScaleBounds.y, velPerc));

            // Wants move
            _animator.SetBool(_wantsMoveStr, !_character.NavMeshAgent.isStopped && _character.NavMeshAgent.desiredVelocity.magnitude > 0.0f);
        }

        // Is aggro
        bool isAggro = _character.Behaviour.HasAggroTarget;
        _aggroFloatCurrent = Mathf.Lerp(_aggroFloatCurrent, isAggro ? 1.0f : 0.0f, _aggroChangeSpeed * Time.deltaTime);
        _animator.SetFloat(_AggroFloatStr, _aggroFloatCurrent);
    }

    private void UpdateChain()
    {
        //if no attack chains return
        if (_attackChains == null) return;

        //if in transition return
        if (_animator.IsInTransition(0)) return;

        //get current anim state info (check if attack anim finished, if so, either set no more chain or trigger a chain and set anim)
        AnimatorStateInfo animStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (!animStateInfo.IsTag(_attackStr)) return;

        //if attack not finished, return
        if (animStateInfo.normalizedTime < 1.0f)
        {
            _chainProcessed = false;
            return;
        }

        //only apply chain once (once next chain anim has played this will be reset)
        if (_chainProcessed) return;

        //check if anymor chains
        if (_chainCount < _attackChains.Count)
        {
            //set correct anim clip, bool and trigger
            AnimationClip chainAnim = _attackChains[_chainCount];
            if (chainAnim && _overrides[_attackOverrideIndex].Value != chainAnim)
            {
                _overrides[_attackOverrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[_attackOverrideIndex].Key, chainAnim);
                _animOverride.ApplyOverrides(_overrides);
            }

            _chainProcessed = true;
            _animator.SetTrigger(_attackChainStr);
            _chainCount++;
        }
        else
        {
            _chainCount = 0;
            _attackChains = null;
            _animator.SetBool(_noMoreChainStr, true);
        }
    }

    private void SetHidden()
    {
        //set initial state to hidden if desired
        if (!_hiddenState || !_hiddenState.StartHidden) return;

        _animator.SetBool(_isHiddenStr, true);
        _animator.Play(_hiddenStr);
        _animator.Update(0.0f);
    }
}