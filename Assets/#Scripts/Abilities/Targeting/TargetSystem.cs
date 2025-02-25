using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TargetPair
{
    public GameObject target = null;
    public float effectiveness = 0.0f;

    public TargetPair(GameObject target, float effectiveness)
    {
        this.target = target;
        this.effectiveness = effectiveness;
    }
}

public enum TargetType { player, character, prop, platform, projectile }
[System.Serializable]
public sealed class TargetTypeMultiplier
{
    public TargetType type;
    public float multiplier;
}

public sealed class LosTarget
{
    public GameObject target = null;
    public float losTimer = 0.0f;

    public LosTarget(GameObject target, float losTimer)
    {
        this.target = target;
        this.losTimer = losTimer;
    }
}

public abstract class TargetSystem : MonoBehaviour
{
    #region Fields

    [SerializeField, Tooltip("For calculating effectiveness with each target")] protected List<Ability> _abilitiesToCheck = new List<Ability>();
    [SerializeField, Tooltip("Which tags to target, leave empty to target none")] protected List<Tag> _targetTags = new List<Tag>();
    [SerializeField] protected List<TargetTypeMultiplier> _targetTypeMultipliers = null;
    [SerializeField] private bool _treatNoTagsAsAnyTarget = false;

    [Space]
    [SerializeField] protected float _minEffectivenessForValid = 0.01f;
    [SerializeField] protected bool _canIncludeSelf = false;
    [SerializeField, Tooltip("How often should targets be updated (seconds)?")] private float _updateInterval = 0.2f;
    [SerializeField, Tooltip("Should the target system only look for targets when a script asks instead of every x seconds?")] private bool _updateOnAskInstead = false;
    [SerializeField, Tooltip("The default behavior (if set to false) will only update the target list when asked, if set to true it will be updated live")] private bool _updateTargetsDuringFire = false;

    [Space]
    [SerializeField] private float _maxVerticalAngle = 90.0f;

    [Header("Line of sight")]
    [SerializeField] private bool _useLineOfSight = false;
    [SerializeField] private LayerMask _losMask = ~0;
    [SerializeField] private Transform _losOriginT = null;
    [SerializeField] private float _losMaxDistance = 10.0f;
    [SerializeField] private float _losMissTargetLifetime = 5.0f;
    [SerializeField] private float _losTargetYOffset = 0.0f;
    [SerializeField] private float _xMinAccuracy = 0.0f;
    [SerializeField] private float _yMinAccuracy = 0.0f;


    private float _updateTargetsTimer = 0.0f;
    private GameObject _overrideTarget = null;

    protected List<TargetPair> _targetPairs = new List<TargetPair>();
    private List<GameObject> _targetGameObjects = new List<GameObject>();

    private List<LosTarget> _losTargets = new List<LosTarget>();

    #endregion
    #region Properties

    public List<Tag> TargetTags { get { return _targetTags; } }
    public GameObject OverrideTarget
    {
        get { return _overrideTarget; }
        set
        {
            if (!value) _overrideTarget = value;
            else if (IsTargetValid(value)) _overrideTarget = value;
        }
    }
    public GameObject Source { get { return _abilitiesToCheck.Count > 0 ? _abilitiesToCheck[0].Source : null; } }
    #endregion

    public Action<GameObject> OnHasFirstTarget;
    public Action OnLoseLastTarget;

    // Returns first target if any
    public TargetPair GetFirstTarget()
    {
        List<TargetPair> targetPairs = GetTargets();
        return targetPairs.Count > 0 ? targetPairs[0] : null;
    }

    public bool HasSpecificTarget(GameObject target)
    {
        List<TargetPair> targetPairs = GetTargets();
        return targetPairs.Find(tp => tp.target == target) != null;
    }

    public bool HasTarget()
    {
        return GetTargets().Count > 0;
    }

    public List<TargetPair> GetTargets()
    {
        if (_updateOnAskInstead)
        {
            _targetPairs.Clear();
            PopulateTargetPairs();
        }
        else
        {
            // Do a check if all targets are valid
            for (int i = 0; i < _targetPairs.Count; i++)
            {
                if (_targetPairs[i] == null || !_targetPairs[i].target || !_targetPairs[i].target.activeInHierarchy)
                {
                    _targetPairs.Remove(_targetPairs[i]);
                    i--;
                }
            }
        }
        return _targetPairs;
    }

    public List<GameObject> GetTargetsAsGameObjects()
    {
        RePopulateGameObjectTargets();
        return _targetGameObjects;
    }

    private void RePopulateGameObjectTargets()
    {
        List<TargetPair> targetPairs = GetTargets();

        // Clear any prev targets
        _targetGameObjects.Clear();

        // Add each target pair's gameobject
        targetPairs.ForEach(p => _targetGameObjects.Add(p.target));
    }

    protected abstract void UpdateCurrentTargetPairs();

    protected abstract void PopulateTargetPairs();

    private void Update()
    {
        UpdateOverrideTarget();
        UpdateTargetPairs();
        UpdateLosTargets();
    }

