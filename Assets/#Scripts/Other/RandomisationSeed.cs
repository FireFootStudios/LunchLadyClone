using UnityEngine;

public sealed class RandomisationSeed : MonoBehaviour
{
    [SerializeField] private int _randomSeed = 666;
    [SerializeField] private bool _randomiseSeed = false;
    [Space]
    [SerializeField] private Health _resetOnRevive = null;
    [SerializeField] private bool _resetOnSessionReset = true;

    private System.Random _randomInstance = null;

    public int RandomInt
    {
        get
        {
            Init();
            return _randomInstance.Next();
        }
    }

    public float RandomFloat
    {
        get
        {
            Init();
            return (float)_randomInstance.NextDouble();
        }
    }

    public float RandomRange(float min, float max)
    {
        Init();

        return Mathf.Lerp(min, max, RandomFloat);
    }

    public int RandomRange(int min, int max)
    {
        Init();

        return (int)Mathf.Lerp(min, max - 1, RandomInt);
    }


    private void Awake()
    {
        if (_resetOnSessionReset) GameMode.OnSessionStart += OnSessionStart;
        if (_resetOnRevive) _resetOnRevive.OnRevive += Resett;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_randomiseSeed)
        {
            _randomSeed = Random.Range(0, int.MaxValue);

            _randomiseSeed = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void OnDestroy()
    {
        GameMode.OnSessionStart -= OnSessionStart;
        if (_resetOnRevive) _resetOnRevive.OnRevive -= Resett;
    }

    private void Init()
    {
        if (_randomInstance != null) return;

        _randomInstance = new System.Random(_randomSeed);
    }

    private void OnSessionStart(GameMode _)
    {
        Resett();
    }

    private void Resett()
    {
        _randomInstance = new System.Random(_randomSeed);
    }
}