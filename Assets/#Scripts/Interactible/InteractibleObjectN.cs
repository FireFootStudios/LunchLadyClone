using System.Collections.Generic;
using UnityEngine;

public class InteractibleObjectN : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _firstAnimTrigger = null;
    [SerializeField] private string _secondAnimTrigger = null;

    private bool _isPlayingAnim = false;

    public void PlayAnimOnInteract()
    {
        foreach (Animator animator in _animators)
        { 
            if(_firstAnimTrigger != null && !_isPlayingAnim)
            {
                animator.SetTrigger(_firstAnimTrigger);
                if (_secondAnimTrigger !=null)
                    _isPlayingAnim = true;
            } 
            else if (_secondAnimTrigger != null && _isPlayingAnim)
            {
                animator.SetTrigger(_secondAnimTrigger);
                _isPlayingAnim = false;
            }
        }
    }
}
