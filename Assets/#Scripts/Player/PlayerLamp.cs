using UnityEngine;

public sealed class PlayerLamp : MonoBehaviour
{
    [SerializeField] private PlayerN _player = null;

    private void Update()
    {
        if (!_player || !_player.PlayerCameras) return;

        // Only host needs to do this
        if (!_player.IsHost) return;

        // Set rotation equal to player look direction for now
        transform.forward = _player.PlayerCameras.transform.forward;
    }
}