using UnityEngine;

public class TriggerEvents : PhysicsEvents
{
    private void OnTriggerEnter(Collider other)
    {
        if (_disableOnEnter) return;
        if (!ValidTag(other.tag)) return;

        OnEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_disableOnExit) return;
        if (!ValidTag(other.tag)) return;

        OnExit?.Invoke(other);
    }
}