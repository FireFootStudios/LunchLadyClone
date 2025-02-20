using UnityEngine;

public sealed class LoopAmbientSoundSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("OnDisable, and if bigger than 0, will fade out cache sound")] private float _fadeOutOnDisabled = 0.0f;
    //[SerializeField, Tooltip("If this will only have to be played once, set to true")] private bool _destroyCompOnPlay = true;
    [SerializeField] private SoundSpawnData _soundData = null;

    //[Space]
    //[SerializeField] private Collider _followPlayerAlongCollider = null;


    private Sound _sound = null;
    //private Player _player = null;

    private void OnEnable()
    {
        if (_sound && !_sound.IsFinished && _sound.Origin == this.gameObject) return;

        //Play and cache sound
        _sound = SoundManager.Instance.PlaySound(_soundData, this.gameObject);

        //Cache player
        //_player = GameManager.Instance.SceneData.Player;

        //Destroy this component
        //if (_destroyCompOnPlay) Destroy(this);
    }

    private void OnDisable()
    {
        if (!_sound || _sound.Origin != this.gameObject || _sound.IsFinished) return;

        if (_fadeOutOnDisabled > 0.0f) _sound.StopWithFadeOut(_fadeOutOnDisabled);
        else _sound.Stop();
    }
}