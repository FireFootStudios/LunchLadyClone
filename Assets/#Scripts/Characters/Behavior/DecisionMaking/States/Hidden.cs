using System;
using UnityEngine;

public sealed class Hidden : FSMState
{
    [SerializeField] private GameObject _visuals = null;
    [SerializeField] private bool _startHidden = false;
    [SerializeField] private bool _canBurrowOnDeAggro = false;
    [SerializeField] private float _resetDelay = 1.0f;

    [Space]
    [SerializeField] private ParticleSystem _onSpawnTemplate = null;
    [SerializeField] private float _onSpawnPsScale = 1.0f;
    [Space]
    [SerializeField] private SoundSpawnData _onSpawnSFX = null;


    private Character _char = null;
    private Health _health = null;

    private int hiddenCount = 0;

    public bool StartHidden { get { return _startHidden; } }
    public bool CanBurrowOnDeAggro { get { return _canBurrowOnDeAggro; } }


    public Action OnBurrow;
    public Action OnHide;
    public Action OnUnHide;

    private void Awake()
    {
        _char = GetComponentInParent<Character>();
        _health = _char.GetComponent<Health>();
        if (_health)
        {
            _health.OnRevive += () => hiddenCount = 0;
        }
    }

    public override void OnEnter()
    {
        if (!_char) return;

        //disable health so lifetime doesnt update
        if (_health) _health.enabled = false;

        //make sure we dont move
        if (_char.Movement)
        {
            _char.Movement.Stop();
            _char.Movement.CanRotate = false;
        }

        //dont burrow on first time entering hidden state as we start the game underground
        if (hiddenCount > 0)
        {
            OnBurrow?.Invoke();

            CancelInvoke();
            Invoke("Resett", _resetDelay);
        }
        else
        {
            OnHide?.Invoke();
            Resett();
        }
        hiddenCount++;
    }

    public override void OnExit()
    {
        //enable health
        if (_health) _health.enabled = true;

        //enable visuals
        if (_visuals) _visuals.SetActive(true);

        OnUnHide?.Invoke();

        //Feedback
        VFXManager.Instance.PlayVFXSimple(_onSpawnTemplate, transform.position, 0.0f, _onSpawnPsScale);
        SoundManager.Instance.PlaySound(_onSpawnSFX);

        CancelInvoke();
    }

    private void Resett()
    {
        //disable visuals
        if (_visuals) _visuals.SetActive(false);

        //set back to original pos
        _char.Movement.transform.position = _char.Spawner.SpawnInfo.pos;
    }
}
