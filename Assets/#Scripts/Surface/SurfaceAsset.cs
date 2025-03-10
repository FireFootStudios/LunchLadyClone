using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceX", menuName = "ScriptableObjects/SurfaceAsset", order = 1)]
public sealed class SurfaceAsset : ScriptableObject
{
    //[SerializeField] private SurfaceType _surfaceType = 0;
    [SerializeField] private bool _overrideVelMultBounds = false;
    [SerializeField] private Vector2 _onGroundedVelMultiplierBounds = Vector2.zero;

    [Space]
    [SerializeField] private List<AudioClip> _impactClips = new List<AudioClip>();
    [SerializeField] private List<ParticleSystem> _impactPsTemplates = new List<ParticleSystem>();

    [Space]
    [SerializeField] private List<AudioClip> _stepClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _landClips = new List<AudioClip>();
    [SerializeField] private List<AudioClip> _jumpClips = new List<AudioClip>();

    //public SurfaceType SurfaceType { get { return _surfaceType; } }
    public List<AudioClip> StepClips { get { return _stepClips; } }
    public List<AudioClip> LandClips { get { return _landClips; } }
    public List<AudioClip> JumpClips { get { return _jumpClips; } }

    public List<AudioClip> ImpactClips { get { return _impactClips; } }
    public List<ParticleSystem> ImpactPsTemplates { get { return _impactPsTemplates; } }

    public Vector2 OnGroundedVelMultBounds { get { return _onGroundedVelMultiplierBounds; } }
    public bool OverrideVelMultBounds { get { return _overrideVelMultBounds; } }
}