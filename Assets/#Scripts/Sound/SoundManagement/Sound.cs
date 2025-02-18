using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public sealed class Sound : MonoBehaviour
{
    [SerializeField] private Vector2 _randomPitchBounds = new Vector2(-0.05f, 0.05f);
    [SerializeField] private bool _ignoreListenerPause = false;

    private static bool _appPaused = false;
    private static SoundManager _soundManager = null;
    private Vector2 _defaultMinMaxDistance = Vector2.zero;
    private bool _defaultRegistered = false;

    private PlayerN _player = null;
    private Collider _followCollider = null;

    private float _maxDuration = 0.0f;

    //Default mixer group from template
    private AudioMixerGroup _defaultMixerGroup = null;

    //private Player _player = null;
    //private SphereCollider _pauseTrigger = null;
    //private const string _triggerLayer = "Trigger";

    public AudioSource AudioSource { get; private set; }
    public SoundType Type { get; private set; } // This will be invalid if the 'Play' function has not been called yet
    public GameObject Origin { get; private set; }
    public bool IsFinished { get { return !this.gameObject.activeSelf || IsFadingOut || (!AudioSource.isPlaying && !AudioListener.pause && !_appPaused); } }
    public bool IsFadingOut { get; private set; }
    public bool CanPool { get; set; }
    public float LifeElapsed { get; set; }

    //public bool Paused { get; private set; }

    //Is fading out property?

    //public void Pause(bool pause)
    //{
    //    if (IsFinished) return;
    //    if (Paused == pause) return;

    //    Paused = pause;
    //    AudioSource.Pause();
    //}

    public void Play(SoundSpawnData spawnData, GameObject origin)
    {
        RegisterDefault();

        //Cache type
        Type = spawnData.soundType;
        Origin = origin;

        //Set position
        if (spawnData.parent && spawnData.setPosToParent)
        {
            transform.position = spawnData.parent.transform.position;
        }
        else transform.position = spawnData.StartPos;

        //set data from spawner
        AudioSource.loop = spawnData.loopAudioSource;
        AudioSource.spatialBlend = spawnData.is2D ? 0.0f : 1.0f;
        AudioSource.spread = spawnData.spread;
        AudioSource.bypassReverbZones = spawnData.ignoreReverb;
        AudioSource.outputAudioMixerGroup = spawnData.overideGroup != null ? spawnData.overideGroup : _defaultMixerGroup;

        //Min/max distance
        AudioSource.maxDistance = spawnData.maxDistanceOverride > 0.0f ? spawnData.maxDistanceOverride : _defaultMinMaxDistance.y;
        AudioSource.minDistance = spawnData.minDistanceOverride > 0.0f ? spawnData.minDistanceOverride : _defaultMinMaxDistance.x;

        //pause trigger (create if not yet done)
        //if (!_pauseTrigger)
        //{
        //    _pauseTrigger = this.gameObject.AddComponent<SphereCollider>();
        //    _pauseTrigger.isTrigger = true;
        //    _pauseTrigger.gameObject.layer = LayerMask.NameToLayer(_triggerLayer);
        //}
        //_pauseTrigger.radius = AudioSource.maxDistance;

        //pick Clip
        SetClip(spawnData);

        //Calc pitch + volume
        AudioSource.pitch = spawnData.pitch + spawnData.randomPitchMultiplier * Utils.GetRandomFromBounds(_randomPitchBounds);
        AudioSource.volume = spawnData.volume;

        //Stop prev coroutines (fade)
        StopAllCoroutines();
        IsFadingOut = false;

        //Fade in/out
        if (spawnData.fadeIn > 0.0f || spawnData.fadeOut > 0.0f)
            StartCoroutine(Fade(spawnData.fadeIn, spawnData.fadeOut));

        //Follow along collider
        _followCollider = spawnData.followPlayerAlongCollider;
        if (_followCollider)
        {
            if (!_player) _player = GameManager.Instance.SceneData.Player;
            UpdateFollowAlongCollider();
        }

        //play audioSource (delay or not)
        float delay;
        if (spawnData.delayBounds != Vector2.zero)
        {
            delay = Utils.GetRandomFromBounds(spawnData.delayBounds);
            AudioSource.PlayDelayed(delay);
        }
        else AudioSource.Play();

        LifeElapsed = 0.0f;
        _maxDuration = spawnData.maxDuration;
    }

    public void Stop()
    {
        //This is here to prevent from evaluating the sound if its parent is being disabled (which throws an error...)
        if (!gameObject.activeInHierarchy) return;
        if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy) return;

        AudioSource.Stop();
        IsFadingOut = false;

        if (CanPool)
        {
            gameObject.SetActive(false);

            //Let the sound manager know we are no longer used
            if (_soundManager) _soundManager.EvaluateSound(this);
        }
    }

    public void StopWithFadeOut(float duration)
    {
        if (IsFinished) return;

        StopAllCoroutines();
        StartCoroutine(FadeOut(duration));
    }

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        AudioSource.ignoreListenerPause = _ignoreListenerPause;

        RegisterDefault();

        //Cache static reference
        _soundManager = SoundManager.Instance;
    }

    private void RegisterDefault()
    {
        if (_defaultRegistered) return;

        _defaultMixerGroup = AudioSource.outputAudioMixerGroup;
        _defaultMinMaxDistance = new Vector2(AudioSource.minDistance, AudioSource.maxDistance);

        _defaultRegistered = true;
    }

    //private void OnEnable()
    //{
    //    _player = GameManager.Instance.SceneData.Player;
    //}

    private void OnDestroy()
    {
        //Let the sound manager know we are being destroyed
        if (_soundManager) _soundManager.RemoveSound(this);
    }

    private void SetClip(SoundSpawnData spawnData)
    {
        if (spawnData.clips.Count == 0) return;

        if (spawnData.clips.Count > 1 && spawnData.ClipIndex == 0 && spawnData.randomiseClipsInitially) spawnData.clips.Shuffle();


        //Check if clip index exceeded the clips amount
        if (spawnData.ClipIndex >= spawnData.clips.Count)
        {
            spawnData.ClipIndex = 0;
            if (spawnData.randomizeClips) spawnData.clips.Shuffle();
        }

        // Randomized clips?
        AudioClip previous = AudioSource.clip;
        if (spawnData.randomizeClips && spawnData.clips.Count > 1)
        {
            //always random?
            if (spawnData.alwaysUseRandomClip)
            {
                int tries = 0;
                while (AudioSource.clip == previous && tries < 10)
                {
                    AudioSource.clip = spawnData.clips.RandomElement();
                    tries++;
                }
            }
            else
            {
                AudioSource.clip = spawnData.clips[spawnData.ClipIndex];
                spawnData.ClipIndex++;
            }
        }
        else
        {
            AudioSource.clip = spawnData.clips[spawnData.ClipIndex];
            spawnData.ClipIndex++;
        }
    }

    private void Update()
    {
        // If done playing while not paused, call Stop which will disable this gameobject and add it back to the sound pool
        if (IsFinished && !IsFadingOut)
        {
            Stop();
            return;
        }

        // Update life elapsed
        LifeElapsed += Time.deltaTime;
        if (_maxDuration > 0.0f && LifeElapsed > _maxDuration)
        {
            Stop();
            return;
        }

        UpdateFollowAlongCollider();
    }

    private void UpdateFollowAlongCollider()
    {
        if (!_followCollider || !_player) return;
        if (!_followCollider.gameObject.activeInHierarchy) return;

        transform.position = _followCollider.ClosestPoint(_player.transform.position);
    }

    private IEnumerator Fade(float fadeInDur, float fadeOutDur /*, float delay*/)
    {
        float defaultVolume = AudioSource.volume;
        float clipTime = AudioSource.clip.length;

        //yield return new WaitForSeconds(delay);

        //Fade in
        while (AudioSource.time < fadeInDur)
        {
            AudioSource.volume = Mathf.Lerp(0.0f, defaultVolume, AudioSource.time / fadeInDur);
            yield return null;
        }

        AudioSource.volume = defaultVolume;

        //Wait for fade out
        while (AudioSource.time < clipTime - fadeOutDur)
        {
            yield return null;
        }

        //Fade out
        while (AudioSource.time < AudioSource.clip.length)
        {
            AudioSource.volume = Mathf.Lerp(defaultVolume, 0.0f, 1.0f - (clipTime - AudioSource.time) / fadeOutDur);
            yield return null;
        }

        AudioSource.volume = 0.0f;

        yield return null;
    }

    private IEnumerator FadeOut(float dur)
    {
        //float adjustedDur = dur;
        //float timeLeft = AudioSource.clip.length - AudioSource.time;
        //if (timeLeft < dur) adjustedDur = timeLeft;
        IsFadingOut = true;

        //Fade out
        float elapsed = 0.0f;
        float startVolume = AudioSource.volume;

        while (elapsed < dur)
        {
            AudioSource.volume = Mathf.Lerp(startVolume, 0.0f, elapsed / dur);
            yield return null;
            elapsed += Time.deltaTime;
        }

        AudioSource.volume = 0.0f;

        Stop();
        yield return null;
    }

    private void OnApplicationPause(bool pause)
    {
        _appPaused = pause;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!_player) return;
    //    if (other.gameObject != _player.gameObject) return;

    //    //Unvirtualize
    //    //AudioSource.virt
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (!_player) return;
    //    if (other.gameObject != _player.gameObject) return;

    //    //Virtualize
    //}
}

