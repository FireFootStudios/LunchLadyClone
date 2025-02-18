using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Executer : MonoBehaviour
{
    [SerializeField] private ExecuterData _executeData = null;

    //can be overriden/set by used script
    public ExecuterData Data { get { return _executeData; } set { _executeData = value; } }

    public bool IsExecuting { get; private set; }
    public int MinTargets { get { return Data.minTargets; } }
    public int MaxTargets { get { return Data.maxTargets; } }

    private Transform OriginT { get { return Data.originT ? Data.originT : this.transform; } }

    //TODO, right now all effects are copied over for each time they are referenced for each execute, this needs to be fixed in future versions
    public Executer Copy(GameObject targetGo)
    {
        if (!targetGo) return null;

        //create copy and set data
        Executer copy = targetGo.AddComponent<Executer>();
        copy.IsExecuting = false;
        copy.Data = new ExecuterData();
        copy.Data.maxTargets = Data.maxTargets;
        copy.Data.minTargets = Data.minTargets;

        //copy executes, make new list and add individual copies of each execute
        foreach (Execute execute in Data.executes)
        {
            Execute executeCopy = execute.Copy(targetGo);
            copy.Data.executes.Add(executeCopy);
        }

        return copy;
    }

    public bool CanExecute()
    {
        if (IsExecuting) return false;

        foreach (Execute execute in Data.executes)
            if (!execute.CanExecute()) return false;

        return true;
    }

    public void Execute(List<GameObject> targets)
    {
        if (!CanExecute()) return;

        //Stop any remaining coroutines
        StopAllCoroutines();

        //Reset executes first
        foreach (Execute execute in Data.executes)
            execute.Reset();

        //Start new coroutine
        StartCoroutine(ExecuteCo(targets));
    }

    public void Execute(GameObject target)
    {
        if (!CanExecute()) return;

        //Stop any remaining coroutines
        StopAllCoroutines();

        //Reset executes first
        foreach (Execute execute in Data.executes)
            execute.Reset();

        //Start new coroutine
        StartCoroutine(ExecuteCo(target));
    }

    public void Cancel()
    {
        if (!IsExecuting) return;

        IsExecuting = false;

        StopAllCoroutines();

        foreach (Execute execute in Data.executes)
            execute.OnCancel();
    }

    public void CleanUp()
    {
        foreach (Execute execute in Data.executes)
            execute.CleanUp();
    }

    private void OnEnable()
    {
        IsExecuting = false;
    }

    private void Start()
    {
        //look for a potential TargetSystem on this or any child object and link to executes
        //important that this happens in start as executer data can be and probably will be set from controller script
        TargetSystem ts = GetComponentInChildren<TargetSystem>();
        foreach (Execute execute in _executeData.executes)
            execute.TargetSystem = ts;
    }

    private IEnumerator ExecuteCo(List<GameObject> targets)
    {
        IsExecuting = true;
        float elapsed = 0.0f;

        while (IsExecuting && Data.executes.Count > 0)
        {
            IsExecuting = false;

            foreach (Execute execute in Data.executes)
            {
                if (execute.IsFinished()) continue;
                if (elapsed >= execute.Time)
                {
                    //check if execute requires a target
                    if (execute.IgnoreTargets || targets == null || targets.Count == 0) execute.DoExecute(null, OriginT);
                    else execute.DoExecuteList(targets, OriginT, MaxTargets);

                    //if this execute is not finished yet, also set is executing to true
                    if (!execute.IsFinished()) IsExecuting = true;
                }
                else IsExecuting = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        IsExecuting = false;
        yield return null;
    }

    private IEnumerator ExecuteCo(GameObject target)
    {
        IsExecuting = true;
        float elapsed = 0.0f;

        while (IsExecuting && Data.executes.Count > 0)
        {
            elapsed += Time.deltaTime;
            IsExecuting = false;

            foreach (Execute execute in Data.executes)
            {
                if (execute.IsFinished()) continue;
                if (elapsed > execute.Time)
                {
                    //check if execute requires a target
                    if (execute.IgnoreTargets || Data.maxTargets == 0) execute.DoExecute(null, OriginT);
                    else execute.DoExecute(target, OriginT);

                    //if this execute is not finished yet, also set is executing to true
                    if (!execute.IsFinished()) IsExecuting = true;
                }
                else IsExecuting = true;
            }

            yield return null;
        }

        IsExecuting = false;
        yield return null;
    }

    public float CalculateEffectiveness(GameObject target)
    {
        if (Data.executes.Count == 0) return 0.0f;

        float effectiveness = 0.0f;
        foreach (Execute execute in Data.executes)
        {
            effectiveness += execute.GetEffectiveness(target);
        }

        return effectiveness > 0.0f ? effectiveness / Data.executes.Count : 0.0f;
    }
}

[System.Serializable]
public sealed class ExecuterData
{
    [SerializeField] public List<Execute> executes = new List<Execute>();
    [SerializeField, Tooltip("overrides the origin T passed when executing")] public Transform originT = null;
    [Tooltip("Min amount of targets to execute for"), SerializeField] public int minTargets = 1;
    [Tooltip("Max amount of targets to execute on"), SerializeField] public int maxTargets = 1;
}