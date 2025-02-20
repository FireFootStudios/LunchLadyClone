using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    
    [SerializeField] private float _pickupDistance = 3f;
   [HideInInspector] public GameObject RedKey,BlueKey,GreenKey = null;


    private void Awake()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {


    }

    void Update()
    {
        // Check for the pickup key (E key in this case)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupLoot();
        }
    }

    void TryPickupLoot()
    {
        // Perform a raycast to detect objects in front of the player
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _pickupDistance))
        {
            //Loot
            if (hit.collider.TryGetComponent<Key>(out var keyItem))
            {
                // Pick up the log
                PickUp(keyItem.gameObject);
                AddToInventory(keyItem);
            }
        }
    }

    void PickUp(GameObject loot)
    {

        loot.SetActive(false);


        
        

    }

    void AddToInventory(Key key)
    {
        switch (key.KeyColorValue)
        {
            case Key.KeyColor.Blue:
                BlueKey = key.gameObject;
                break;
            case Key.KeyColor.Red:
                RedKey = key.gameObject;
                break;
            case Key.KeyColor.Green:
                GreenKey = key.gameObject;
                break;
        }
        
        UpdateUI();

    }
}
