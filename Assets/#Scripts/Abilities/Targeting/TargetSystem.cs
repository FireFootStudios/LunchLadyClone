using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetSystem : MonoBehaviour
{
    #region Fields

    [SerializeField, Tooltip("For calculating effectiveness with each target")] protected List<Ability> _abilitiesToCheck = new List<Ability>();
    [SerializeField, Tooltip("Which tags to target, leave empty to target none")] protected List<Tag> _targetTags = new List<Tag>();
    [SerializeField] protected List<TargetTypeMultiplier> _targetTypeMultipliers = null;
    [SerializeField] private bool _treatNoTagsAsAnyTarget = false;
    [SerializeField] private bool _random = false;
    [SerializeField, Tooltip("How long do targets stay in target system after becoming invalid?")] private float _targetLifeTime = 0.0f;

    [Space]
    [SerializeField] protected float _minEffectivenessForValid = 0.01f;
    [SerializeField] protected bool _canIncludeSelf = false;
    [SerializeField, Tooltip("How often should targets be updated (seconds)?")] private float _updateInterval = 0.2f;
    [SerializeField, Tooltip("Should the target system only look for targets when a script asks instead of every x seconds?")] private bool _updateOnAskInstead = false;
    [SerializeField, Tooltip("The default behavior (if set to false) will only update the target list when asked, if set to true it will be updated live")] private bool _updateTargetsDuringFire = false;

    [Space]
    [SerializeField, Range(0.0f, 90.0f)] private float _maxVerticalAngle = 90.0f;
    [SerializeField, Range(0.0f, 180.0f)] private float _maxHorizontalAngle = 180.0f;

    [Header("Line of sight")]
    [SerializeField] private bool _useLineOfSight = false;
    [SerializeField] private LineOfSight _losData = null;
    [SerializeField] private float _losTargetLifetime = 0.0f;

    [Space]
    [SerializeField] private bool _reverseTargetValidation = false;


    private float _updateTargetsTimer = 0.0f;

    protected List<TargetPair> _targetPairs = new List<TargetPair>();
    private List<TargetPair> _outputBuffer = new List<TargetPair>();

    private List<GameObject> _targetGameObjects = new List<GameObject>();

    private List<LosTarget> _losTargets = new List<LosTarget>();

    #endregion
    #region Properties

    public List<Tag> TargetTags { get { return _targetTags; } }

    public GameObject Source { get { return _abilitiesToCheck.Count > 0 ? _abilitiesToCheck[0].Source : null; } }
    public float DefaultTargetLifeTime { get { return _updateInterval + _targetLifeTime; } }

    #endregion

    public Action<GameObject> OnHasFirstTarget;
    public Action OnLoseLastTarget;

    // Returns first target if any
    public TargetPair GetFirstTarget()
    {
        List<TargetPair> targetPairs = GetTargets();
        return targetPairs.Count > 0 ? targetPairs[0] : null;
    }

    // Force adds a target to this target system, this does not check if it is a valid target in any way!
    public void AddOverrideTarget(GameObject target, float effectiveness, float duration)
    {
        AddOrUpdateTarget(target, effectiveness, duration);
    }

    public void RemoveOverrideTarget(GameObject target)
    {
        if (!target) return;

        TargetPair targetPair = _targetPairs.Find(p => p.target == target);
        RePopulateTargetPairs();
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
        if (_updateOnAskInstead) RePopulateTargetPairs();
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

    private void RePopulateTargetPairs()
    {
        _outputBuffer.Clear();
        PopulateTargetPairs(ref _outputBuffer);

        // Add/Update new targets
        foreach (TargetPair outputTarget in _outputBuffer)
            AddOrUpdateTarget(outputTarget);

        // Sort OR randomise list
        if (_targetPairs.Count > 1)
        {
            if (_random) _targetPairs.Shuffle();
            else
            {
                // Sort targets based on caculated effectivenesses
                _targetPairs.Sort((a, b) =>
                {
                    return b.effectiveness.CompareTo(a.effectiveness);
                });
            }
        }
    }

    protected void AddOrUpdateTarget(GameObject target, float eff, float lifeTime)
    {
        TargetPair pair = _targetPairs.Find(p => p.target == target);
        if (pair == null)
        {
            pair = new TargetPair(target, eff, lifeTime);
            _targetPairs.Add(pair);
        }
        else
        {
            pair.effectiveness = eff;
            pair.lifeElapsed = 0.0f;
            pair.lastValidPos = target.transform.position;

            // Pick best lifetime
            if (lifeTime > pair.lifetime) pair.lifetime = lifeTime;
        }
    }

    protected void AddOrUpdateTarget(TargetPair pair)
    {
        AddOrUpdateTarget(pair.target, pair.effectiveness, pair.lifetime);
    }

    protected virtual void UpdateCurrentTargetPairs()
    {
        // Check if target null or has 0 effectiveness, remove if any true
        for (int i = 0; i < _targetPairs.Count; i++)
        {
            TargetPair targetPair = _targetPairs[i];

            if (targetPair == null || !targetPair.target || !targetPair.target.activeInHierarchy)
            {
                _targetPairs.RemoveAt(i);
                i--;
                continue;
            }

            // Lifetime
            targetPair.lifeElapsed += Time.deltaTime;
            if (targetPair.lifeElapsed > targetPair.lifetime)
            {
                _targetPairs.RemoveAt(i);
                i--;
                continue;
            }

            // Recalculate effectiveness
            targetPair.effectiveness = EffectivenessTarget(targetPair.target);
            if (targetPair.effectiveness < _minEffectivenessForValid)
            {
                _targetPairs.RemoveAt(i);
                i--;
                continue;
            }
        }
    }

    protected abstract void PopulateTargetPairs(ref List<TargetPair> targetPairs);

    private void Update()
    {
        UpdatePopulating();
        UpdateLosTargets();
    }

    private void UpdatePopulating()
    {
        if (_updateOnAskInstead) return;

        _updateTargetsTimer -= Time.deltaTime;
        int targetCount = _targetPairs.Count;
        if (_updateTargetsTimer < 0.0f)
        {
            RePopulateTargetPairs();
            _updateTargetsTimer = _updateInterval;

            // If false, this is only called when asked (so at start of ability)
            if (_updateTargetsDuringFire) RePopulateGameObjectTargets();
        }
        else
        {
            UpdateCurrentTargetPairs();

            // If a target was removed, repopulate target pairs also!
            if (targetCount > _targetPairs.Count)
            {
                RePopulateTargetPairs();
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
        if (!target) return false;
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

        GameObject fromGo = Source;
        GameObject toTarget = target;
        if (_reverseTargetValidation)
        {
            fromGo = target;
            toTarget = Source;
        }

        // Validate/Update Line of Sight
        bool losValid = true;
        if (_useLineOfSight)
        {
            losValid = _losData.InLineOfSight(toTarget, toTarget.transform.position, fromGo.transform.position);

            // Get target pair to reset timer OR check if still valid
            LosTarget losTarget = _losTargets.Find(l => l.target == toTarget);
            if (losTarget == null && losValid)
            {
                // Create new los target
                losTarget = new LosTarget(toTarget, _losTargetLifetime, toTarget.transform.position);
                _losTargets.Add(losTarget);
            }
            else if (losTarget != null && losValid)
            {
                // Reset timer + update last seen pos
                losTarget.losTimer = _losTargetLifetime;
                losTarget.lastSeenPos = toTarget.transform.position;
            }
            else if (losTarget != null && losTarget.losTimer > 0.0f && !losValid) losValid = true;
        }

        if (!losValid) return false;


        Vector3 dirToTarget = (toTarget.transform.position - fromGo.transform.position).normalized;

        // Check horizontal angle
        if (_maxHorizontalAngle < 179.0f && fromGo)
        {
            Vector3 forwardXZ = new Vector3(fromGo.transform.forward.x, 0, fromGo.transform.forward.z).normalized;
            Vector3 dirToTargetXZ = new Vector3(dirToTarget.x, 0, dirToTarget.z).normalized;

            float horizontalAngle = Vector3.Angle(forwardXZ, dirToTargetXZ);
            if (horizontalAngle > _maxHorizontalAngle)
                return false;
        }

        // Check vertical angle

        // One way is to compute the elevation angle for both vectors.
        // Elevation is defined as the angle above the XZ plane.
        if (_maxVerticalAngle < 89.0f && fromGo)
        {
            float targetElevation = Mathf.Atan2(dirToTarget.y, new Vector2(dirToTarget.x, dirToTarget.z).magnitude) * Mathf.Rad2Deg;
            float forwardElevation = Mathf.Atan2(fromGo.transform.forward.y, new Vector2(fromGo.transform.forward.x, fromGo.transform.forward.z).magnitude) * Mathf.Rad2Deg;

            float verticalAngle = Mathf.Abs(targetElevation - forwardElevation);
            if (verticalAngle > _maxVerticalAngle)
                return false;
        }

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

[System.Serializable]
public sealed class TargetPair
{
    public GameObject target = null;
    public float effectiveness = 0.0f;
    public float lifetime = 0.0f;
    public float lifeElapsed = 0.0f;

    // Last target pos while valid
    public Vector3 lastValidPos = Vector3.zero;

    public TargetPair(GameObject target, float effectiveness)
    {
        this.target = target;
        this.effectiveness = effectiveness;
    }

    public TargetPair(GameObject target, float effectiveness, float lifetime)
    {
        this.target = target;
        this.effectiveness = effectiveness;
        this.lifetime = lifetime;
        this.lastValidPos = target.transform.position;
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
    public Vector3 lastSeenPos = Vector3.zero;

    public LosTarget(GameObject target, float losTimer, Vector3 lastSeenPos)
    {
        this.target = target;
        this.losTimer = losTimer;
    }
}

[System.Serializable]
public sealed class LineOfSight
{
    public LayerMask _mask = ~0;
    public Transform _originT = null;
    public float _maxDistance = 10.0f;
    public float _targetOffset = 0.0f;
    public float _fromOffset = 0.0f;


    //public bool InLineOfSight(GameObject target)
    //{
    //    if (!target) return false;

    //    return InLineOfSight(target.transform.position);
    //}

    public bool InLineOfSight(GameObject target, Vector3 targetPos)
    {
        if (!_originT) return false;

        return InLineOfSight(target, targetPos, _originT.position);
    }

    public bool InLineOfSight(GameObject target, Vector3 targetPos, Vector3 fromPos)
    {
        if (!target) return false;

        targetPos.y += _targetOffset;
        fromPos.y += _fromOffset;
        Vector3 dirToTarget = (targetPos - fromPos).normalized;

        if (Physics.Raycast(fromPos, dirToTarget, out RaycastHit hitInfo, _maxDistance, _mask) && hitInfo.collider.gameObject == target.gameObject)
        {
            return true;
        }

        return false;
    }
}