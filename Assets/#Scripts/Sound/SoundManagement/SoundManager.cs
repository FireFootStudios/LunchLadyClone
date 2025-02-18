using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;


public enum SoundType { effectsDefault, music, ambient, dialogue }
public sealed class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private List<SoundTemplate> _soundTemplates = new List<SoundTemplate>();
    [SerializeField] private AudioMixer _mixer = null;
    [SerializeField, Tooltip("Min and max decibel for the mixer group volumes")] private Vector2 _mixerVolumeBounds = new Vector2(-20.0f, 5.0f);
    [SerializeField] private float _dialogueSnapshotTransitionTime = 0.5f;

    private Dictionary<SoundType, List<Sound>> _activeSounds = new Dictionary<SoundType, List<Sound>>();
    private Dictionary<SoundType, List<Sound>> _inactiveSounds = new Dictionary<SoundType, List<Sound>>();

    private List<SoundSpawnData> _trackedSpawners = new List<SoundSpawnData>(); // This is used in cases where sound spawning has a cooldown which needs to be tracked

    // Cached volume strings
    private const string _masterVolumeStr = "Volume_Master";
    private const string _musicVolumeStr = "Volume_Music";
    private const string _ambientVolumeStr = "Volume_Ambient";
    private const string _effectsVolumeStr = "Volume_Effects";
    private const string _dialogueVolumeStr = "Volume_Dialogue";

    private const string _normalSnapshot = "Normal";
    private const string _dialogueSnapshot = "ActiveDialogue";

    //private SettingsData _settings = null;

    // Paused?


    [System.Serializable]
    private class SoundTemplate
    {
        public SoundType type = 0;
        public Sound template = null;
    }


    #region PublicMethods
    //Should only be called for cleaning up a sound that was on object that got deleted (cannot recover sound)
    public void RemoveSound(Sound sound)
    {
        _activeSounds[sound.Type].Remove(sound);
        _inactiveSounds[sound.Type].Remove(sound);
    }

    public void EvaluateSound(Sound sound)
    {
        if (!sound.CanPool) return;

        //Try remove
        RemoveSound(sound);

        //Is sound currently active?
        bool isActive = sound.gameObject.activeInHierarchy || sound.AudioSource.isPlaying;

        //Add to correct list
        if (isActive) _activeSounds[sound.Type].Add(sound);
        else
        {
            _inactiveSounds[sound.Type].Add(sound);

            //parent the sound object back to this
            sound.transform.parent = this.transform;
        }
    }

    //TODO: Implement canPool = false, this would require us to have a seperate container to track these non pooled sounds and when their sources/origins are
    //null/disable we can readd them to the pool or clean them up -> this would ofcourse require a origin to be passed in the first place...
    public Sound PlaySound(SoundSpawnData spawnData, GameObject origin = null/*, bool canPool = true*/)
    {
        if (spawnData == null || spawnData.clips.Count == 0) return null;

        return SpawnSounds(spawnData, origin, true);
    }

    //Meant for looping sounds mainly, but also ideal when wanting to cache a sound which we want to stop at some point are check if finished, since this version does not reuse the sound object
    //Allowing a sound object to be passed (which preferably shpuld be the one we got from calling the function initially) is just for efficieny/performance to preserve some kind of pooling
    //This sound object can always be reAdded to the pool by setting CanPool to true and reevaluating the sound, just make sure to lose the reference!
    public bool PlayReUseSound(SoundSpawnData spawnData, ref Sound sound, GameObject origin = null)
    {
        if (spawnData == null || spawnData.clips.Count == 0) return false;

        //Get a sound object first if there is none passed yet
        if (!sound) sound = GetSoundForUse(spawnData.soundType, false);

        return ReSpawnSound(sound, spawnData, origin);
    }

    //public Sound GetSoundReUse(SoundSpawnData spawnData)
    //{
    //    if (spawnData == null || spawnData.clips.Count == 0) return null;

    //    return SpawnSounds(spawnData);
    //}

    public void StopAll(bool includeDontDestroyOnLoad = false)
    {
        foreach (KeyValuePair<SoundType, List<Sound>> sounds in _activeSounds)
        {
            for (int i = 0; i < sounds.Value.Count; i++)
            {
                Sound sound = sounds.Value[i];

                //Is this object not part of a scene (so dontDestroyOnLoad is activated)
                if (sound.gameObject.scene.buildIndex == -1) continue;

                sound.Stop();
                i--;
            }
        }

        StopAllCoroutines();
    }

    #endregion



    #region PrivateMethods

    protected override void Awake()
    {
        base.Awake();

        if (_isDestroying) return;

        //Create lists for each sound template
        foreach (SoundTemplate template in _soundTemplates)
        {
            _activeSounds.Add(template.type, new List<Sound>());
            _inactiveSounds.Add(template.type, new List<Sound>());
        }

        //SettingsView.OnSettingsChanged += UpdateWithSettings;
        //SaveManager.OnLoaded += UpdateOnLoad;
    }

    private void OnDestroy()
    {
        //SettingsView.OnSettingsChanged -= UpdateWithSettings;
        //SaveManager.OnLoaded -= UpdateOnLoad;
    }

    private void Update()
    {
        for (int i = 0; i < _trackedSpawners.Count; i++)
        {
            SoundSpawnData data = _trackedSpawners[i];

            // Update cooldown
            if (data != null) data.CooldownTimer -= Time.deltaTime;

            // Remove if null or no more cooldown
            if (data == null || data.CooldownTimer < 0.0f)
            {
                _trackedSpawners.RemoveAt(i);
                i--;
            }
        }
    }

    //private void UpdateWithSettings(SettingsData settingsData)
    //{
    //    if (settingsData == null) return;

    //    _settings = settingsData;

    //    SetMixerGroupVolume(_masterVolumeStr, _mixerVolumeBounds, settingsData.masterVolume);
    //    SetMixerGroupVolume(_musicVolumeStr, _mixerVolumeBounds, settingsData.musicVolume);
    //    SetMixerGroupVolume(_ambientVolumeStr, _mixerVolumeBounds, settingsData.ambientVolume);
    //    SetMixerGroupVolume(_effectsVolumeStr, _mixerVolumeBounds, settingsData.effectsVolume);
    //    SetMixerGroupVolume(_dialogueVolumeStr, _mixerVolumeBounds, settingsData.dialogueVolume);
    //}

    private void SetMixerGroupVolume(string paramName, Vector2 bounds, float volume)
    {
        // Min bounds are probably still audable to have a even slider in UI, thus we manually set it to the min decibel if setting is at 0 volume
        if (volume > 0.01f) _mixer.SetFloat(paramName, Mathf.Lerp(bounds.x, bounds.y, volume));
        else
        {
            _mixer.SetFloat(paramName, -80.0f);
        }
    }

    //private void UpdateOnLoad(Save saveData, SaveLoadResult result, bool succes)
    //{
    //    UpdateWithSettings(saveData.settingsData);
    //}

    private Sound SpawnSounds(SoundSpawnData spawnData, GameObject origin, bool canPool)
    {
        // Spawn loop
        Sound sound;

        // Frequency
        if (spawnData.frequency < 1.0f && UnityEngine.Random.value > spawnData.frequency) return null;

        // Cooldown, return if timer > 0.0f
        if (spawnData.cooldown > 0.0f)
        {
            if (spawnData.CooldownTimer > 0.0f) return null;
            else
            {
                // Set the cooldown + track if not yet
                spawnData.CooldownTimer = spawnData.cooldown;
                if (!_trackedSpawners.Contains(spawnData)) _trackedSpawners.Add(spawnData);
            }
        }

        // Get sound for playing
        sound = GetSoundForUse(spawnData.soundType, canPool);
        if (!sound) return null;

        // Set parent, always aprent 1 above actual parent to account for evaluating sounds
        sound.transform.parent = spawnData.parent ? Utils.GetUpperTransform(spawnData.parent.transform, spawnData.parentUp) : null;

        // Always make sure we are part of the scene in case no parent is specified
        if (!spawnData.parent) SceneManager.MoveGameObjectToScene(sound.gameObject, SceneManager.GetActiveScene());

        // Play sound
        sound.Play(spawnData, origin);

        return sound;
    }

    private bool ReSpawnSound(Sound sound, SoundSpawnData spawnData, GameObject origin)
    {
        if (!sound) return false;

        // Frequency
        if (spawnData.frequency < 1.0f && UnityEngine.Random.value > spawnData.frequency) return false;

        // Cooldown, return if timer > 0.0f
        if (spawnData.cooldown > 0.0f)
        {
            if (spawnData.CooldownTimer > 0.0f) return false;
            else
            {
                // Set the cooldown + track if not yet
                spawnData.CooldownTimer = spawnData.cooldown;
                if (!_trackedSpawners.Contains(spawnData)) _trackedSpawners.Add(spawnData);
            }
        }

        // Set parent
        sound.transform.parent = spawnData.parent ? Utils.GetUpperTransform(spawnData.parent.transform, spawnData.parentUp) : null;

        // Always make sure we are part of the scene in case no parent is specified
        if (!spawnData.parent) SceneManager.MoveGameObjectToScene(sound.gameObject, SceneManager.GetActiveScene());

        // This will automatically cause sound to evaluate itself onEnable
        sound.gameObject.SetActive(true);

        // Play sound
        sound.Play(spawnData, origin);

        return true;
    }

    private Sound GetSoundForUse(SoundType type, bool canPool)
    {
        Sound sound = null;

        //find inactive sound or create new sound
        if (_inactiveSounds[type].Count > 0)
        {
            //Get from pool
            sound = _inactiveSounds[type][0];

            //Remove from inactive list
            _inactiveSounds[type].RemoveAt(0);

            //reset local position, rotation and scale
            sound.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            sound.transform.localScale = Vector3.one;

            //This will automatically cause sound to evaluate itself onEnable
            sound.gameObject.SetActive(true);
        }
        else
        {
            Sound template = _soundTemplates.Find(st => st.type == type).template;

            //create new sound
            sound = Instantiate(template, this.transform);
        }

        //Wheter the sound comes from a pool or was newly created, set whether it can be pooled on evaluate or not!
        sound.CanPool = canPool;

        return sound;
    }
    #endregion
}