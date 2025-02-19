using System.Collections.Generic;
using UnityEngine;

public sealed class PatrolPath : MonoBehaviour
{
    [SerializeField] private List<PatrolPoint> _points = new List<PatrolPoint>();
    [SerializeField, Tooltip("Delay output upon reaching a point.")] private float _delay = 0.0f;
    [SerializeField] private bool _loop = true;
    [SerializeField, Tooltip("Don't use with beziers.")] private bool _reverseLoop = false;
    [SerializeField, Tooltip("Don't use with beziers.")] private bool _randomise = false;

    [Space, SerializeField, Tooltip("Click to automatically add points (children)")] private bool _updatePoints = false;

    private static float _triggerRadius = 0.05f;
    private static int _bezierDebugPrecision = 10;
    private const string _patrolLayerStr = "Patrol";
    
    public List<PatrolPoint> Points { get { return _points; } }
    public float Delay { get { return _delay; } }
    public bool Loop { get { return _loop; } }
    public bool ReverseLoop { get { return _reverseLoop; } }
    public bool Randomise { get { return _randomise; } }

    //this is to add transforms added in the editor as patrolpoints to this path
    public void UpdatePoints()
    {
        //clear prev
        _points.Clear();

        //get patrol layer
        int patrolLayer = LayerMask.NameToLayer(_patrolLayerStr);

        //add all children as patrol points and init them
        foreach (Transform child in transform)
        {
            PatrolPoint pp =  new PatrolPoint();
            pp.transform = child;
            pp.controlT = child.childCount > 0 ? child.GetChild(0) : null;
            pp.transform.gameObject.layer = patrolLayer;
            _points.Add(pp);
        }
    }

    private void Awake()
    {
        UpdatePoints();

        //create/update/init sphere triggers on spawnpoints (cant do it in update points cuz unity cries like a little **** -> adding components is not supported in OnValidate)
        InitSphereTriggers();
    }

    private void InitSphereTriggers()
    {
        foreach (PatrolPoint pp in _points)
        {
            //get or create sphere collider
            pp.sphereTrigger = pp.transform.GetComponent<SphereCollider>();
            if (!pp.sphereTrigger) pp.sphereTrigger = pp.transform.gameObject.AddComponent<SphereCollider>();

            //initialize sphere collider
            pp.sphereTrigger.isTrigger = true;
            pp.sphereTrigger.radius = _triggerRadius;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //debug path
        Vector3 yOffset = new Vector3(0.0f, 0.1f, 0.0f);
        for (int i = 0; i < _points.Count; i++)
        {
            PatrolPoint pp = _points[i];
            if (pp == null || !pp.transform) continue;

            //first draw pp
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(pp.transform.transform.position + yOffset, _triggerRadius);

            // Get prevPP (skip this point if first point unless loop || reverseloop)
            PatrolPoint prevPp = null;
            if (i > 0) prevPp = _points[i - 1];
            else if (i == 0 && (_loop || _reverseLoop)) prevPp = _points[_points.Count - 1];
            else continue;

            if (prevPp == null || !prevPp.transform) continue;

            //Finally, draw lines (normal/bezier)
            if (pp.controlT == null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(prevPp.transform.position + yOffset, pp.transform.position + yOffset);
            }
            else
            {
                Gizmos.color = Color.cyan;
                for (int j = 0; j < _bezierDebugPrecision; j++)
                {
                    Vector3 from = Utils.EvaluateQuadraticCurve(prevPp.transform.position, pp.transform.position, ((float)j / _bezierDebugPrecision), pp.controlT.position);
                    Vector3 to = Utils.EvaluateQuadraticCurve(prevPp.transform.position, pp.transform.position, ((float)(j + 1) / _bezierDebugPrecision), pp.controlT.position);

                    Gizmos.DrawLine(from + yOffset, to + yOffset);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (_updatePoints)
        {
            UpdatePoints();
            _updatePoints = false;
        }
    }
#endif
}

public sealed class PatrolPoint
{
    public Transform transform = null;
    public Transform controlT = null; //If set, the path to this point will become a bezier curve, controlled by this transform
    public SphereCollider sphereTrigger = null;
}