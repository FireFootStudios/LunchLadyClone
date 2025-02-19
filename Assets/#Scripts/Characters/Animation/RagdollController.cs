using System.Collections.Generic;
using UnityEngine;


public sealed class RagdollController : MonoBehaviour
{
    [SerializeField] private GameObject _rigGO = null;
    [SerializeField] private Rigidbody _mainRB = null;
    [SerializeField] private Rigidbody _mainRagdollRB = null;
    [SerializeField] private bool _stateOnAwake = false;

    [Space]
    [SerializeField] private LayerMask _excludeLayersDisabled = 0;
    [SerializeField] private LayerMask _excludeLayersEnabled = 0;
    [Space]
    [SerializeField] private string _tagDisabled = "Untagged";
    [SerializeField] private string _tagEnabled = "Untagged";


    private Health _health = null;
    private CharAnimation _charAnimation = null;
    private FreeMovement _movement = null;

    private Vector3 _defaultRigGoPos = Vector3.zero;
    private Quaternion _defaultRigGoRot = Quaternion.identity;

    private List<Rigidbody> _ragdollRBs = new List<Rigidbody>();
    private List<Collider> _colliders = new List<Collider>();
    private List<Joint> _ragdollJoints = new List<Joint>();

    private List<JointInfo> _jointInfos = new List<JointInfo>();

    public Rigidbody MainRagdollRB { get { return _mainRagdollRB; } }
    public bool IsRagdollActive { get; private set; }

    private void Awake()
    {
        _health = GetComponent<Health>();
        _charAnimation = GetComponent<CharAnimation>();
        _movement = GetComponent<FreeMovement>();

        _defaultRigGoPos = _rigGO.transform.localPosition;
        _defaultRigGoRot = _rigGO.transform.localRotation;
        
        if (_health)
        {
            _health.OnDeath += () => SetRagdoll(true);
            _health.OnRevive += () => SetRagdoll(false);
        }

        //cache all ragdoll rigidbodies, colliders and joints
        _rigGO.GetComponentsInChildren(true, _ragdollRBs);
        _rigGO.GetComponentsInChildren(true, _ragdollJoints);
        _rigGO.GetComponentsInChildren(true, _colliders);

        //Cach joint init info + destroy (Joints cannot be disabled... THANK YOU UNITY :D)
        foreach (Joint joint in _ragdollJoints)
        {
            _jointInfos.Add(new JointInfo(joint));

            //RB first
            //Destroy(joint.connectedBody);
            Destroy(joint);
        }

        //Ignore collision between all colliders on and childed to this gameobject
        foreach (Collider coll in _colliders)
            foreach (Collider collOther in _colliders)
                Physics.IgnoreCollision(coll, collOther);
    }

    private void OnEnable()
    {
        SetRagdoll(_stateOnAwake);
    }

    private void OnDisable()
    {
        SetRagdoll(false);
    }

    private void SetRagdoll(bool active)
    {
        if (!_rigGO) return;
        //if (IsRagdollActive == active) return;

        IsRagdollActive = active;

        //Set all rbs kinematic
        foreach (Rigidbody rb in _ragdollRBs)
            rb.isKinematic = !active;

        if (active)
        {
            //Re-create all joints
            foreach (JointInfo jointInfo in _jointInfos)
                _ragdollJoints.Add(jointInfo.CreateJoint());
        }
        else
        {
            //Destroy any remaining joints (these will be recreated if enabled)!
            foreach (Joint joint in _ragdollJoints)
            {
                Destroy(joint);
            }

            _ragdollJoints.Clear();

            //Reset the locol pos and rot of the rigGO
            _rigGO.transform.localPosition = _defaultRigGoPos;
            _rigGO.transform.localRotation = _defaultRigGoRot;
        }
        
        //Set collider exclude layers + tags
        foreach (Collider coll in _colliders)
        {
            //skip this if collider is trigger(hitbox) or, if it is the main collider, on main object
            if (coll.isTrigger || coll.gameObject == _mainRB.gameObject) continue;

            coll.gameObject.tag = active ? _tagEnabled : _tagDisabled;
            coll.excludeLayers = active ? _excludeLayersEnabled : _excludeLayersDisabled;
        }

        //Remove main rb constraints
        _mainRB.constraints = active ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeRotation;
        //_mainRB.useGravity = active;
        _mainRB.isKinematic = active;

        //Ignore decel
        _movement.IgnoreDecelerate = active;

        //This will/should disable the animator comp linked to the animation comp
        if (_charAnimation) _charAnimation.enabled = !active;
    }
}

public enum JointType { fixedd, character}
public sealed class JointInfo
{
    private GameObject _source = null;
    private Rigidbody _connectedBody = null;
    private bool _enableCollision = false;
    private JointType _type = default;

    //



    public JointInfo(Joint joint)
    {
        //save info of character joint
        _source = joint.gameObject;
        _connectedBody = joint.connectedBody;
        _enableCollision = joint.enableCollision;

        _type = joint is CharacterJoint ? JointType.character : JointType.fixedd;
    }

    public Joint CreateJoint()
    {
        if (!_source) return null;

        Joint joint;
        if (_type == JointType.character) joint = _source.AddComponent<CharacterJoint>();
        else joint = _source.AddComponent<FixedJoint>();

        //set saved info -> All different from default fields of the joing comp should be added here!
        joint.connectedBody = _connectedBody;
        joint.enableCollision = _enableCollision;

        return joint;
    }
}