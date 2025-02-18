using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Slot : MonoBehaviour
{
    public Action<Slot> OnClick;

    protected virtual void Awake()
    {
        Button button = GetComponentInChildren<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    protected virtual void OnButtonClick()
    {
        OnClick?.Invoke(this);
    }
}
