using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public abstract class Effect : MonoBehaviour
{
    [SerializeField] protected EffectTargetMode _targetMode = 0;

    public enum EffectTargetMode { initialFirst, initialOnly, referrerFirst, all }

    public Ability Ability { get; private set; }

    private List<GameObject> _targetList = new List<GameObject>();

    protected virtual void Awake()
    {
        Ability = GetComponent<Ability>();
    }

    public Effect Copy(GameObject target)
    {
        if (!target) return null;

        //Effect effect = (Effect)target.AddComponent(this.GetType());
        Effect effect = this.CopyComponent(target);

        effect._targetMode = _targetMode;
        effect.Ability = Ability;

        Copy(this);

        return effect;
    }

    protected abstract void Copy(Effect template);

    public void Apply(GameObject initialTarget, Transform originT)
    {
        EvaluateTarget(initialTarget, out EffectModifiers effectMods);

        //Elements could be null as certain effects do not require a target!
        foreach (GameObject target in _targetList)
            OnApply(target, originT, effectMods);
    }

    public float GetEffectiveness(GameObject initialTarget)
    {
        float combined = 0.0f;

        EvaluateTarget(initialTarget, out EffectModifiers _);

        //TODO: Include effect mods in effect effectiveness calculations

        //Elements could be null as certain effects do not require a target!
        foreach (GameObject target in _targetList)
            combined += Effectiveness(target);

        return _targetList.Count > 0 ? combined / _targetList.Count : 0.0f;
    }

    private void EvaluateTarget(GameObject initialTarget, out EffectModifiers effectMods)
    {
        _targetList.Clear();
        EffectReferrer referrer = initialTarget ? initialTarget.GetComponent<EffectReferrer>() : null;

        //Search for effect mods(this will always prefer initial target (for now) but look on referred if none found)
        if (initialTarget)
        {
            effectMods = initialTarget.GetComponent<EffectModifiers>();
            if (!effectMods && referrer && referrer.ReferToGo) effectMods = referrer.ReferToGo.GetComponent<EffectModifiers>();
        }
        else effectMods = null;

        //Fill list based on mode
        switch (_targetMode)
        {
            case EffectTargetMode.initialOnly:

                //Only add initial
                _targetList.Add(initialTarget);

                break;

            case EffectTargetMode.initialFirst:

                //Add either one (but atleast one, even if null)
                if (Effectiveness(initialTarget) > 0.0f) _targetList.Add(initialTarget);
                else if (referrer) _targetList.Add(referrer.ReferToGo);
                else _targetList.Add(null);

                break;

            case EffectTargetMode.referrerFirst:

                //Add either one (but atleast one, even if null)
                if (referrer && Effectiveness(referrer.ReferToGo) > 0.0f) _targetList.Add(referrer.ReferToGo);
                else _targetList.Add(initialTarget);

                break;

            case EffectTargetMode.all:

                if (referrer && Effectiveness(referrer.ReferToGo) > 0.0f) _targetList.Add(referrer.ReferToGo);

                //Always add initial
                _targetList.Add(initialTarget);

                break;
        }
    }

    protected abstract void OnApply(GameObject target, Transform originT, EffectModifiers effectMods);

    protected virtual float Effectiveness(GameObject target) { return 1.0f; }

    public virtual bool CanApply() { return true; }

    public virtual void OnCancel() { }

    public virtual void OnReset() { } //Called before firing the ability we are part off

    public virtual void OnCleanUp() { } //Called when the ability is fully reset


    public virtual bool IsFinished() { return true; }
}