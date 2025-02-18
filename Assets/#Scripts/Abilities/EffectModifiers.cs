using System.Collections.Generic;
using UnityEngine;

public enum EffectModifierType { knockback, stun, slow }
public sealed class EffectModifiers : MonoBehaviour
{
    [SerializeField] private List<EffectModifier> _modifiers = new List<EffectModifier>();

    [Space]
    [SerializeField] private Health _linkedHealth = null;

    public bool HasModifier(EffectModifierType type)
    {
        return _modifiers.Exists(m =>
        {
            //if(!_applyWhenDead _linkedHealth && _linkedHealth.i)

           return m.type == type;
        });
    }

    public float GetModifier(EffectModifierType type)
    {
        float finalMod = 1.0f;

        foreach (EffectModifier mod in _modifiers)
        {
            if (mod.type != type) continue;
            if (mod.applyOnlyWhileAlive && _linkedHealth && _linkedHealth.IsDead) continue;
            if (mod.applyOnlyWhileDeath && _linkedHealth && !_linkedHealth.IsDead) continue;

            finalMod *= mod.value;
        }

        return finalMod;
    }
}

[System.Serializable]
public struct EffectModifier
{
    public EffectModifierType type;
    public float value;
    public bool applyOnlyWhileAlive;
    public bool applyOnlyWhileDeath;
}