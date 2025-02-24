using System.Collections;
using UnityEngine;

public class JackBoxTrap : MonoBehaviour
{
    [SerializeField] private GameObject _scaryObject;
    [SerializeField] private float _removeBoxDuration;
    [SerializeField] private MovementModifier _movementModifier;


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent<PlayerN>(out PlayerN player))
        {
            if(_scaryObject != null)
            {
                _scaryObject.SetActive(true);
                StartCoroutine(StunPlayer(player));
                this.GetComponent<Collider>().enabled = false;
            }
        }
    }

    IEnumerator StunPlayer(PlayerN player)
    {
        _scaryObject.transform.LookAt(player.transform.position);
        player.Movement.AddOrUpdateModifier(_movementModifier);

        yield return new WaitForSeconds(_removeBoxDuration);

        this.gameObject.SetActive(false);
    }
}
