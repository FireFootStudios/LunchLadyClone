using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AbilityManager))]
public sealed class AttackBehaviour : MonoBehaviour
{
    #region Fields

    private CharAnimation _charAnimation = null;
    private CharBehaviour _charBehaviour = null;

    #endregion

    #region Properties

    public AbilityManager AbilityManager { get; private set; }
    public AiAbility PreferedAbility { get; private set; }

    #endregion

    public Action<AnimationClip, List<AnimationClip>> OnAttack = null;//default anim override + attack chain animations
    public Action OnCancelAttack = null;


    public bool IsInAttackRange()
    {
        foreach (AiAbility ability in AbilityManager.Abilities)
        {
            if (ability.IgnoreForAttackRange) continue;
            if (!ability.HasEnoughTargets()) continue;

            if (ability.RequireAggro && (_charBehaviour && !_charBehaviour.HasAggroTarget)) continue;

            return true;
        }

        return false;
    }

    // Abilities could be used but have no targets (aka not in range)
    //public bool AbilitiesRequireAttackRange()
    //{
    //    foreach (AiAbility ability in AbilityManager.Abilities)
    //    {
    //        if (ability.IgnoreForAttackRange) continue;
    //        if (ability.IsOnCooldown) continue;
    //        if (ability.HasEnoughTargets()) continue;

    //        // If ability has not effectiveness for aggro target also continue
    //        //if(ability.GetEffectiveness())

    //        return true;
    //    }


    //    return false;
    //}

    public bool CouldUseAbilityIfInRange()
    {
        foreach (AiAbility ability in AbilityManager.Abilities)
        {
            if (ability.IgnoreForAttackRange) continue;
            if (ability.IsOnCooldown) continue;
            // If ability has not effectiveness for aggro target also continue
            //if(ability.GetEffectiveness())

            return true;
        }


        return false;
    }

    public bool IsAttacking()
    {
        return (AbilityManager.CurrentFiring) || IsInAttackAnim();
    }

    public bool CanAttack()
    {
        if (!HasAbilityWithEnoughTargets(false)) return false;
        if (IsAttacking()) return false;

        return true;
    }

    public bool HasAbilityWithEnoughTargets(bool includeOnCooldown = true)
    {
        foreach (AiAbility ability in AbilityManager.Abilities)
        {
            if (!ability.HasEnoughTargets()) continue;
            if (!includeOnCooldown && ability.IsOnCooldown) continue;
            if (ability.RequireAggro && (_charBehaviour && !_charBehaviour.HasAggroTarget)) continue;

            return true;
        }
        return false;
    }

    public Vector3 GetDesiredForward()
    {
        if (!PreferedAbility || (PreferedAbility.IsFiring && !PreferedAbility.RotateWhileFiring))
        {
            return transform.forward;
        }
        else if (PreferedAbility.HasTargets())
        {
            return PreferedAbility.DesiredRot();
        }
        else if (_charBehaviour.HasAggroTarget)
        {
            return (_charBehaviour.AggroTargetSystem.GetFirstTarget().target.transform.position - transform.position);
        }

        return transform.forward;
    }

    // Will return negative if invalid
    public float DistanceToTarget()
    {
        if (PreferedAbility && PreferedAbility.HasTargets())
        {
            return (PreferedAbility.TargetSystem.GetFirstTarget().target.transform.position - transform.position).magnitude;
        }
        else if (_charBehaviour && _charBehaviour.HasAggroTarget)
        {
            return (_charBehaviour.AggroTargetSystem.GetFirstTarget().target.transform.position - transform.position).magnitude;
        }

        return -1.0f;
    }

    //Vector to preferred target
    public Vector3 GetDesiredMovement()
    {
        if (!PreferedAbility && !_charBehaviour.HasAggroTarget) return transform.forward;

        GameObject target = null;
        if (PreferedAbility && PreferedAbility.TargetSystem && PreferedAbility.HasTargets()) target = PreferedAbility.TargetSystem.GetFirstTarget().target;
        else if (_charBehaviour.AggroTargetSystem && _charBehaviour.AggroTargetSystem.GetTargets().Count > 0) target = _charBehaviour.AggroTargetSystem.GetFirstTarget().target;

        if (target) return target.transform.position - transform.position;

        return transform.forward;
    }

    public bool IsRotatedToTarget()
    {
        return PreferedAbility && PreferedAbility.RotatedToTarget();
    }

    public bool IsInAttackAnim()
    {
        if (!_charAnimation) return false;
        if (_charAnimation.IsInAttackState || _charAnimation.IsInTransition) return true;
        return false;
    }

    private void Awake()
    {
        AbilityManager = GetComponent<AbilityManager>();
        _charAnimation = GetComponent<CharAnimation>();
        _charBehaviour = GetComponent<CharBehaviour>();

        foreach (Ability Ability in AbilityManager.Abilities)
        {
            Ability.OnCancel += () => { OnCancelAttack?.Invoke(); };
        }
    }

    private void OnDisable()
    {
        if (PreferedAbility && PreferedAbility.IsFiring)
        {
            PreferedAbility.Cancel();
        }

        //always call on cancel attack as we might still be in animation
        OnCancelAttack?.Invoke();
    }

    private void Update()
    {
        UpdatePreferedAbility();
        UpdateAttacking();
    }

    private void UpdatePreferedAbility()
    {
        if (IsAttacking()) return;

        //update prefered ability based on effectiveness, individual cooldown
        PreferedAbility = null;
        float bestEffPrefered = -0.0001f;

        //first try find an ability that is usable (exclude rotation because we can only rotate for an ability once ability it is prefered)
        foreach (AiAbility ability in AbilityManager.Abilities)
        {
            if (!AbilityManager.CanUseAbility(ability, false)) continue;
            if (ability.RequireAggro && (_charBehaviour && !_charBehaviour.HasAggroTarget)) continue;

            float effectiveness = ability.CalculateCurrentEffectiveness();
            if (effectiveness > bestEffPrefered)
            {
                bestEffPrefered = effectiveness;
                PreferedAbility = ability;
            }
        }

        //if found, return
        if (PreferedAbility) return;

        //find most effective ability which cannot be used (for rotating purposes), if no targets ability gets ignored
        foreach (AiAbility ability in AbilityManager.Abilities)
        {
            if (!ability.HasEnoughTargets()) continue;
            if (ability.RequireAggro && (_charBehaviour && !_charBehaviour.HasAggroTarget)) continue;

            float effectiveness = ability.CalculateCurrentEffectiveness();
            if (effectiveness > bestEffPrefered)
            {
                bestEffPrefered = effectiveness;
                PreferedAbility = ability;
            }
        }
    }

    private void UpdateAttacking()
    {
        if (IsAttacking()) return;

        if (AbilityManager.TryUseAbility(PreferedAbility))
        {
            OnAttack?.Invoke(PreferedAbility.BaseAnimationOverride, PreferedAbility.AttackChains);
        }
    }
}