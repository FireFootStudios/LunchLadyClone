using UnityEngine;

public sealed class Dead : FSMState
{
    private Character _char = null;

    private void Awake()
    {
        _char = GetComponentInParent<Character>();
    }

    public override void OnEnter()
    {
        if (!_char) return;

        if (_char.Movement)
        {
            _char.Movement.CanRotate = false;
            _char.Movement.Stop();
            _char.Movement.ClearModifiers();
        }
    }

    public override void OnExit()
    {
        if (!_char) return;

        if (_char.Movement)
        {
            // Set position to spawn pos
            _char.transform.position = _char.Spawner.SpawnInfo.pos;

            // Clear velocity
            _char.Movement.RB.linearVelocity = Vector3.zero;
        }
    }
}