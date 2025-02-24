using UnityEngine;

public sealed class PlayerLamp : MonoBehaviour
{
    [SerializeField] private PlayerN _player = null;

    private void Update()
    {
        if (!_player || !_player.PlayerCameras) return;

        // Only for player owner
        if (!_player.IsOwner) return;

        // Set rotation equal to player look direction for now
        transform.forward = _player.PlayerCameras.transform.forward;
    }
}