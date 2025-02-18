using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TimeScaleManager : SingletonBase<TimeScaleManager>
{
    private List<TimeScaleMod> _timeScaleMods = new List<TimeScaleMod>();

    private float _defaultFixedStep = 1.0f;

    private sealed class TimeScaleMod
    {
        public float value = 1.0f;
        public float targetValue = 1.0f;
        public MonoBehaviour source = null;

        // Unscaled
        public float maxDuration = 0.0f;
        public float elapsed = 0.0f;

        public Coroutine coroutine = null;
    }

    public void AddTimeScaleMod(float targetMod, MonoBehaviour source, float maxDuration = 0.0f, float smoothInSpeed = 0.0f, float smoothOutSpeed = 0.0f)
    {
        //TODO: optionally reuse or update mod isntead (start from current value)

        TimeScaleMod mod = new TimeScaleMod
        {
            value = 1.0f,
            targetValue = targetMod,
            source = source,
            maxDuration = maxDuration,
        };

        //Start manage coroutine
        mod.coroutine = StartCoroutine(ManageTimeMod(mod, smoothInSpeed, smoothOutSpeed));

        _timeScaleMods.Add(mod);
        UpdateTimeScale();
    }

    //Remove all time scale mods having passed source
    public void RemoveTimeScaleMod(MonoBehaviour source)
    {
        _timeScaleMods.RemoveAll(t => t.source == source);
        UpdateTimeScale();
    }

    private IEnumerator ManageTimeMod(TimeScaleMod mod, float smoothInSpeed, float smoothOutSpeed)
    {
        //calculate smooth time from smooth speed and target time scale
        float smoothTimeIn = 0.0f;
        float smoothTimeOut = 0.0f;

        if (smoothInSpeed > 0.0f) smoothTimeIn = Mathf.Abs(mod.value - mod.targetValue) / smoothInSpeed;
        if (smoothOutSpeed > 0.0f) smoothTimeOut = Mathf.Abs(1.0f - mod.targetValue) / smoothOutSpeed;

        //calculate adjusted duration of wait based on target time scale and smoothtime
        //float adjustedDuration = Mathf.Clamp(mod.maxDuration * _targetTimeScale - (smoothTime * _targetTimeScale), 0.0f, float.MaxValue);

        //smooth towards target timescale
        while (mod.elapsed < smoothTimeIn)
        {
            mod.elapsed += Time.unscaledDeltaTime;
            mod.value = Mathf.MoveTowards(mod.value, mod.targetValue, smoothInSpeed * Time.unscaledDeltaTime);

            if (mod.elapsed < smoothTimeIn) yield return null;
        }

        //Set value to target value
        mod.value = mod.targetValue;
        yield return null;

        //Wait for max duration
        float waitDuration = mod.maxDuration - smoothTimeOut;
        while (mod.elapsed < waitDuration)
        {
            mod.elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        //Smooth out
        while (mod.elapsed < mod.maxDuration)
        {
            mod.elapsed += Time.unscaledDeltaTime;
            mod.value = Mathf.MoveTowards(mod.value, 1.0f, smoothOutSpeed * Time.unscaledDeltaTime);

            if (mod.elapsed < mod.maxDuration) yield return null;
        }

        //End by removing this mod (unless mod has no max duration)
        if (mod.maxDuration > 0.0f) _timeScaleMods.Remove(mod);

        yield return null;
    }

    protected override void Awake()
    {
        base.Awake();

        _defaultFixedStep = Time.fixedDeltaTime;
    }

    private void Update()
    {
        UpdateTimeScaleMods();
    }

    private void UpdateTimeScaleMods()
    {
        // Update timescale mods if they have null source, it got disabled or duration was > 0 and elapsed is < 0
        for (int i = 0; i < _timeScaleMods.Count; i++)
        {
            TimeScaleMod mod = _timeScaleMods[i];
            //mod.lifeElapsed += Time.unscaledDeltaTime;

            if (mod == null || mod.source == null/* || !mod.source.activeInHierarchy*//* || (mod.maxDuration > 0.0f && mod.lifeElapsed > mod.maxDuration)*/)
            {
                _timeScaleMods.Remove(mod);
                i--;
            }
        }

        UpdateTimeScale();
    }

    private void UpdateTimeScale()
    {
        float timeScale = 1.0f;

        // Calculate final timescale
        foreach (TimeScaleMod timeScaleMod in _timeScaleMods)
            timeScale *= timeScaleMod.value;

        Time.timeScale = timeScale;

        // Adjust fixed delta time so we have smooth gameplay
        Time.fixedDeltaTime = timeScale * _defaultFixedStep;
    }
}