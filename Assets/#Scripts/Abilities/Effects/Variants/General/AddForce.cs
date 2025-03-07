using Unity.Netcode;
using UnityEngine;

public sealed class AddForce : Effect
{
    [Header("General"), SerializeField, Tooltip("Main target force")] private float _force = 10.0f;
    [SerializeField, Tooltip("If this target is a referrer (is part of a bigger object), you can specify a force to be added to that target also")] private float _referrerForce = 0.0f;
    [SerializeField, Tooltip("If this target ,or whatever it is referring 2, has an active ragdoll controller, this value will add force to the main ragdoll RB")] private float _mainRagdollForce = 0.0f;

    [SerializeField] private ForceMode _forceMode = ForceMode.VelocityChange;
    [SerializeField] private bool _resetTargetVelocity = false;
    [SerializeField, Tooltip("Will take away force if needed before adding new force so to not exceed this valued (-1 == disable)")] private float _maxSpeedTargetHorizontal = -1.0f;

    [Header("Direction"), SerializeField, Tooltip("Ignore the heigh difference between target and this transform")] private bool _ignoreY = true;
    [SerializeField, Tooltip("Angle in degrees around the X axis applied to force direction if not 0")] private float _xAngle = 0.0f;
    [SerializeField, Tooltip("Use the origin transform (object that executes) for calculating the knockback direction")] private bool _useOriginT = true;
    [SerializeField, Tooltip("Use a directionT instead")] private Transform _directionT = null;
    [SerializeField] private Transform _fromT = null;


    public float Force { get { return _force; } }
    public ForceMode ForceMode { get { return _forceMode; } }


    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        // We require time scale to be bigger than zero (so not paused) cuz unity will stack all of the force for next frame!!!
        if (!target || !(Time.timeScale > 0.0f)) return;

        if (target.TryGetComponent(out FreeMovement movement))
        {
            AddForceToMovementTarget(movement, originT, effectMods);
        }
        else AddForceToTarget(target, originT, effectMods);


        EffectReferrer referrer = target.GetComponent<EffectReferrer>();

        //  If target is a referrer, AND a referrer force higher than 0 has been set, add the appropriate force to that GO as well
        //if (_referrerForce > 0.0f && referrer)
        //{
        //    AddForceToTarget(_referrerForce, referrer.ReferToGo, originT, effectMods);
        //}

        //if (_mainRagdollForce > 0.0f)
        //{
        //    RagdollController ragdollController = target.GetComponent<RagdollController>();
        //    if (!ragdollController && referrer) ragdollController = referrer.ReferToGo.GetComponent<RagdollController>();

        //    if (ragdollController) AddForceToTarget(_mainRagdollForce, ragdollController.MainRagdollRB.gameObject, originT, effectMods);
        //}
    }


    private void AddForceToTarget(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!target) return;

        Rigidbody targetRB = target.GetComponent<Rigidbody>();
        if (!targetRB) return;

        // Reset vel
        if (_resetTargetVelocity && !targetRB.isKinematic) targetRB.linearVelocity = Vector3.zero;

        // Calculate final force
        Vector3 finalForce = CalculateForce(target, originT, effectMods);

        // In case we have a max speed target cap, validate and adjust its velocity before adding new force so we do not exceed this cap
        if (_maxSpeedTargetHorizontal > 0.0f) ValidateVelocity(finalForce, targetRB);

        // Finally add the force vector to target's the rigidbody
        targetRB.AddForce(finalForce, _forceMode);
    }

    // This requires some checks as movement is networked and thus will need to check ownership
    private void AddForceToMovementTarget(FreeMovement movement, Transform originT, EffectModifiers effectMods)
    {
        if (!movement || !movement.IsSpawned) return;

        // Calculate final force
        Vector3 finalForce = CalculateForce(movement.gameObject, originT, effectMods);

        //// Movement is networked and needs a RPC to set force
        //if (movement.IsOwner) movement.RB.AddForce(finalForce, _forceMode);
        //else movement.AddForceServerRpc(movement.OwnerClientId, finalForce, _forceMode);

        if (movement.IsOwner) movement.RB.AddForce(finalForce, _forceMode);
        else if (movement.IsServer) movement.AddForceClientRPC(finalForce, _forceMode, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { movement.OwnerClientId } }
        });
        else movement.AddForceServerRpc(movement.OwnerClientId, finalForce, _forceMode);
    }

    public Vector3 CalculateForce(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        // Calculate direction
        Vector3 direction = transform.forward;
        Transform fromT = _useOriginT ? originT : (_fromT ? _fromT : transform);
        if (_directionT) direction = _directionT.forward;
        else if (target)
        {
            Transform targetT = target.transform;
            direction = targetT.position - fromT.position;
        }

        if (_ignoreY) direction.y = 0.0f;
        direction.Normalize();

        // X angle override?
        if (!Utils.IsFloatZero(_xAngle)) direction = Vector3.RotateTowards(direction, fromT.up, _xAngle * Mathf.Deg2Rad, 0.0f);

        // Modifier?
        float effectMod = 1.0f;
        if (effectMods) effectMod = effectMods.GetModifier(EffectModifierType.knockback);

        Vector3 finalForce = _force * effectMod * direction;
        return finalForce;
    }

    // Horizontal only
    private void ValidateVelocity(Vector3 force, Rigidbody targetRB)
    {
        Vector3 predictedAddedVel = Utils.HorizontalVector(force);
        Vector3 targetVel = Utils.HorizontalVector(targetRB.linearVelocity);

        //Calculate velocity that would be added by applying move force (F = v / t), if acceleration only
        if (_forceMode == ForceMode.Acceleration) predictedAddedVel *= Time.deltaTime;

        float targetSpeed = targetVel.magnitude;
        float toAddSpeed = predictedAddedVel.magnitude;
        float predictedTotalSpeed = targetSpeed + toAddSpeed;

        //would we exceed max speed by adding the current force amount? (also check if we would actually increase speed)
        if (predictedTotalSpeed > _maxSpeedTargetHorizontal && predictedTotalSpeed > targetSpeed)
        {
            //there might be some speed still left before reaching max speed
            float speedAllowed = Mathf.Clamp(_maxSpeedTargetHorizontal - targetSpeed, 0.0f, float.MaxValue);

            //calculate new speed by lowering current velocity untill we are at (max - speedToAdd) and set on rigidbody
            float newSpeed = targetSpeed - (toAddSpeed - speedAllowed);
            Vector3 newVel = targetVel.normalized * newSpeed;

            //Keep vertical velocity
            newVel.y = targetRB.linearVelocity.y;

            //Set new velocity
            targetRB.linearVelocity = newVel;
        }
    }

    protected override float Effectiveness(GameObject target)
    {
        
        return target && target.TryGetComponent(out Rigidbody rb)/* && !rb.isKinematic */? 1.0f : 0.0f;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}