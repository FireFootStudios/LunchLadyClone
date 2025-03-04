using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class SkillCheckManager : SingletonBaseNetwork<SkillCheckManager>
{
    [SerializeField] private List<SkillCheckGame> _games = null;

    // Local player current game (if any)
    public SkillCheckGame CurrentGame { get; private set; }


    public static Action<SkillCheckGame> OnGameStarted;




    public bool TryStartGame(PlayerN player, SkillCheckGameType type, out TaskCompletionSource<bool> resultTCS)
    {
        resultTCS = null;

        if (player == null) return false;
        if (CurrentGame && CurrentGame.InProgress) return false;

        // Look for game
        CurrentGame = _games.Find(game => game.Type == type);
        if (!CurrentGame) return false;

        // Try start game
        bool gameStarted = CurrentGame.TryStart(player, out resultTCS);
        if (gameStarted)
        {
            OnGameStarted?.Invoke(CurrentGame);
            Debug.Log("Local skill check game started!");
        }

        return gameStarted;
    }

    // Force end current game for local player
    public bool TryEndGame()
    {
        if (!CurrentGame) return false;

        return CurrentGame.ForceEnd();
    }
}