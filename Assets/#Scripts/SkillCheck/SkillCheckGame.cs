using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public enum SkillCheckGameType { slider, }
public abstract class SkillCheckGame : MonoBehaviour
{
    [SerializeField] private SkillCheckGameType _type = default;
    [SerializeField] private float _maxDuration = 180.0f;


    private TaskCompletionSource<bool> _resultTCS = null;
    private Coroutine _endGameCo = null;

    public SkillCheckGameType Type { get { return _type; } }

    public bool InProgress { get; private set; }
    public float Elapsed { get; private set; }
    public bool EndingGame { get; private set; }

    public PlayerN Player { get; private set; }


    public bool TryStart(PlayerN player, out TaskCompletionSource<bool> resultTCS)
    {
        resultTCS = null;
        if (player == null) return false;
        if (InProgress) return false;

        Player = player;
        _resultTCS = new TaskCompletionSource<bool>();
        StartGame();

        resultTCS = _resultTCS;
        return true;
    }

    public bool ForceEnd(bool succes = false, float delay = 0.0f)
    {
        if (!CanEnd()) return false;

        EndGame(succes, delay);
        return true;
    }

    public bool CanEnd()
    {
        if (!InProgress) return false;
        if (EndingGame) return false;

        return true;
    }


    protected void Update()
    {
        UpdateLifeTime();
    }

    private void UpdateLifeTime()
    {
        if (!InProgress) return;

        Elapsed += Time.deltaTime;
        if (_maxDuration > 0.0f && Elapsed > _maxDuration)
        {
            EndGame(false);
        }
    }

    private void StartGame()
    {
        InProgress = true;

        GameStarted();
    }

    protected void EndGame(bool succes, float delay = 0.0f)
    {
        if (!InProgress) return;

        StopAllCoroutines();
        StartCoroutine(EndGameCo(succes, delay));
    }

    private IEnumerator EndGameCo(bool succes, float delay)
    {
        EndingGame = true;

        if (delay > 0.0f)
            yield return new WaitForSeconds(delay);

        _resultTCS.SetResult(succes);
        InProgress = false;
        GameEnded(succes);

        EndingGame = false;

        yield return null;
    }

    protected abstract void GameStarted();

    protected abstract void GameEnded(bool succes);
}