[System.Serializable]
public sealed class SoundSpawnData
{
    public List<AudioClip> clips = new List<AudioClip>();
    public bool randomizeClips = true;
    public bool alwaysUseRandomClip = true;
    public bool randomiseClipsInitially = true;

    public GameObject parent = null;
    public bool setPosToParent = true;
    public int parentUp = 1;
    public Collider followPlayerAlongCollider = null;


    [Space]
    [Range(0.01f, 1.0f)] public float volume = 1.0f;
    public float pitch = 1.0f;
    public float randomPitchMultiplier = 1.0f;
    public Vector2 delayBounds = Vector2.zero;
    [Range(0.01f, 1.0f), Tooltip("The chance of playing this sound")] public float frequency = 1.0f;
    
    [Space]
    public bool is2D = false;
    public bool ignoreReverb = false;
    public SoundType soundType = SoundType.effectsDefault;
    public AudioMixerGroup overideGroup = null;

    public float spread = 0.0f;

    [Space]
    [Tooltip("Will only override if value is bigger than zero")] public float maxDistanceOverride = 0.0f;
    [Tooltip("Will only override if value is bigger than zero")] public float minDistanceOverride = 0.0f;

    [Space]
    [Tooltip("Will set the used audio source to looping")] public bool loopAudioSource = false;

    [Space]
    public float fadeIn = 0.0f;
    public float fadeOut = 0.0f;
    public float maxDuration = 0.0f;
    public float cooldown = 0.0f;

    // If parent this will be a position offset, else it will just be its world position
    public Vector3 StartPos { get; set; }

    // Used to determine what clip gets chosen on playing if not fully randomized
    public int ClipIndex {  get; set; }

    public float CooldownTimer { get; set; } // Managed by sound manager if this spawn data has cooldown > 0
}