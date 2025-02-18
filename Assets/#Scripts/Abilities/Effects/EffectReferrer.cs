using UnityEngine;


//Effects can look for this comp on targets to see if they should actually be targetting something else (or also target it)
public sealed class EffectReferrer : MonoBehaviour
{
    [SerializeField, Tooltip("To refer object")] private GameObject _referToGo = null;

    public GameObject ReferToGo {  get { return _referToGo; } set { _referToGo = value; } }
}
