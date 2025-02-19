using System.Collections.Generic;

public sealed class NavmMeshCharBeh : CharBehaviour
{
    protected override void InitFSM()
    {
        Character character = GetComponent<Character>();

        // STATES
        Idle idle = _statesGo.GetComponent<Idle>();
        Wander wander = _statesGo.GetComponent<Wander>();
        MoveToSpawn moveToSpawn = _statesGo.GetComponent<MoveToSpawn>();
        Combat combat = _statesGo.GetComponent<Combat>();
        Dead dead = _statesGo.GetComponent<Dead>();
        Aggro aggro = _statesGo.GetComponent<Aggro>();
        Flee flee = _statesGo.GetComponent<Flee>();
        Patrol patrol = _statesGo.GetComponent<Patrol>();
        Hidden hidden = _statesGo.GetComponent<Hidden>();

        // TRANSITIONS
        CanBurrow canHide = new CanBurrow(hidden);
        HasAggroTarget hasAggroTarget = new HasAggroTarget(character);
        InSpawnRange isInSpawnRange = _spawnZone ? new InSpawnRange(character, _spawnZone) : new InSpawnRange(character, _maxRangeFromSpawn);
        CanCombat canCombat = new CanCombat(character);
        LeaveCombat leaveCombat = new LeaveCombat(character);
        IsDead isDead = new IsDead(character);
        IsSpawning isSpawning = new IsSpawning(character);

        //ORDER MATTERS (eg: check if dead first before checking whether to move) (first transitions to return true will be picked)
        //sorted on from states (as these transitions only get checked when in that state)

        // If we deaggro on spawn exit then we require to be in spawn range before we can aggro
        List<FSMTransition> canAggro = new List<FSMTransition>() { hasAggroTarget };
        if (_deAggroOnSpawnExit) canAggro.Add(isInSpawnRange);

        if (hidden)
        {
            FSM.AddState(hidden);
            FSM.AddTransitionList(canAggro, hidden, idle);
        }

        // From idle
        if (idle)
        {
            FSM.AddState(idle);
            FSM.AddTransition(isDead, idle, dead);

            //stay in idle if spawning
            FSM.AddTransition(isSpawning, idle, null, true, true);

            FSM.AddTransitionList(canAggro, idle, flee);
            FSM.AddTransition(hasAggroTarget, idle, patrol, false);
            FSM.AddTransition(hasAggroTarget, idle, wander, false);
            FSM.AddTransition(canCombat, idle, combat);
            FSM.AddTransitionList(canAggro, idle, aggro);
            FSM.AddTransition(isInSpawnRange, idle, moveToSpawn, false);
            FSM.AddTransition(canHide, idle, hidden);
        }

        // From wander
        if (wander)
        {
            FSM.AddState(wander);
            FSM.AddTransition(isDead, wander, dead);
            FSM.AddTransitionList(canAggro, wander, flee);
            FSM.AddTransitionList(canAggro, wander, aggro);
            FSM.AddTransition(isInSpawnRange, wander, moveToSpawn, false);
            FSM.AddTransition(canCombat, wander, combat);
        }

        // From patrol
        if (patrol)
        {
            FSM.AddState(patrol);
            FSM.AddTransition(isDead, patrol, dead);
            FSM.AddTransitionList(canAggro, patrol, flee);
            FSM.AddTransitionList(canAggro, patrol, aggro);
            FSM.AddTransition(canCombat, patrol, combat);
        }

        // From MoveToSpawn
        if (moveToSpawn)
        {
            FSM.AddState(moveToSpawn);
            FSM.AddTransition(isDead, moveToSpawn, dead);
            FSM.AddTransitionList(canAggro, moveToSpawn, flee);
            FSM.AddTransitionList(canAggro, moveToSpawn, aggro);

            List<FSMTransition> burrow = new List<FSMTransition> { canHide, isInSpawnRange };
            FSM.AddTransitionList(burrow, moveToSpawn, hidden);
            FSM.AddTransition(isInSpawnRange, moveToSpawn, idle);
            FSM.AddTransition(canCombat, moveToSpawn, combat);
        }

        // From aggro
        if (aggro)
        {
            FSM.AddState(aggro);
            FSM.AddTransition(isDead, aggro, dead);
            FSM.AddTransition(canCombat, aggro, combat);
            FSM.AddTransition(hasAggroTarget, aggro, idle, false);
            if (_deAggroOnSpawnExit) FSM.AddTransition(isInSpawnRange, aggro, moveToSpawn, false);
        }

        // From flee
        if (flee)
        {
            FSM.AddState(flee);
            FSM.AddTransition(isDead, flee, dead);
            FSM.AddTransitionList(canAggro, flee, idle, false);
            FSM.AddTransition(canCombat, flee, combat);
        }

        // From combat
        if (combat)
        {
            FSM.AddState(combat);
            FSM.AddTransition(isDead, combat, dead);
            FSM.AddTransition(leaveCombat, combat, idle);
        }

        // From dead
        if (dead)
        {
            FSM.AddState(dead);
            if (hidden) FSM.AddTransition(isDead, dead, hidden, false);
            FSM.AddTransition(isDead, dead, idle, false);
        }
    }
}