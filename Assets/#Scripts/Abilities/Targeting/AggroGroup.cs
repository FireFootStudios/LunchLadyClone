using System.Collections.Generic;
using UnityEngine;


public class AggroGroup : MonoBehaviour
{
    [SerializeField] private List<Character> _aggroBuddies = new List<Character>();

    [Space]
    [SerializeField] private bool _findAggroBuddies = false;


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_findAggroBuddies)
        {
            _aggroBuddies.Clear();

            foreach (Character buddy in GetComponentsInChildren<Character>(false))
            {
                if (buddy.Behaviour.AggroTargetSystem == null) continue;

                _aggroBuddies.Add(buddy);
            }
            _findAggroBuddies = false;

            //is required for it to be saveable as a prefab?
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Start()
    {
        foreach (Character aggroBuddy in _aggroBuddies)
        {
            if (aggroBuddy.Behaviour.AggroTargetSystem == null) continue;

            aggroBuddy.Behaviour.AggroTargetSystem.OnHasFirstTarget += OnBuddyAggro;
            //aggroBuddy.Spawner += OnRespawn;
        }
    }

    private void OnBuddyAggro(GameObject target)
    {
        if (!target) return;

        foreach (Character aggroBuddy in _aggroBuddies)
        {
            if (!aggroBuddy || !aggroBuddy.Behaviour.AggroTargetSystem) continue;

            //aggroBuddy.AggroTargetSystem.OverrideTarget = target;
            Tag tag = TagManager.Instance.GetTagID(target.tag);
            if (tag == Tag.Invalid || aggroBuddy.Behaviour.AggroTargetSystem.TargetTags.Contains(tag)) return;

            aggroBuddy.Behaviour.AggroTargetSystem.TargetTags.Add(tag);
        }
    }
}
