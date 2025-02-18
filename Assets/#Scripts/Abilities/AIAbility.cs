using System.Collections.Generic;
using UnityEngine;

public sealed class AiAbility : Ability
{
    #region Fields
    [Header("AI"), SerializeField] private bool _rotateToTarget = true;
    [SerializeField] private float _maxRotationDiffBetweenTarget = 5.0f; //degrees [1,179]
    [SerializeField] private bool _rotateWhileFiring = true;
    [SerializeField] private bool _includeXRotation = false;
    [SerializeField] private bool _ignoreForAttackRange = false;
    [SerializeField] private bool _requireAggro = false;
    [SerializeField, Space, Tooltip("Allow move while firing")] private bool _allowMove = false;
    [SerializeField, Tooltip("If allowMove is enabled, this will determine the min distance for movement (too prevent running too close)")] private float _stopDistance = 1.0f;
    #endregion

    #region Properties
    public bool IgnoreForAttackRange { get { return _ignoreForAttackRange; } }
    public bool RequireAggro { get { return _requireAggro; } }
    public bool RotateWhileFiring { get { return _rotateWhileFiring; } }
    public bool AllowMove { get { return _allowMove; } }
    public float StopDistance { get { return _stopDistance; } }
    #endregion


    public override bool CanFire(float generalCooldownTimer, bool checkRotation = true)
    {
        if (checkRotation && !RotatedToTarget()) return false;

        return base.CanFire(generalCooldownTimer);
    }

    public Vector3 DesiredRot()
    {
        if (!_rotateToTarget || !TargetSystem) return transform.forward;

        List<TargetPair> targets = TargetSystem.GetTargets();
        if (targets.Count == 0) return transform.forward;
        return targets[0].target.transform.position - transform.position;
    }

    public bool RotatedToTarget()
    {
        if (!_rotateToTarget || !TargetSystem) return true;

        List<TargetPair> targets = TargetSystem.GetTargets();
        //check if angle to target is smaller than maxRotDiff
        if (targets.Count > 0 && targets[0] != null && targets[0].target)
        {
            //if target is ourselves, just return true!
            if (targets[0].target == Source) return true;

            Vector3 dirToTarget = targets[0].target.transform.position - transform.position;
            Vector3 currentDir = transform.forward;

            //ignore y element? (x rotation)
            if (!_includeXRotation)
            {
                dirToTarget.y = 0.0f;
                currentDir.y = 0.0f;
            }
            return Vector3.Angle(dirToTarget.normalized, currentDir.normalized) < _maxRotationDiffBetweenTarget;
        }
        return false;
    }
}