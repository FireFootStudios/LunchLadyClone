using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAnimationPlayer : MonoBehaviour
{
    [SerializeField] private List<Animator> _animators = new List<Animator>();
    [SerializeField] private string _playAnimTrigger;
    [SerializeField] private string _stopAnimTrigger;
    [SerializeField] private float _timerBetweenTriggers = 0.5f;

    private bool _coRoutineIsRunning = false;


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent<PlayerN>(out PlayerN player) && !_coRoutineIsRunning)
        {
            foreach(Animator animator in _animators)
            {
                animator.SetTrigger(_playAnimTrigger);
            }
            StartCoroutine(WaitBetweenAnims());
        }
    }

    IEnumerator WaitBetweenAnims()
    {
        _coRoutineIsRunning = true;

        yield return new WaitForSeconds(_timerBetweenTriggers);

        foreach (Animator animator in _animators)
        {
            animator.SetTrigger(_stopAnimTrigger);
        }

        _coRoutineIsRunning = false;
    }
}
