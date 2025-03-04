using System;
using UnityEngine;

public sealed class SliderGame : SkillCheckGame
{
    public static Action<SliderGame> OnStarted;
    public static Action<SliderGame> OnEnded;


    protected override void GameStarted()
    {
        OnStarted?.Invoke(this);
    }

    protected override void GameEnded(bool succes)
    {
        OnEnded?.Invoke(this);
    }
}