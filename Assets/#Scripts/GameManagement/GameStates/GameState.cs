using UnityEngine;

public abstract class GameState : MonoBehaviour
{
    [SerializeField] private GameObject _HUD = null;

    protected GameManager _gameManager = null;

    public GameObject HUD { get { return _HUD; } }

    protected virtual void Awake()
    {
        _gameManager = GameManager.Instance;

        if (_HUD && _gameManager.CurrentState != this) _HUD.SetActive(false);
    }

    public virtual void OnEnter()
    {
        if (_HUD) _HUD.SetActive(true);
    }

    public virtual void OnExit()
    {
        if (_HUD) _HUD.SetActive(false);
    }
}
