using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _fpsTMP = null;
    [SerializeField] private int _maxFramesTrack = 60;
    [SerializeField] private float _updateInterval = 0.2f;

    private Queue<float> _prevFPS = new Queue<float>();
    private float _updateTimer = 0.0f;


    void Update()
    {
        if (!_fpsTMP) return;

        // Current pfs
        float currentfps = 1.0f / Time.unscaledDeltaTime;

        // Add to queue
        if (_prevFPS.Count >= _maxFramesTrack) _prevFPS.Dequeue();
        _prevFPS.Enqueue(currentfps);

        _updateTimer -= Time.unscaledDeltaTime;
        if (_updateTimer > 0.0f) return;
        _updateTimer = _updateInterval;

        // Calc avg fps
        float avgFPS = 0.0f;
        foreach (float fps in _prevFPS)
            avgFPS += fps;

        avgFPS /= _prevFPS.Count;


        _fpsTMP.text = ((int)avgFPS).ToString();
    }
}