using System.Collections.Generic;
using UnityEngine;

public class InteractibleObjectN : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;

    private bool _playedAnim = false;

    public void PlayAnimOnInteract()
    {
        foreach (Animator animator in _animators)
        {
            if (_firstAnimTrigger != null && !_playedAnim)
            {
                animator.SetTrigger(_firstAnimTrigger);
                if (_secondAnimTrigger != null)
                    _playedAnim = true;
            }
            else if (_secondAnimTrigger != null && _playedAnim)
            {
                animator.SetTrigger(_secondAnimTrigger);
                _playedAnim = false;
            }
        }
    }

    public bool CanInteract()
    {
        if (_firstAnimTrigger == null) return false;
        if (_playedAnim && _secondAnimTrigger == null) return false;
        return true;
    }
}
