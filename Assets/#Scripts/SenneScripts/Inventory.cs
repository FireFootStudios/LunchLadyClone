using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int AmountOfWood { get { return _inventoryWood.Count; } }

    [SerializeField] private TextMeshProUGUI _amountOfWood,_amountOfScrap,_amountOfPlastic;
    [SerializeField] private float _pickupDistance = 3f;
    private List<GameObject> _inventoryWood = new List<GameObject>();
    private List<GameObject> _inventoryPlastic = new List<GameObject>();
    private List<GameObject> _inventoryScrap = new List<GameObject>();

    [Space]
    [SerializeField] private Raft _Raft = null;


    private void Awake()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
      // _amountOfWood.text = _inventoryWood.Count.ToString();
      // _amountOfScrap.text = _inventoryScrap.Count.ToString();
      // _amountOfPlastic.text = _inventoryPlastic.Count.ToString();
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
            if (hit.collider.TryGetComponent<Loot>(out var lootItem))
            {
                // Pick up the log
                PickUp(lootItem.gameObject);
                AddToInventory(lootItem);
            }
            //Raft Start and stoppings
            else if(hit.collider.TryGetComponent<Motor>(out var motor))
            {
                if (_Raft.IsMoving)
                { 
                    _Raft.StopRaft();
                }
                else
                {
                    _Raft.StartRaft();
                }
            }
        }
    }

    void PickUp(GameObject loot)
    {

        loot.SetActive(false);


        
        

    }

    void AddToInventory(Loot item)
    {
        switch (item.LootTypeValue)
        {
            case Loot.LootType.Wood:
                _inventoryWood.Add(item.gameObject);
                break;
            case Loot.LootType.Scrap:
                _inventoryScrap.Add(item.gameObject);
                break;
            case Loot.LootType.Plastic:
                _inventoryPlastic.Add(item.gameObject);
                break;
        }
        
        UpdateUI();

    }
}
