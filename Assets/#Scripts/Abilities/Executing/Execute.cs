using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public sealed class Execute
{
    [Header("General"), SerializeField] private List<Effect> _effects = new List<Effect>();
    [SerializeField, Tooltip("Optional effect in charge of providing targets for other effects, will be fired before any other effects and will override any and all targets used for them")] private TargettingEffect _targettingEffect = null;
    [SerializeField, Tooltip("Amount of seconds after which this execute will fire")] private float _time = 0.0f;
    [SerializeField, Tooltip("Amount of times to loop this execute")] private int _loops = 0;
    [SerializeField, Tooltip("Time before the first and between each loop")] private float _loopDuration = 0.2f;
    [SerializeField, Tooltip("If multiple valid targets are available, a random one will be picked each time one is needed")] private bool _pickRandomTarget = false;
    [SerializeField, Tooltip("Should targets be passed to the execute or ignored? (excludes targetting effects)")] private bool _ignoreTargets = false;
    [SerializeField, Tooltip("Checks if a target is still in range before applying the effects (TargetSystem only)")] private bool _checkInRange = true;
    //[SerializeField] private bool _killSelfOnExecute = false;

    [Header("SFX"), SerializeField] private List<SoundSpawnData> _sounds = new List<SoundSpawnData>();
    [SerializeField] private bool _soundRequireTarget = false;
    [SerializeField] private bool _soundUseTargetPos = false;

    [Header("VFX")]
    [SerializeField] private List<VFXSpawner> _vfxSpawners = null;

    private Transform _originT = null; //temp for during execute
    private bool _initialized = false;

    public bool Executed { get; private set; }
    public float CurrentLoops { get; private set; }
    public float Time { get { return _time + (Executed ? _loopDuration * (CurrentLoops + 1) : 0.0f); } }
    public bool PickRandomTarget { get { return _pickRandomTarget; } }
    public bool IgnoreTargets { get { return _ignoreTargets; } }

    public TargetSystem TargetSystem { get; set; } // optional, set by executer

    public Execute Copy(GameObject target)
    {
        Execute copy = new Execute();

        //copy effects
        foreach (Effect effect in _effects)
            copy._effects.Add(effect.Copy(target));

        //copy targetting effect
        if (_targettingEffect)
        {
            _targettingEffect.OnTargetsReady -= ResolveTargettingEffect;
            copy._targettingEffect = _targettingEffect.Copy(target) as TargettingEffect;
        }

        copy._initialized = false;
        copy._time = _time;
        copy._loops = _loops;
        copy._loopDuration = _loopDuration;
        copy._pickRandomTarget = _pickRandomTarget;
        copy._ignoreTargets = _ignoreTargets;
        copy._checkInRange = _checkInRange;
        copy.TargetSystem = TargetSystem;

        copy._vfxSpawners = _vfxSpawners;
        copy._sounds = _sounds;
        copy._soundRequireTarget = _soundRequireTarget;
        copy._soundUseTargetPos = _soundUseTargetPos;

        return copy;
    }

    public void Reset()
    {
        Executed = false;
        CurrentLoops = 0;

        if (_targettingEffect) _targettingEffect.OnReset();

        foreach (Effect effect in _effects)
            effect.OnReset();
    }

    public void CleanUp()
    {
        if (_targettingEffect) _targettingEffect.OnCleanUp();

        foreach (Effect effect in _effects)
            effect.OnCleanUp();
    }

    public bool CanExecute()
    {
        if (_targettingEffect && !_targettingEffect.CanApply()) return false;

        foreach (Effect effect in _effects)
            if (!effect.CanApply()) return false;

        return true;
    }

    public void OnCancel()
    {
        if (IsFinished()) return;

        if (_targettingEffect) _targettingEffect.OnCancel();

        foreach (Effect effect in _effects) 
            effect.OnCancel();
    }

    public bool IsFinished()
    {
        if (!Executed) return false;
        if (CurrentLoops < _loops) return false;
        if (_targettingEffect && !_targettingEffect.IsFinished()) return false;

        foreach (Effect effect in _effects)
            if (!effect.IsFinished()) return false;

        return true;
    }

    public void DoExecute(GameObject target, Transform originT)
    {
        if (Executed && CurrentLoops == _loops) return;

        Init();

        // Validate execute
        if (Executed) CurrentLoops++;
        Executed = true;

        // Check if target still in range (if desired)
        if (_checkInRange && TargetSystem && !_ignoreTargets && !TargetSystem.HasSpecificTarget(target)) return;

        // Cache originT
        _originT = originT;

        // If targetting effect, apply this first, collect targets and apply each effect on each target
        if (_targettingEffect)
        {
            // Apply targetting effect
            _targettingEffect.Apply(target, originT);
        }
        else
        {
            // Apply general effects
            foreach (Effect effect in _effects)
                effect.Apply(target, originT);
        }

        SpawnVFX(target);
        ExecuteSounds(target);
    }

    public void DoExecuteList(List<GameObject> targets, Transform originT, int maxTargets)
    {
        if (Executed && CurrentLoops == _loops) return;

        Init();

        //validate execute
        if (Executed) CurrentLoops++;
        Executed = true;

        //cache originT
        _originT = originT;

        for (int i = 0; i < targets.Count && i < maxTargets; i++)
        {
            GameObject target = targets[i];

            //pick random target
            if (PickRandomTarget)
                target = targets[UnityEngine.Random.Range(0, targets.Count)];

            //check if target still in range (if desired)
            if (_checkInRange && TargetSystem && !_ignoreTargets && !TargetSystem.HasSpecificTarget(target)) continue;

            // If targetting effect, apply this first, collect targets and apply each effect on each target
            if (_targettingEffect)
            {
                //apply targetting effect
                _targettingEffect.Apply(target, originT);
            }
            else
            {
                // Apply general effects
                foreach (Effect effect in _effects)
                    effect.Apply(target, originT);
            }

            SpawnVFX(target);
            ExecuteSounds(target);
        }
    }

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

        //subscribe to targetting effect
        if (_targettingEffect) _targettingEffect.OnTargetsReady += ResolveTargettingEffect;
    }

    private void ResolveTargettingEffect()
    {
        //apply general effects upon targets from targetting effect
        foreach (GameObject target in _targettingEffect.Targets)
            foreach (Effect effect in _effects)
                effect.Apply(target, _originT);

        //_targettingEffect.StartCoroutine(ApplyTargettingEffects());
    }

    //private IEnumerator ApplyTargettingEffects()
    //{
    //    if (_targettingEffect.ApplyEffectsDelay > 0.0f)
    //        yield return new WaitForSeconds(_targettingEffect.ApplyEffectsDelay);

    //    //apply general effects upon targets from targetting effect
    //    foreach (GameObject target in _targettingEffect.Targets)
    //        foreach (Effect effect in _effects)
    //            effect.Apply(target, _originT);

    //    yield return null;
    //}

    public float GetEffectiveness(GameObject target)
    {
        int nrEffects = _effects.Count;
        float effectiveness = 0.0f;

        //targetting effect
        if (_targettingEffect)
        {
            effectiveness += _targettingEffect.GetEffectiveness(target);
            nrEffects++;
        }

        //general effects
        foreach (Effect effect in _effects)
        {
            effectiveness += effect.GetEffectiveness(target);
        }

        return effectiveness > 0.0f ? effectiveness / nrEffects : 0.0f;
    }

    private void ExecuteSounds(GameObject target)
    {
        if (_sounds.Count == 0) return;
        if (_soundRequireTarget && !target) return;

        foreach (SoundSpawnData soundSpawnerData in _sounds)
        {
            if (_soundUseTargetPos && target) soundSpawnerData.StartPos = target.transform.position;
            else if (!soundSpawnerData.parent)
                soundSpawnerData.StartPos = _originT.transform.position;
            
            SoundManager.Instance.PlaySound(soundSpawnerData);
        }
    }

    private void SpawnVFX(GameObject target)
    {
        if (_vfxSpawners == null || _vfxSpawners.Count == 0) return;

        foreach (VFXSpawner vfxSpawner in _vfxSpawners)
        {
            vfxSpawner.Spawn(target, _originT);
        }
    }


    /////////////////////////////////////////////
    public enum SpawnMode { self, target, spawnTs }
    [System.Serializable]
    public sealed class VFXSpawner
    {
        [SerializeField] private VFXData _data = null;
        [SerializeField, Tooltip("Method of spawning")] private SpawnMode _spawnMode = 0;
        [SerializeField, Tooltip("Only necessary if spawn mode is spawnT")] private List<Transform> _spawnTs = null;
        [SerializeField, Tooltip("If more than 1 spawnTs are used, set this to the desired amount to pick that many, randomly, on ask. Leave 0 otherwise")] private int _spawnTAmount = 0;

        public void Spawn(GameObject target, Transform originT)
        {
            int loopAmount = 1;

            GameObject vfxTarget = null;
            switch (_spawnMode)
            {
                case SpawnMode.self:
                    vfxTarget = originT.gameObject;
                    break;

                case SpawnMode.target:
                    vfxTarget = target;
                    break;

                case SpawnMode.spawnTs:

                    if (_spawnTAmount > 0)
                    {
                        loopAmount = _spawnTAmount;
                        _spawnTs.Shuffle();
                    }
                    else loopAmount = _spawnTs.Count;
                    break;
            }

            for (int i = 0; i < loopAmount; i++)
            {
                if (_spawnMode == SpawnMode.spawnTs) vfxTarget = _spawnTs[i].gameObject;

                //TODO: we might have to copy the data here because of source being a possible ref that can be changed if
                // multiple loops or ability is fired again
                _data.source = vfxTarget;

                //get vfx object and play
                VFXObject vfxObject = VFXManager.Instance.GetVFXObject(_data, true);
                if(vfxObject != null) vfxObject.Play();
            }
        }
    }
}