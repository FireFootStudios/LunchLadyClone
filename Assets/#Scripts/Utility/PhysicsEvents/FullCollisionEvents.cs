using System;
using UnityEngine;

public sealed class FullCollisionEvents : CollEvents
{
    public Action<Collision> OnCollStay;

    private void OnCollisionStay(Collision collision)
    {
        if (_disableOnStay) return;
        if (!ValidTag(collision.gameObject.tag) || !ValidCollision(collision)) return;

        OnStay?.Invoke(collision.collider);
        OnCollStay?.Invoke(collision);
    }
}
