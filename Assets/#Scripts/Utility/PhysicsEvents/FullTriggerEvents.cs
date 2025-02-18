using UnityEngine;

public sealed class FullTriggerEvents : TriggerEvents
{

    private void OnTriggerStay(Collider other)
    {
        if (_disableOnStay) return;
        if (!ValidTag(other.tag)) return;

        OnStay?.Invoke(other);
    }
}
