using System.Collections.Generic;
using UnityEngine;

public class SceneData : MonoBehaviour
{
    [Space]
    [SerializeField] private List<Transform> _playerSpawnTs = new List<Transform>();
    [SerializeField] private List<Transform> _tpPoints = new List<Transform>();
    [SerializeField] private List<Transform> _wanderPoints = new List<Transform>();
    [SerializeField] private HitBox _escapeHitBox = null;

    private Level _currentLevel = null;
    //private LevelGroup _currentLevelGroup = null;
    private PlayerN _localPlayer = null;
    private List<PlayerN> _players = new List<PlayerN>();


    //public PlayerN Player
    //{
    //    get
    //    {
    //        if (!_player) _player = FindObjectOfType<PlayerN>(true);
    //        return _player;
    //    }
    //    private set { _player = value; }
    //}

    public PlayerN LocalPlayer
    {
        get
        {
            // For offline testing purposes:
            if (!_localPlayer) _localPlayer = FindObjectOfType<PlayerN>(true);

            return _localPlayer;
        }
        set { _localPlayer = value; }
    }
    public List<PlayerN> Players { get { return _players; } }

    public Level CurrentLevel
    {
        get
        {
            if (_currentLevel == null) _currentLevel = LevelManager.Instance.FindCurrentLevel();
            return _currentLevel;
        }
    }


    public List<Transform> TPPoints { get { return _tpPoints; } }
    public List<Transform> PlayerSpawnTs { get { return _playerSpawnTs; } }
    public List<Transform> WanderPoints { get { return _wanderPoints; } }
    public HitBox EscapeHitBox { get { return _escapeHitBox; } }

#if UNITY_EDITOR
    private void OnValidate()
    {
        //if (_searchCheckpoints)
        //{
        //    _checkPoints.Clear();
        //    _checkPoints.AddRange(FindObjectsOfType<Checkpoint>());

        //    // Order is probably reversed...
        //    _checkPoints.Reverse();

        //    UnityEditor.EditorUtility.SetDirty(this);
        //    _searchCheckpoints = false;
        //}
    }
#endif
}