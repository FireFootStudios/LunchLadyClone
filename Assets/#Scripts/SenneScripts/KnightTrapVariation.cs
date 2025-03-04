using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightTrapVariation : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _playAnimTrigger;
    [SerializeField] private string _stopAnimTrigger;

    [SerializeField] private bool _isActivated = true;

    private void Awake()
    {
        ActivateButton();
    }
    private void ActivateButton()
    {
        if (_isActivated)
        {
            foreach (Animator animator in _animators)
            {
                animator.SetTrigger(_stopAnimTrigger);

            }
            _isActivated = false;

        }
        else if (!_isActivated)
        {
            foreach (Animator animator in _animators)
            {
                animator.SetTrigger(_playAnimTrigger);
            }
            _isActivated = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent<PlayerN>(out PlayerN player))
            ActivateButton();
    }



}
