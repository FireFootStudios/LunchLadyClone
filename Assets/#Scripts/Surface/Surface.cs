using UnityEngine;

//public enum SurfaceType { stone, sand, metal, wood, grass, snow, waterOrMud, gravel, flesh }
public sealed class Surface : MonoBehaviour
{
    [SerializeField] private SurfaceAsset _surfaceAsset = null;

    [Space]
    [SerializeField] private float _kickImpactSFXPitch = 1.0f;
    [SerializeField] private float _kickImpactSFXVolMult = 1.0f;

    [Space]
    [SerializeField] private ParticleSystem _onCollEnterPsTemplate = null;
    [SerializeField] private float _onCollEnterPsScale = 1.0f;

    public SurfaceAsset Asset { get { return _surfaceAsset; } }

    public float KickImpactSFXPitch { get { return _kickImpactSFXPitch; } }
    public float KickImpactVolumeMult { get { return _kickImpactSFXVolMult; } }

    private void OnCollisionEnter(Collision collision)
    {
        if (_onCollEnterPsTemplate)
        {
            VFXObject vfxObject = VFXManager.Instance.PlayVFXSimple(_onCollEnterPsTemplate, collision.gameObject.transform.position, 0.0f, _onCollEnterPsScale);
            if (vfxObject != null)
            {
                vfxObject.PS.transform.forward = -collision.GetContact(0).normal;
            }
        }
    }
}