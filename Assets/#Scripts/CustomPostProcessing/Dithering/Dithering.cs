using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Custom/Dithering")]
public sealed class Dithering : VolumeComponent, IPostProcessComponent
{
    [SerializeField] private BoolParameter _isActive = new BoolParameter(true);

    [SerializeField] private ClampedFloatParameter _spread = new ClampedFloatParameter(value: 0.5f, min: 0, max: 1, overrideState: true);
    
    [SerializeField] private NoInterpClampedIntParameter _colorCount = new NoInterpClampedIntParameter(value: 128, 2, 256, true);

    [SerializeField] private NoInterpClampedIntParameter _bayerLevel = new NoInterpClampedIntParameter(value: 0, 0, 2, true);

    public bool IsActive() { return _isActive.value; }

    public bool IsTileCompatible() { return true; }

    public float Spread { get { return _spread.value; } }

    public int ColorCount { get { return _colorCount.value; } }

    public int BayerLevel { get { return _bayerLevel.value; } }
}