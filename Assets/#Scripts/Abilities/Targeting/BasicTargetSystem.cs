using System.Collections.Generic;
using UnityEngine;

public sealed class BasicTargetSystem : TargetSystem
{
    [SerializeField] private bool _onlyTargetSelf = false;
    //[Tooltip("Default effectiveness value for when random is true"), SerializeField] private float _randomTargetEff = 1.0f;

    protected override void PopulateTargetPairs(ref List<TargetPair> targets)
    {
        float effectiveness = 0.0f;

        // If targetself ignore others
        if (_onlyTargetSelf)
        {
            GameObject source = Source;

            effectiveness = EffectivenessTarget(source);
            if (effectiveness >= _minEffectivenessForValid) targets.Add(new TargetPair(source, effectiveness, DefaultTargetLifeTime));
            return;
        }

        foreach (PlayerN player in GameManager.Instance.SceneData.Players)
        {
            if (!player) continue;
            if (!IsTargetValid(player.gameObject)) continue;
         
            effectiveness = EffectivenessTarget(player.gameObject);
            if (effectiveness >= _minEffectivenessForValid) targets.Add(new TargetPair(player.gameObject, effectiveness, DefaultTargetLifeTime));
        }


        //TODO -> add targets through character manager
        //TODO -> add targets through building/targetable environment manager

        //go over all characters, if random add all of them if valid
        //foreach (Character character in CharacterManager.Instance.Characters)
        //{
        //    if (!IsTargetValid(character.gameObject)) continue;

        //    effectiveness = EffectivenessTarget(character.gameObject);
        //    if (effectiveness > _minEffectivenessForValid) _targetPairs.Add(new TargetPair(character.gameObject, effectiveness));
        //}
    }
}