    private void UpdateTargetPairs()
    {
        if (_updateOnAskInstead) return;

        _updateTargetsTimer -= Time.deltaTime;
        int targetCount = _targetPairs.Count;
        if (_updateTargetsTimer < 0.0f)
        {
            _targetPairs.Clear();
            PopulateTargetPairs();
            _updateTargetsTimer = _updateInterval;

            // If false, this is only called when asked (so at start of ability)
            if (_updateTargetsDuringFire) RePopulateGameObjectTargets();
        }
        else
        {
            UpdateCurrentTargetPairs();

            // If a target was removed, repopulate target pairs!
            if (targetCount > _targetPairs.Count)
            {
                _targetPairs.Clear();
                PopulateTargetPairs();
                _updateTargetsTimer = _updateInterval;

                // If false, this is only called when asked (so at start of ability)
                if (_updateTargetsDuringFire) RePopulateGameObjectTargets();
            }
        }

        if (targetCount == 0 && _targetPairs.Count > 0)
        {
            OnHasFirstTarget?.Invoke(_targetPairs[0].target);
        }
        else if (targetCount > 0 && _targetPairs.Count == 0)
        {
            OnLoseLastTarget?.Invoke();
        }
    }

    private void UpdateOverrideTarget()
    {
        if (OverrideTarget && !OverrideTarget.activeInHierarchy && (OverrideTarget.TryGetComponent(out Health health) && health.IsDead))
            OverrideTarget = null;
    }

    protected virtual void Awake()
    {
        if (_abilitiesToCheck.Count == 0) Debug.Log("Target system has no linked abilities and will not work properly because of it.");
    }

    protected float EffectivenessTarget(GameObject target)
    {
        float effectiveness = 0.0f;
        foreach (Ability ability in _abilitiesToCheck)
        {
            effectiveness += ability.GetEffectiveness(target);
        }

        // Apply target type multipliers
        if (target && target.TryGetComponent(out Health targetHealth))
        {
            foreach (TargetType type in targetHealth.TargettingTypes)
            {
                TargetTypeMultiplier ttm = _targetTypeMultipliers.Find(ttm => ttm.type == type);
                if (ttm != null) effectiveness *= ttm.multiplier;
            }
        }

        return effectiveness > _minEffectivenessForValid ? effectiveness / _abilitiesToCheck.Count : effectiveness;
    }

    protected bool IsTargetValid(GameObject target)
    {
        if (Source && target == Source && !_canIncludeSelf) return false;

        if (TargetTags.Count == 0) return _treatNoTagsAsAnyTarget;

        // Validate tag
        bool validTag = false;
        foreach (Tag tag in TargetTags)
        {
            if (!target.CompareTag(TagManager.Instance.GetTagValue(tag))) continue;

            validTag = true;
            break;
        }

        if (!validTag) return validTag;

        // Validate angle
        if (_maxVerticalAngle < 90.0f && Source)
        {
            Vector3 dir = (target.transform.position - Source.transform.position).normalized;
            Vector3 dirhorizontal = dir;
            dirhorizontal.y = 0.0f;
            dirhorizontal.Normalize();

            // Should be between [-90,90]
            float verticalAngle = Vector3.Angle(dir, dirhorizontal);

            // Check if the calculated angle is within the allowed max vertical angle
            if (Mathf.Abs(verticalAngle) > _maxVerticalAngle) return false;
        }

        // Validate Line of Sight
        bool losValid = true;
        if (_useLineOfSight)
        {
            losValid = InLineOfSight(target);
            
            // Get target pair to reset timer OR check if still valid
            LosTarget losTarget = _losTargets.Find(l => l.target == target);
            if (losTarget == null && losValid)
            {
                // Create new los target
                losTarget = new LosTarget(target, _losMissTargetLifetime);
                _losTargets.Add(losTarget);
            }
            else if (losTarget != null && losValid)
            {
                // Reset the timer
                losTarget.losTimer = _losMissTargetLifetime;
            }
            else if (losTarget != null && losTarget.losTimer > 0.0f && !losValid) losValid = true;
        }

        if (!losValid) return false;

        return true;
    }

    // Gets or creates targetpair 
    //protected TargetPair CreateOrUpdateTargetPair(GameObject target, float effectiveness)
    //{
    //    if (!target) return null;

    //    TargetPair targetPair = _targetPairs.Find(p => p.target == target);

    //    if (targetPair == null)
    //    {
    //        targetPair = new TargetPair(target, effectiveness);
    //        _targetPairs.Add(targetPair);
    //    }
    //    else targetPair.effectiveness = effectiveness;

    //    return targetPair;
    //}

    private bool InLineOfSight(GameObject target)
    {
        if (!target) return false;

        Vector3 targetPos = target.transform.position;
        targetPos.y += _losTargetYOffset;
        Vector3 dirToTarget = (targetPos - _losOriginT.position).normalized;
        if (_losOriginT && Physics.Raycast(_losOriginT.transform.position, dirToTarget, out RaycastHit hitInfo, _losMaxDistance, _losMask) && hitInfo.collider.gameObject == target.gameObject)
        {
            return true;
        }

        return false;
    }

    private void UpdateLosTargets()
    {
        for (int i = 0; i < _losTargets.Count; i++)
        {
            LosTarget losTarget = _losTargets[i];

            losTarget.losTimer -= Time.deltaTime;
            if (losTarget.target && losTarget.target.activeInHierarchy && losTarget.losTimer > 0.0f) continue;

            // Remove los target
            _losTargets.RemoveAt(i);
            i--;
        }
    }
}
