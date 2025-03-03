using UnityEngine;

public class OnEnableSFX : MonoBehaviour
{
    [SerializeField] private SoundSpawnData _soundData = null;

    private void OnEnable()
    {
        _soundData.StartPos = transform.position;
        SoundManager.Instance.PlaySound(_soundData);
    }
}