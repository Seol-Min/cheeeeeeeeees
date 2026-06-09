using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Control control;
    [SerializeField] private List<Slot> slots;
    [SerializeField] private bool isWhiteInventory;

    private void Start()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].Initialize(this, i);
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (Control.Instance.IsGameOver) return;
        if (Control.Instance.IsPaused) return;
        if (Control.Instance.IsChangingTurn) return;
        if (isWhiteInventory != control.isWhiteTurn) return;

        Slot slot = slots[slotIndex];
        if (slot.Item == null) return;

        ItemData item = slot.Item;
        TooltipUI.Instance?.Hide();
        slot.ClearSlot();
        item.Use(control.isWhiteTurn, this, slotIndex);
    }

    public bool AddItem(ItemData item)
    {
        Slot emptySlot = slots.Find(s => s.Item == null);

        emptySlot.AddItem(item);
        return true;
    }

    public void ReturnItem(ItemData item, int slotIndex)
    {
        slots[slotIndex].AddItem(item);
        SoundControl.Instance.PlaySound("Click");
    }

    public bool IsFull() => slots.TrueForAll(s => s.Item != null);

    public void GiveItem(ItemData item)
    {
        if (item is InstantItemData)
            item.Use(control.isWhiteTurn);
        else
            AddItem(item);
    }
}