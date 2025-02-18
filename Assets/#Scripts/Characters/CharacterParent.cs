using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterParent : MonoBehaviour
{
    #region Fields
    [SerializeField] private FullTriggerEvents _triggerEvents = null;
    [SerializeField] private FullCollisionEvents _collEvents = null;
    [SerializeField, Tooltip("Optional (other than this) transform to parent children to")] private Transform _childrenT = null;


    [Header("Sync velocities")]
    [SerializeField, Tooltip("If set, velocity of this rb will be added to the characters RB on unparenting and removed on parenting")] private Rigidbody _mainRB = null;
    //[SerializeField, Tooltip("Same function as RB but for kinematic movement (since they do not use the RB to move)")] private KinematicMovement _kinematicMovement = null;


    private List<ChildInfo> _children = new List<ChildInfo>();
    private PlayerN _player = null;
    #endregion

    public Transform ParentT { get { return _childrenT ? _childrenT : this.transform; } }

    public void UnparentAll()
    {
        ClearTouches();
        EvaluateChildren();
    }

    private void Awake()
    {
        if (_triggerEvents) _triggerEvents.OnStay += OnStayTrigger;
        if (_collEvents) _collEvents.OnStay += OnStayTrigger;
    }

    private void OnEnable()
    {
        _player = GameManager.Instance.SceneData.Player;
    }

    private void OnDisable()
    {
        UnparentAll();
    }

    private void FixedUpdate()
    {
        //clear touches every fixed update (should be before collision events)
        ClearTouches();
    }

    private void Update()
    {
        EvaluateChildren();
    }

    public void ClearTouches()
    {
        foreach (ChildInfo childInfo in _children)
            childInfo.touches.Clear();
    }

    private void EvaluateChildren()
    {
        if (!this.isActiveAndEnabled) return;

        //evaluate touches and (un)parent children
        foreach (ChildInfo childInfo in _children)
        {
            //ignore null
            if (childInfo.collider == null) continue;

            //are there any touches since latest collision events?
            if (childInfo.touches.Count == 0)
            {
                //is still parented?
                if (childInfo.collider.transform.parent != ParentT) continue;

                //unparent (player requires to be called by separate function for rotation purposes)
                if (childInfo.collider.gameObject == _player.gameObject) _player.Parent(null);
                else childInfo.collider.transform.parent = null;

                //Add (if set) parent velocity to children on unparenting
                //if (childInfo.rigidbody && (_mainRB || _kinematicMovement))
                //{
                //    Vector3 vel = _kinematicMovement ? _kinematicMovement.CurrentVelocity : _mainRB.velocity;
                //    childInfo.rigidbody.AddForce(vel, ForceMode.VelocityChange);
                //}
            }
            else
            {
                //is already parented?
                if (childInfo.collider.transform.parent == ParentT) continue;

                if (childInfo.collider.gameObject == _player.gameObject) _player.Parent(ParentT);
                else childInfo.collider.transform.parent = ParentT;

                //Add (if set) inverted parent velocity to children on unparenting
                //if (childInfo.rigidbody && (_mainRB || _kinematicMovement))
                //{
                //    Vector3 vel = _kinematicMovement ? _kinematicMovement.CurrentVelocity : _mainRB.velocity;
                //    childInfo.rigidbody.AddForce(-vel, ForceMode.VelocityChange);
                //}
            }
        }
    }

    private void OnStayTrigger(Collider collider)
    {
        if (!this.enabled || collider == null) return;

        //update or create its childinfo (just because their is a childinfo, doesnt mean it is a active child, we just keep their data for future parenting)
        ChildInfo childInfo = _children.Find(x => x.collider == collider);

        //Allow re-use leftover infos (of deleted objects)
        if (childInfo == null) _children.Find(x => x.collider == null);

        //If still null, create new
        if (childInfo == null)
        {
            childInfo = new ChildInfo(collider);
            _children.Add(childInfo);
        }
        else childInfo.collider = collider;

        childInfo.AddTouch(collider);

        //Evaluate initially
        EvaluateChildren();
    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    if (!this.enabled) return;

    //    //Important to note, as far as i understand it, to know what collider was hit on this gameobject ('thisCollider'), one must loop over all contact points and retrieve it from one of them, even though they are all the fuckin same colliders.
    //    //Unity fires collision events (such as this function) for each unique pair of colliders/rigidbodies between 2 gameobjects.
    //    //Then why is 'thisCollider' not on the collision object just as the 'otherCollider' is? Im typing this here cuz i dont have a clue and might be wrong about how all of this works so ye... Best case scenerio I do underestand this correctly and somebody should be fired.

    //    //go over contact points, check if their is one valid (only grounded touches will parent the character)
    //    Collider thisCollider = null;
    //    bool isValid = false;
    //    foreach (ContactPoint contact in collision.contacts)
    //    {
    //        thisCollider = contact.thisCollider; //We could have 1000 contact points but this would be the exact same everytime! So would this: "contact.otherCollider"...
    //        if (Vector3.Angle(-contact.normal, Vector3.up) > character.Movement.GroundedAngle) continue;

    //        //Character is a valid child! (no need to continue loop)
    //        isValid = true;
    //        break;
    //    }

    //    //update or create its childinfo (just because their is a childinfo, doesnt mean it is a active child, we just keep their data for future parenting)
    //    ChildInfo childInfo = _children.Find(x => x.collider == character || x.collider == null); //also allow using left over infos (child null)
    //    if (childInfo == null)
    //    {
    //        childInfo = new ChildInfo(character);
    //        _children.Add(childInfo);
    //    }
    //    else childInfo.collider = character;

    //    //check if valid touch or not 
    //    if (isValid)
    //    {
    //        childInfo.AddTouch(thisCollider);
    //    }
    //    else
    //    {
    //        //this will just try to remove it in case it is an active child (would only happen if angle was okay previously, but now due to moving for example is not anymore)
    //        childInfo.RemoveTouch(thisCollider);
    //    }
    //}
}

public sealed class ChildInfo
{
    public Collider collider = null;
    public List<Collider> touches = new List<Collider>();
    public Rigidbody rigidbody = null;

    public ChildInfo(Collider collider)
    {
        this.collider = collider;

        //Optionally cache rb
        this.rigidbody = collider.GetComponent<Rigidbody>();
    }

    public void AddTouch(Collider collider)
    {
        if (touches.Contains(collider)) return;

        touches.Add(collider);
    }

    public void RemoveTouch(Collider collider)
    {
        touches.Remove(collider);
    }
}