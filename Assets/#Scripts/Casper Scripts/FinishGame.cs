using UnityEngine;

public class FinishGame : MonoBehaviour
{
    [SerializeField] private GameObject _inventoryGO = null;
    [SerializeField] private GameObject _finishedText = null;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent<PlayerN>(out PlayerN player))
        {
            _inventoryGO.SetActive(false);
            _finishedText.SetActive(true);
        }
    }

}
