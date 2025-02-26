using System.Collections.Generic;
using UnityEngine;

public enum Tag
{
    Invalid = 0,
    Player = 1,
    Enemy = 2,
    Camera = 3,
    Item = 4,
}

public sealed class TagManager : SingletonBase<TagManager>
{
    [SerializeField] private List<TagPair> _tagPairs = null;

    private Dictionary<Tag, string> _tags = new Dictionary<Tag, string>();

    protected override void Awake()
    {
        base.Awake();

        foreach (TagPair p in _tagPairs)
        {
            _tags.Add(p.ID, p.Value);
        }
    }

    public string GetTagValue(Tag id)
    {
        if (!_tags.ContainsKey(id)) return null;

        return _tags[id];
    }

    public Tag GetTagID(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Tag.Invalid;

        TagPair tagPair = _tagPairs.Find(t => t.Value == tag);

        return tagPair != null ? tagPair.ID : Tag.Invalid;
    }
}

[System.Serializable]
public sealed class TagPair
{
    [field: SerializeField] public Tag ID { get; private set; }
    [field: SerializeField] public string Value { get; private set; }
}