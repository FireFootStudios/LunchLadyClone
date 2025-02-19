using System.Collections.Generic;
using UnityEngine;


public sealed class HasAggroTarget : FSMTransition
{
    private Character _char = null;

    public HasAggroTarget(Character character) { _char = character; }


    public override bool Evaluate(GameObject go)
    {
        if (!_char) return false;

        return _char.Behaviour.HasAggroTarget;
    }
}

public sealed class IsSpawning : FSMTransition
{
    private Character _char = null;

    public IsSpawning(Character character) { _char = character; }

    public override bool Evaluate(GameObject go)
    {
        if (!_char) return false;

        return _char.Health.LifeElapsed < _char.Behaviour.SpawnDelay;
    }
}

public sealed class CanBurrow : FSMTransition
{
    private Hidden _hiddenState = null;

    public CanBurrow(Hidden hiddenState) 
    {
        _hiddenState = hiddenState;
    }

    public override bool Evaluate(GameObject go)
    {
        if (!_hiddenState) return false;

        return _hiddenState.CanBurrowOnDeAggro;
    }
}

public sealed class InSpawnRange : FSMTransition
{
    private Character _char = null;
    private float _rangeSquared = 25.0f;
    private HitBox _spawnZone = null;

    public InSpawnRange(Character character, float range)
    { _char = character; _rangeSquared = range * range; }

    public InSpawnRange(Character character, HitBox spawnZone)
    { _char = character; _spawnZone = spawnZone; }

    public override bool Evaluate(GameObject _)
    {
        if (!_char) return false;

        //If no spawnzones do distance check
        if (_spawnZone == null)
            return (_char.transform.position - _char.Spawner.SpawnInfo.pos).sqrMagnitude < _rangeSquared;

        //Check hitboxes, if even 1 zone is populate with themselves, return true
        if (_spawnZone.Targets.Contains(_char.gameObject))
            return true;

        return false;
    }
}

public sealed class CanCombat : FSMTransition
{
    private Character _char = null;
    public CanCombat(Character character)
    { _char = character; }

    public override bool Evaluate(GameObject go)
    {
        if (!_char || !_char.AttackBehaviour) return false;


        if (!_char.AttackBehaviour.IsInAttackRange() || !_char.AttackBehaviour.CanAttack()) return false;

        if (_char.Behaviour.DesiredDistanceToTarget > 0.0f)
        {
            // Distance to first target
            float distance = _char.AttackBehaviour.DistanceToTarget();
            if (distance > 0.0f && distance > _char.Behaviour.DesiredDistanceToTarget) return false;
        }
        return true;
    }
}

public sealed class LeaveCombat : FSMTransition
{
    private Character _char = null;
    public LeaveCombat(Character character)
    { _char = character; }

    public override bool Evaluate(GameObject go)
    {
        if (!_char || !_char.AttackBehaviour) return false;

        return !_char.AttackBehaviour.IsInAttackRange() && !_char.AttackBehaviour.CanAttack() && !_char.AttackBehaviour.IsAttacking();
    }
}

public sealed class IsDead : FSMTransition
{
    private Character _char = null;
    public IsDead(Character character)
    { _char = character; }

    public override bool Evaluate(GameObject go)
    {
        if (!_char) return false;

        return _char.Health.IsDead;
    }
}

