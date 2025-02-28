using System.Collections.Generic;
using UnityEngine;

public class InteractibleObjectN : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _animTrigger = null;
    [SerializeField] private GameObject _itemInsideChest;

    public void PlayAnimOnInteract()
    {
        foreach (Animator animator in _animators)
        { 
            if(_animTrigger != null)
            {
                animator.SetTrigger(_animTrigger);
                ShowItemWhenOpened();
            } 
        }
    }

    private void ShowItemWhenOpened()
    {
        if (_itemInsideChest != null)
            _itemInsideChest.SetActive(true);
    }
}
