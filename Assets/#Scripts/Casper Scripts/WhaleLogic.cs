using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WhaleLogic : MonoBehaviour
{
    public Health Health { get { return _monsterHP; } }
    public float Speed { get => _speed; set => _speed = value; }

    [SerializeField] private float _speed = 10.0f;
    [SerializeField] private float _acceleration = 1f;

    private PlayerN _player = null;
    private Health _monsterHP = null;

    void Start()
    {
        _monsterHP = this.gameObject.GetComponent<Health>();
        _player = FindAnyObjectByType<PlayerN>();
        _monsterHP.OnDeath += OnDeath;
    }

    void Update()
    {
        IncreaseSpeed();
        ChasePlayer(_speed, _player);
        LeaveWhenPLayerDied();
    }

    private void IncreaseSpeed()
    {
        if(_speed >= 0.5f)
        {
            _speed -= _acceleration * Time.deltaTime;
        }
    }

    private void ChasePlayer(float speed, PlayerN target)
    {
        if (this.gameObject.activeSelf && target != null)
        {
            this.gameObject.transform.DOMove(new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z), speed, false);
            this.gameObject.transform.DOLookAt(target.transform.position, speed, AxisConstraint.Y, null);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<PlayerN>(out PlayerN player))
        {
            player.Health.Add_Server(-1, this.gameObject);
        }
    }

    private void LeaveWhenPLayerDied()
    {
        if (_player.Health.IsDead)
        {
            Destroy(this.gameObject);
        } 
    }

    private void OnDeath()
    {
        Destroy(this.gameObject);
    }
}
