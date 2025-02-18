using UnityEngine;

public class Raft : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1f;

    private float _oldValue;

    [HideInInspector] public bool IsMoving;


    private void Awake()
    {
        _oldValue = _moveSpeed;
        IsMoving = true;
    }
    private void FixedUpdate()
    {
        transform.Translate(Vector3.forward * _moveSpeed * Time.deltaTime);
    } 

    public void StopRaft()
    {
        _moveSpeed = 0f;
        IsMoving = false;
    }

    public void StartRaft()
    {
        _moveSpeed = _oldValue;
        IsMoving = true;
    }

    
}
