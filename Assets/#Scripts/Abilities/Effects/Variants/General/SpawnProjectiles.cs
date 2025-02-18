using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class SpawnProjectiles : Effect
{
    [Header("General"), SerializeField] private ExecuterData _executeData = null;
    [SerializeField] private Projectile _template = null;
    [SerializeField] private ProjectileData _projectileData = null;
    [SerializeField] private List<Transform> _spawnTs = new List<Transform>();
    [SerializeField, Tooltip("Set the target tags, leave empty to allow any targets")] private List<Tag> _targetTags = new List<Tag>();

    [Header("Extra"), SerializeField] private float _scale = 1.0f;
    [SerializeField, Tooltip("Pass target to projectile?")] private bool _passTarget = true;

    [Header("Impact Visuals")]
    [SerializeField] private ProjectileImpactVisualizer _impactVisualizerTemplate = null;
    [SerializeField] private LayerMask _impactVisualizerRayCastLayerMask = default;
    [SerializeField] private QueryTriggerInteraction _impactVisualizerRayCastTriggerInteraction = default;
    
    //[SerializeField, Tooltip("If no initial target is passed")] private Transform _impactVisualizerT = null;

    [Header("Pooling"), SerializeField] private int _maxPool = 0;
    [SerializeField,Tooltip("How much time after the projectiles death before adding it back to the pool")] private float _poolDelay = 15.0f;
    [SerializeField] private bool _disableWhilePooled = false;

    [Space, SerializeField] private bool _gizmoDebugging = true;
    [SerializeField] private float _debugDistance = 10.0f;

    private Executer _executer = null;

    private List<Coroutine> _poolProjectiles = new List<Coroutine>();

    private List<Projectile> _activeProjectiles = new List<Projectile>();
    private Queue<Projectile> _projQueue = new Queue<Projectile>();

    protected override void Awake()
    {
        base.Awake();

        //create executer and set data
        _executer = this.gameObject.AddComponent<Executer>();
        _executer.Data = _executeData;
    }

    protected override void OnApply(GameObject target, Transform executerT, EffectModifiers effectMods)
    {
        if (!_template || _spawnTs.Count == 0) return;

        foreach (Transform spawnT in _spawnTs)
        {
            Projectile proj;

            //Instantiate or use pooled projectile
            if (_projQueue.Count == 0)
            {
                proj = Instantiate(_template, spawnT.position, spawnT.rotation);
                proj.transform.localScale = Vector3.one * _scale;
                proj.tag = Ability.Source.tag;

                //This is to make sure the projectile gets pooled on death if enabled
                if (_maxPool > 0) proj.Health.OnDeath += () =>
                {
                    StartCoroutine(PoolProjectile(proj));
                    //_poolProjectiles.Add(StartCoroutine(PoolProjectile(proj)));
                };

                //Copy executer
                Executer executer = _executer.Copy(proj.gameObject);

                //Initialize projectile
                proj.Init(_projectileData, executer, _targetTags);
            }
            else
            {
                proj = _projQueue.Dequeue();
                proj.transform.SetPositionAndRotation(spawnT.position, spawnT.rotation);
                proj.RB.linearVelocity = Vector3.zero;
                proj.gameObject.SetActive(true);
            }
           
            //Add to active projectiles
            if (_maxPool > 0) _activeProjectiles.Add(proj);

            //Fire proj
            proj.ResetAndFire(_passTarget ? target : null);

            CreateVisualizer(proj);
         }
    }

    private void CreateVisualizer(Projectile proj)
    {
        if (!_impactVisualizerTemplate) return;

        bool valid = false;
        Vector3 targetPos = Vector3.zero;
        quaternion targetRot = Quaternion.identity;

        //Target
        if (proj.InitialTarget)
        {
            targetPos = proj.InitialTargetPos + _impactVisualizerTemplate.transform.localPosition;
            targetRot = _impactVisualizerTemplate.transform.rotation;
            valid = true;
        }
        //No target
        else if (Physics.Raycast(proj.transform.position, proj.transform.forward, out RaycastHit hitInfo, float.MaxValue, _impactVisualizerRayCastLayerMask, _impactVisualizerRayCastTriggerInteraction))
        {
            targetPos = hitInfo.point + _impactVisualizerTemplate.transform.localPosition;
            targetRot = _impactVisualizerTemplate.transform.rotation;
            valid = true;
        }
        //Debug.DrawRay(proj.transform.position, proj.transform.forward * 1000.0f, Color.red, 3.0f);

        if (valid)
        {
            //Create or Re-use a impact visualizer and use hit info to set it the hit normal
            ProjectileImpactVisualizer visualizer = Instantiate(_impactVisualizerTemplate, targetPos, targetRot);
            visualizer.Init(proj);
        }
    }

    private IEnumerator PoolProjectile(Projectile proj)
    {
        if (_poolDelay > 0.0f)
            yield return new WaitForSeconds(_poolDelay);

         CleanUpProjectile(proj);

        yield return null;
    }

    protected override float Effectiveness(GameObject target)
    {
        if (_template == null || _spawnTs.Count == 0) return 0.0f;

        return _executer.CalculateEffectiveness(target);
    }

    public override void OnCleanUp()
    {
        for (int i = 0; i < _activeProjectiles.Count; i++)
        {
            bool removed = CleanUpProjectile(_activeProjectiles[i]);
            if (removed) i--;
        }

        StopAllCoroutines();
    }

    private bool CleanUpProjectile(Projectile proj)
    {
        //Remove from active list
        bool wasActive = _activeProjectiles.Remove(proj);
        if (!wasActive) return false;

        //Kill in case still active
        proj.Kill();

        //If under max pool count, enqueue
        if (_projQueue.Count < _maxPool)
        {
            _projQueue.Enqueue(proj);

            //Disable if desired
            if (_disableWhilePooled) proj.gameObject.SetActive(false);
        }
        //if queue is at max size, destroy instead
        else Destroy(proj.gameObject);

        return true;
    }

    protected override void Copy(Effect effect)
    {
        //TODO
    }

    #region Debugging
    public void OnDrawGizmos()
    {
        if (!_gizmoDebugging) return;
        if (_template == null || _spawnTs.Count == 0) return;

        foreach (Transform spawnT in _spawnTs)
        {
            if (!spawnT) continue;

            Vector3 targetPos = spawnT.transform.position + spawnT.transform.forward * _debugDistance;

            //Debug path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnT.transform.position, targetPos);
        }
    }
    #endregion
}