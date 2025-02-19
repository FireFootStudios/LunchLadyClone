using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class FSMState : MonoBehaviour
{
    [SerializeField] private float _minStateTime = 0.25f;
    //Ignore min state time?

    public float MinStateTime { get { return _minStateTime; } }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

public abstract class FSMTransition
{
    public abstract bool Evaluate(GameObject go);
}

public sealed class FSM : MonoBehaviour
{
    [SerializeField] private bool _debugStateChanges = false;

    private List<FSMState> _states = new List<FSMState>();
    private List<Tuple<FSMState, FSMState, List<FSMTransition>, bool>> _transitions = new List<Tuple<FSMState, FSMState, List<FSMTransition>, bool>>(); //from, to, transition, required state
    private FSMState _currentState = null;
    private float _currentStateElapsed = 0.0f;

    public FSMState CurrentState { get { return _currentState; } }

    public void Resett()
    {
        //if any states, start with first added
        if (_states.Count > 0) SetState(_states[0]);
    }

    public void AddTransition(FSMTransition t, FSMState from, FSMState to, bool requiredState = true, bool isBreakTransition = false)
    {
        if (t == null || from == null || (to == null && !isBreakTransition)) return;

        List<FSMTransition> l = new List<FSMTransition> { t };

        _transitions.Add(new Tuple<FSMState, FSMState, List<FSMTransition>, bool>(from, to, l, requiredState));
    }

    //this will make a transition if all of transitions in the list are true/false
    public void AddTransitionList(List<FSMTransition> tList, FSMState from, FSMState to, bool requiredState = true)
    {
        if (tList == null || from == null || to == null || tList.Count == 0) return;

        _transitions.Add(new Tuple<FSMState, FSMState, List<FSMTransition>, bool>(from, to, tList, requiredState));
    }

    public void AddState(FSMState state)
    {
        if (state == null) return;
        if (_states.Contains(state)) return;

        //disable state
        state.enabled = false;
        _states.Add(state);
    }

    private void Start()
    {
        Resett();
    }

    private void Update()
    {
        //check if current state is not null
        if (_currentState == null) return;

        _currentStateElapsed += Time.deltaTime;
        if (_currentStateElapsed < _currentState.MinStateTime) return;

        //go over all transitions
        foreach (Tuple<FSMState, FSMState, List<FSMTransition>, bool> tuple in _transitions)
        {
            //is transition for our current state?
            if (tuple.Item1 != _currentState) continue;

            //is transition true/false (if more than 1 transition, all of them are required to be true/false)
            for (int i = 0; i < tuple.Item3.Count; i++)
            {
                //evaluate transition, leave loop as soon as a transition is wrong, else, if not last transition, continue
                if (tuple.Item3[i].Evaluate(this.gameObject) != tuple.Item4) break;
                else if (i < tuple.Item3.Count - 1) continue;

                //first, check if to state is not null, if so, stay in same state and do nothing
                if (tuple.Item2 == null) 
                    return;

                SetState(tuple.Item2);
                return;
            }
        }
    }

    private void SetState<T>(T state) where T : FSMState
    {
        //exit and disable previous state
        if (_currentState)
        {
            _currentState.enabled = false;
            _currentState.OnExit();
        }

        //set, enter and enable new state
        _currentState = state;
        _currentState.enabled = true;
        _currentState.OnEnter();

        //reset state elapsed
        _currentStateElapsed = 0.0f;

        if (_debugStateChanges) Debug.Log(gameObject.name + " entered state: " + _currentState.ToString());
    }
}
