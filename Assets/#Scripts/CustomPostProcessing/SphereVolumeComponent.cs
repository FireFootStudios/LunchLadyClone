using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[VolumeComponentMenu("Custom/SphereVolumeComponent")]
public class SphereVolumeComponent : VolumeComponent, IPostProcessComponent
{
    [SerializeField] private ClampedFloatParameter _intensity = new ClampedFloatParameter(value: 0, min: 0, max: 1, overrideState: true);

    //[SerializeField] private ClampedFloatParameter _spread = new ClampedFloatParameter(value: 0.5f, min: 0, max: 1, overrideState: true);

    //[SerializeField] private NoInterpClampedIntParameter _colorCount = new NoInterpClampedIntParameter(value: 128, 2, 256, true);


    public bool IsActive() => _intensity.value > 0.0f;

    //public float Spread { get { return _spread.value; } }

    //public int ColorCount { get { return _colorCount.value; } }
}