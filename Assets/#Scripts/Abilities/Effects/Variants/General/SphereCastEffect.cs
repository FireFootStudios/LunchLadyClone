using UnityEngine;

public sealed class SphereCastEffect : TargettingEffect
{
    [SerializeField] private float _radius = 5.0f;
    [SerializeField] private LayerMask _mask = default;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = default;

    [Space]
    [SerializeField] private GameObject _visualizeTarget = null;
    [SerializeField] private bool _debugSphereCasts = false;

    //allowed tags?

    //TODO: There is a lot we could do to improve this effect:
    // Mix with other types of casts (although it might be better to keep them seperated)
    // Allow deciding where to originate the sphere cast from
    // ..

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        //Clear list first
        Targets.Clear();

        //Perform sphere cast
        //RaycastHit[] hits = Physics.SphereCastAll(originT.transform.position, _radius, Vector3.zero, 0.0f, ~_mask, _triggerInteraction);
        Collider[] hits = Physics.OverlapSphere(originT.transform.position, _radius, _mask, _triggerInteraction);

        if (_debugSphereCasts)
        {
            Debug.DrawLine(originT.transform.position, originT.transform.position + Vector3.up * _radius, Color.red, 3.0f);
            Debug.DrawLine(originT.transform.position, originT.transform.position + Vector3.down * _radius, Color.red, 3.0f);
            Debug.DrawLine(originT.transform.position, originT.transform.position + Vector3.forward * _radius, Color.red, 3.0f);
            Debug.DrawLine(originT.transform.position, originT.transform.position + Vector3.back * _radius, Color.red, 3.0f);
        }

        //Check if any hits
        if (hits.Length > 0)
        {
            foreach (Collider hit in hits)
            {
                Targets.Add(hit.GetComponent<Collider>().gameObject);
            }
        }

        //invoke for initial targets in hitbox
        if (Targets.Count > 0)
            OnTargetsReady?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (!_visualizeTarget) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_visualizeTarget.transform.position, _radius);
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }
}