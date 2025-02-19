using UnityEngine;

public sealed class Idle : FSMState
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
            _char.Movement.Stop();
            _char.Movement.CanRotate = false;
        }
    }
}