using UnityEngine;

public sealed class BasicTargetSystem : TargetSystem
{
    [SerializeField] private bool _onlyTargetSelf = false;
    [SerializeField] private bool _random = false;
    //[Tooltip("Default effectiveness value for when random is true"), SerializeField] private float _randomTargetEff = 1.0f;

    protected override void UpdateCurrentTargetPairs()
    {
        //check if target null or has 0 effectiveness, remove if any true
        for (int i = 0; i < _targetPairs.Count; i++)
        {
            TargetPair targetPair = _targetPairs[i];
            if (!targetPair.target) _targetPairs.Remove(targetPair);
            else if(!_random)
            {
                //recalculate effectiveness
                targetPair.effectiveness = EffectivenessTarget(targetPair.target);
                if (targetPair.effectiveness < _minEffectivenessForValid) _targetPairs.Remove(targetPair);
            }

            //if targetpair got removed, decrement index
            if (!_targetPairs.Contains(targetPair)) i--;
        }
    }

    protected override void PopulateTargetPairs()
    {
        float effectiveness = 0.0f;

        //if targetself ignore others
        if (_onlyTargetSelf)
        {
            GameObject source = Source;

            effectiveness = EffectivenessTarget(source);
            if (effectiveness >= _minEffectivenessForValid) _targetPairs.Add(new TargetPair(source, effectiveness));
            return;
        }

        foreach (PlayerN player in GameManager.Instance.SceneData.Players)
        {
            if (!IsTargetValid(player.gameObject)) continue;
         
            effectiveness = EffectivenessTarget(player.gameObject);
            if (effectiveness >= _minEffectivenessForValid) _targetPairs.Add(new TargetPair(player.gameObject, effectiveness));
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

        //sort OR randomise list
        if (_targetPairs.Count > 1)
        {
            if (_random) _targetPairs.Shuffle();
            else
            {
                //sort targets based on caculated effectivenesses
                _targetPairs.Sort((a, b) =>
                {
                    return b.effectiveness.CompareTo(a.effectiveness);
                });
            }
        }
    }
}