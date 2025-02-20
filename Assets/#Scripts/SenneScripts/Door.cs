using NUnit.Framework.Constraints;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Door : MonoBehaviour
{


    public enum DoorColor
    {
        Green, Blue,Red
    }

    public DoorColor color;

    private void Awake()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
       if(other.TryGetComponent<Inventory>(out var inventory))
        {
            TryOpenDoor(inventory);
        }
    }


    private void TryOpenDoor(Inventory inventory)
    {
        switch (this.color)
        {
            case DoorColor.Green:
                if(inventory.GreenKey.gameObject != null)
                    this.gameObject.SetActive(false);
            break;
            case DoorColor.Blue:
                if (inventory.BlueKey.gameObject != null)
                    this.gameObject.SetActive(false);
                break;
            case DoorColor.Red:
                if (inventory.RedKey.gameObject != null)
                    this.gameObject.SetActive(false);
                break;

        }
        
    }

}
