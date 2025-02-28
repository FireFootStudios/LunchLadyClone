using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class InventoryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _itemInfoTemplate = null;
    [SerializeField] private Transform _itemInfoContentT = null;

    private List<TextMeshProUGUI> _itemTmps = new List<TextMeshProUGUI>();
    private PlayerN _localPlayer = null;



    private void OnEnable()
    {
        _localPlayer = GameManager.Instance.SceneData.LocalPlayer;
        if (_localPlayer && _localPlayer.Inventory)
            _localPlayer.Inventory.OnItemAddClient += OnInventoryItemAdded;

        UpdateUI();
    }
    private void OnDisable()
    {
        if (_localPlayer && _localPlayer.Inventory)
            _localPlayer.Inventory.OnItemAddClient -= OnInventoryItemAdded;
    }

    private void OnInventoryItemAdded(ItemData _)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!_localPlayer || !_localPlayer.Inventory) return;
        if (!_itemInfoTemplate || !_itemInfoContentT) return;

        // Disable first
        foreach (TextMeshProUGUI tmp in _itemTmps)
            tmp.gameObject.SetActive(false);

        List<ItemData> itemData = _localPlayer.Inventory.ClientItems;
        for (int i = 0; i < itemData.Count; i++)
        {
            ItemData item = itemData[i];
            if (itemData == null) continue;

            TextMeshProUGUI tmp = null;
            if (i < _itemTmps.Count) tmp = _itemTmps[i];
            else
            {
                tmp = Instantiate(_itemInfoTemplate, _itemInfoContentT);
                _itemTmps.Add(tmp);
            }
            tmp.text = item.Name;
            tmp.gameObject.SetActive(true);
        }
    }
}