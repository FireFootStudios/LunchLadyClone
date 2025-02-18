using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class ProjectileImpactVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject _visuals = null;
    [SerializeField] private DecalProjector _decalProjector = null;

    [Space]
    [SerializeField] private GameObject _tweenTarget = null;
    [SerializeField] private float _passiveScaleMult = 0.95f;
    [SerializeField] private float _passiveScaleSpeed = 1.0f;

    private bool _initialized = false;
    private Tween _scalePingPong = null;

    public Projectile Projectile { get; private set; }


    public void Init(Projectile projectile)
    {
        if (Projectile)
        {
            Projectile.Health.OnDeath -= OnProjectileDeath;
        }

        //Cache projectile
        Projectile = projectile;

        if (Projectile)
        {
            Projectile.Health.OnDeath += OnProjectileDeath;
        }

        _initialized = Projectile != null;

        //Enable/disable visuals
        if (_visuals) _visuals.gameObject.SetActive(_initialized);

        //Start tween
        if (_tweenTarget) _scalePingPong = _tweenTarget.transform.DOScale(Vector3.one * _passiveScaleMult, _passiveScaleSpeed).SetLoops(-1, LoopType.Yoyo);


        //TODO: fade in/out opacity of decal projector
    }

    private void OnProjectileDeath()
    {
        if (!Projectile) return;

        if (_visuals) _visuals.gameObject.SetActive(false);


        if (_scalePingPong != null) _scalePingPong.Kill();

        //For now clean up + destroy 
        //Uninitiliaze so that we are no longer subbed to event of projectile first
        Init(null);
        Destroy(this.gameObject);
    }
}