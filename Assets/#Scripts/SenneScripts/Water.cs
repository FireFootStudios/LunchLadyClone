using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private CharMovement _charMovement;

    [SerializeField] private MovementModifier _movementModifier;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
            _charMovement.AddOrUpdateModifier(_movementModifier);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            _charMovement.ClearModifiers();
    }
}
