using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class BadGuyEvents : MonoBehaviour
{
    [SerializeField] private BadGuy _badGuy = null;
    //[SerializeField] private List<BGPaperEventData> _paperEvents = new List<BGPaperEventData>();

    [Space]
    [SerializeField] private BGPaperEventData _paperEventData = null;
    [SerializeField] private Vector2 _aggroDurDistScaleBounds = new Vector2(10.0f, 50.0f);
    [SerializeField] private int _paperCountForAggro = 5;
    [Space]
    [SerializeField] private BadGuyEventData _jackBoxActivateEvent = null;

    private MainGamemode _gamemode = null;
    private List<BadGuyEvent> _activeEvents = new List<BadGuyEvent>();


    private void Awake()
    {
        MainGamemode.OnPaperPickedUp += OnPaperPickedUp;
        JackBox.OnJackBoxActivate += OnJackBoxActivate;

        GameMode currentGM = GameManager.Instance.CurrentGameMode;
        _gamemode = currentGM is MainGamemode ? currentGM as MainGamemode : null;
    }

    private void OnDestroy()
    {
        MainGamemode.OnPaperPickedUp -= OnPaperPickedUp;
    }

    private void OnPaperPickedUp(ItemN item)
    {
        if (!_gamemode) return;
        if (!_badGuy.IsHost) return;

        // Look for events with current paper
        //BGPaperEventData eventData = _paperEvents.Find(e => e.paperCount == _gamemode.PapersCollected);
        //if (eventData == null) return;

        bool aggroSource = _gamemode.PapersCollected >= _paperCountForAggro;
        _paperEventData.aggroSource = aggroSource;

        // Create event from data
        AddEvent(_paperEventData, item.PickUpSource);
    }

    private void OnJackBoxActivate(JackBox jackbox)
    {
        if (!_gamemode) return;
        if (!_badGuy.IsHost) return;

        // Look for events with current paper
        //BGPaperEventData eventData = _paperEvents.Find(e => e.paperCount == _gamemode.PapersCollected);
        //if (eventData == null) return;

        PlayerN source = null;
        if (jackbox.FireTarget) jackbox.FireTarget.TryGetComponent(out source);

        // Create event from data
        AddEvent(_jackBoxActivateEvent, source);
    }

    private void AddEvent(BadGuyEventData eventData, PlayerN source)
    {
        if (!_badGuy || eventData == null) return;

        // Create and add event
        BadGuyEvent gEvent = new BadGuyEvent();
        gEvent.Init(eventData);
        _activeEvents.Add(gEvent);

        // Execute Event stuff \/\/\/
        
        // Move Mod
        _badGuy.Movement.AddOrUpdateModifier(eventData.moveMod, false, true);

        // Aggro players
        if (eventData.aggroSource && source)
        {
            _badGuy.Behaviour.AggroTargetSystem.AddOverrideTarget(source.gameObject, 10.0f, AggroDur(eventData.aggroDurBounds, source.transform.position));
        }
        else if (eventData.aggroRandom)
        {
            PlayerN randomPlayer = GameManager.Instance.SceneData.Players.RandomElement();
            if (randomPlayer) _badGuy.Behaviour.AggroTargetSystem.AddOverrideTarget(randomPlayer.gameObject, .1f, AggroDur(eventData.aggroDurBounds, source.transform.position));
        }
    }

    private float AggroDur(Vector2 durBounds, Vector3 targetPos)
    {
        float duration = durBounds.x;



        // Calculate the path to the target position.
        NavMeshPath path = new NavMeshPath();

        // Create and configure the filter.
        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = _badGuy.NavMeshAgent.agentTypeID;
        // Optionally, set a custom area mask:
        filter.areaMask = NavMesh.AllAreas;

        if (NavMesh.CalculatePath(transform.position, targetPos, filter, path))
        {
            float distance = 0f;

            // Sum the distances between consecutive corners.
            for (int i = 1; i < path.corners.Length; i++)
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);

            float scalePerc = Mathf.InverseLerp(_aggroDurDistScaleBounds.x, _aggroDurDistScaleBounds.y, distance);
            duration = Mathf.Lerp(durBounds.x, durBounds.y, scalePerc);
        }

        return duration;
    }


    //private void Update()
    //{
    //    //for (int i = 0; i < _activeEvents.Count; i++)
    //    //{
    //    //    //if ((gEvent.Data.duration > 0)) continue;
    //    //    BadGuyEvent gEvent = _activeEvents[i];
    //    //    gEvent.Elapsed += Time.deltaTime;

    //    //    if (gEvent.Data.duration > 0.0f && gEvent.Elapsed > gEvent.Data.duration)
    //    //    {
    //    //        _activeEvents.RemoveAt(i);
    //    //        i--;
    //    //    }
    //    //}


    //    foreach(BadGuyEvent badGuyEvent in _events)
    //    {

    //    }
    //}
}


[System.Serializable]
public class BadGuyEventData
{
    public MovementModifier moveMod = null;
    public bool aggroRandom = false;
    public bool aggroSource = false;
    public Vector2 aggroDurBounds = new Vector2(5.0f, 50.0f);
}

[System.Serializable]
public class BGPaperEventData : BadGuyEventData
{
    public int paperCount = 0;
}

public sealed class BadGuyEvent
{
    public BadGuyEventData Data { get; private set; }
    public float Elapsed { get; set; }

    public void Init(BadGuyEventData badGuyEventData)
    {
        Data = badGuyEventData;
    }
}