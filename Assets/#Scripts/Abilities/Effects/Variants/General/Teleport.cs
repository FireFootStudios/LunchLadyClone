using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public sealed class Teleport : Effect
{
    [SerializeField] private Transform _teleportTo = null;
    [SerializeField] private bool _resetVelocity = true;
    [Space]
    [SerializeField] private PlayableAsset _transition = null;
    [SerializeField] private float _afterTransDelay = .2f;


    protected override void Copy(Effect template) { }

    protected override void OnApply(GameObject target, Transform originT, EffectModifiers effectMods)
    {
        if (!_teleportTo || !target) return;

        StopAllCoroutines();
        StartCoroutine(TeleportCo(target));
    }

    private IEnumerator TeleportCo(GameObject target)
    {
        //Disable target input


        //TransitionManager.Instance.DoTransition(_transition, out PlayableDirector director);
        //if (director) yield return new WaitForSeconds(_afterTransDelay);

        if (target.TryGetComponent(out Rigidbody rb))
        {
            if (_resetVelocity) rb.linearVelocity = Vector3.zero;
            rb.MovePosition(_teleportTo.position);
            rb.MoveRotation(_teleportTo.rotation);
            Physics.SyncTransforms();
        }
        else
        {
            target.transform.SetPositionAndRotation(_teleportTo.position, _teleportTo.rotation);
        }
        yield return this;
    }
}