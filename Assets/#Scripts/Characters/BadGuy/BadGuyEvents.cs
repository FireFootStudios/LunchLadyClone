using System.Collections.Generic;
using UnityEngine;

public sealed class BadGuyEvents : MonoBehaviour
{
    [SerializeField] private BadGuy _badGuy = null;
    [SerializeField] private List<BGPaperEventData> _paperEvents = new List<BGPaperEventData>();


    private MainGamemode _gamemode = null;
    private List<BadGuyEvent> _activeEvents = new List<BadGuyEvent>();


    private void Awake()
    {
        MainGamemode.OnPaperPickedUp += OnPaperPickedUp;

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
        BGPaperEventData eventData = _paperEvents.Find(e => e.paperCount == _gamemode.PapersCollected);
        if (eventData == null) return;

        // Create event from data
        AddEvent(eventData, item.PickUpSource);
    }

    private void AddEvent(BadGuyEventData eventData, PlayerN source)
    {
        if (!_badGuy || eventData == null) return;

        // Create and add event
        BadGuyEvent gEvent = new BadGuyEvent();
        gEvent.Init(eventData);
        _activeEvents.Add(gEvent);

        // Execute Event
        
        // Move Mod
        _badGuy.Movement.AddOrUpdateModifier(eventData.moveMod, false, true);

        // Aggro players?
        //if(eventData.aggroSource && source)
        //{
        //    _badGuy.Behaviour.AggroTargetSystem.over
        //}
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