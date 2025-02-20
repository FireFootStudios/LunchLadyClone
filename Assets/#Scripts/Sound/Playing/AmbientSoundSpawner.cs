using System.Collections.Generic;
using UnityEngine;

public sealed class AmbientSoundSpawner : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private SpawnMode _spawnMode = 0;
    [SerializeField] private Vector2 _intervalBounds = new Vector2(5.0f, 20.0f);
    [SerializeField, Tooltip("Whether to play the first sound without interval delay")] private bool _playImmediate = false;
    [SerializeField,Tooltip("Max distance from player at which the sound gets spawned, if the player is too far from target position nothing is spawned")] private float _maxDistFromPlayer = 50.0f;
    [SerializeField] private SoundSpawnData _soundData = null;

    [SerializeField] private bool _visualize = false;
    [SerializeField] private GameObject _targetVisualizer = null;
    //[Header("Self")]

    [Header("SpawnTs")]
    [SerializeField] private List<Transform> _spawnTs = new List<Transform>();

    [Header("AroundPlayer")]
    [SerializeField] private Vector2 _radiusBounds = new Vector2(5.0f, 50.0f);
    [SerializeField] private bool _overrideYOffset = true;
    [SerializeField] private float _overrideYOffsetAmount = 0.0f;

    [Header("InsideCollider")]
    [SerializeField] private Collider _collider = null;

    [Header("RayCast")]
    [SerializeField] private Transform _rayOrigin = null;
    [SerializeField] private Vector3 _rayOriginOffset = Vector3.zero;
    [SerializeField] private Transform _rayDirection = null;
    [SerializeField] private Vector2 _rayDistanceBounds = new Vector2(1.0f, 50.0f);
    [SerializeField] private LayerMask _rayLayerMask = 0;
    [SerializeField] private Vector2 _rayXRangeBounds = new Vector2(30.0f, 90.0f);
    [SerializeField] private Vector2 _rayYRangeBounds = new Vector2(30.0f, 90.0f);


    private float _currentInterval = 0.0f;
    private const string _playSoundStr = "PlaySound";
    private SoundManager _soundManager = null;
    private PlayerN _player = null;

    private void Awake()
    {
        _soundManager = SoundManager.Instance;
        _player = GameManager.Instance.SceneData.LocalPlayer;
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Start()
    {
        if (_soundData.clips.Count == 0) return;

        //Play immediate?
        if (_playImmediate) PlaySound();
        else InvokeSound();
    }

    private void PlaySound()
    {
        bool dontPlay = false;

        switch (_spawnMode)
        {
            case SpawnMode.self:
                //Use this position
                _soundData.StartPos = this.transform.position;
                break;

            case SpawnMode.spawnTs:
                //Random spawn transform
                _soundData.StartPos = _spawnTs.RandomElement().transform.position;
                break;

            case SpawnMode.aroundPlayer:

                float randRadius = Utils.GetRandomFromBounds(_radiusBounds);

                Vector3 offset = UnityEngine.Random.insideUnitCircle * randRadius;
                if (_overrideYOffset) offset.y = _overrideYOffsetAmount; 

                _soundData.StartPos = _player.transform.position + offset;

                break;

            case SpawnMode.insideCollider:

                _soundData.StartPos = Utils.RandomPointInCollider(_collider);

                break;

            case SpawnMode.raycast:

                //Get random angle
                Vector3 rayDir = _rayDirection.forward;

                float randomXAngle = Utils.GetRandomFromBounds(_rayXRangeBounds, true);
                float randomYAngle = Utils.GetRandomFromBounds(_rayYRangeBounds, true);

                Quaternion rotX = Quaternion.AngleAxis(randomXAngle, _rayDirection.right);
                Quaternion rotY = Quaternion.AngleAxis(randomYAngle, _rayDirection.up);

                rayDir = rotY * rotX * rayDir;

                if (Physics.Raycast(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir, out RaycastHit hitInfo, _rayDistanceBounds.y, _rayLayerMask, QueryTriggerInteraction.Ignore))
                {
                    _soundData.StartPos = hitInfo.point;
                }
                else dontPlay = true;

                break;
        }

        //Invoke the next sound (make sure not to return from function before this is called)
        InvokeSound();

        if (dontPlay)
            return;

        //do distance check, return if too far
        if (!_soundData.is2D && _spawnMode != SpawnMode.aroundPlayer)
        {
            float distanceSqr = (_player.transform.position - _soundData.StartPos).sqrMagnitude;
            if (distanceSqr > _maxDistFromPlayer * _maxDistFromPlayer) return;
        }

        //Play sound
        _soundManager.PlaySound(_soundData);
    }

    private void InvokeSound()
    {
        _currentInterval = Utils.GetRandomFromBounds(_intervalBounds);
        Invoke(_playSoundStr, _currentInterval);
    }

    #region Debug
    private void OnDrawGizmosSelected()
    {
        switch (_spawnMode)
        {
            case SpawnMode.aroundPlayer:
                VisualizeAroundPlayer();
                break;

            case SpawnMode.raycast:
                VisualizedRaycast();
                break;
        }
    }

    private void VisualizedRaycast()
    {
        if (!_rayOrigin || !_rayDirection) return;

        Vector3 rayDir;

        //X
        rayDir = Quaternion.AngleAxis(_rayXRangeBounds.x, _rayDirection.right) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.red);

        rayDir = Quaternion.AngleAxis(_rayXRangeBounds.y, _rayDirection.right) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.red);

        rayDir = Quaternion.AngleAxis(-_rayXRangeBounds.x, _rayDirection.right) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.red);

        rayDir = Quaternion.AngleAxis(-_rayXRangeBounds.y, _rayDirection.right) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.red);

        //Y
        rayDir = Quaternion.AngleAxis(_rayYRangeBounds.x, _rayDirection.up) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.green);

        rayDir = Quaternion.AngleAxis(_rayYRangeBounds.y, _rayDirection.up) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.green);

        rayDir = Quaternion.AngleAxis(-_rayYRangeBounds.x, _rayDirection.up) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.green);

        rayDir = Quaternion.AngleAxis(-_rayYRangeBounds.y, _rayDirection.up) * _rayDirection.forward;
        Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, Color.green);

        if (!_visualize) return;

        for (int i = 0; i < 10; i++)
        {
            //Get random angle
            rayDir = _rayDirection.forward;

            float randomXAngle = Utils.GetRandomFromBounds(_rayXRangeBounds, true);
            float randomYAngle = Utils.GetRandomFromBounds(_rayYRangeBounds, true);

            Quaternion rotX = Quaternion.AngleAxis(randomXAngle, _rayDirection.right);
            Quaternion rotY = Quaternion.AngleAxis(randomYAngle, _rayDirection.up);

            rayDir = rotY * rotX * rayDir;
            Color yellow = Color.yellow;
            yellow.a = 0.3f;
            Debug.DrawRay(_rayOrigin.position + _rayOriginOffset + _rayDistanceBounds.x * rayDir, rayDir * _rayDistanceBounds.y, yellow, 0.5f);
        }
    }

    private void VisualizeAroundPlayer()
    {
        if (!_targetVisualizer) return;

        Vector3 pos = _targetVisualizer.transform.position;
        if (_overrideYOffset) pos.y += _overrideYOffsetAmount;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, _radiusBounds.x);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, _radiusBounds.y);

    }

    #endregion

    ////////////////////////////
    public enum SpawnMode { self, spawnTs, aroundPlayer, insideCollider, raycast }
}