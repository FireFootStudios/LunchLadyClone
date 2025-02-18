using System;
using UnityEngine;

public class CollEvents : PhysicsEvents
{
    [SerializeField, Tooltip("the minimum required angle between up and collision contact normal for counting as valid event")] private float _minContactAngle = 60.0f;

    public Action<Collision> OnCollEnter;
    public Action<Collision> OnCollExit;

    //protected override void OnDisable()
    //{
    //    base.OnDisable();

    //    OnCollExit?.Invoke(null);
    //}

    private void OnCollisionEnter(Collision collision)
    {
        if (_disableOnEnter)  return;
        if (!ValidTag(collision.gameObject.tag) || !ValidCollision(collision)) return;

        OnCollEnter?.Invoke(collision);
        OnEnter?.Invoke(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (_disableOnExit) return;
        if (!ValidTag(collision.gameObject.tag)) return;

        OnCollExit?.Invoke(collision);
        OnExit?.Invoke(collision.collider);
    }

    protected bool ValidCollision(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Angle(-contact.normal, Vector3.up) > _minContactAngle) continue;

            return true;
        }

        return false;
    }
}