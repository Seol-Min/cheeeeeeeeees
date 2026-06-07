using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ItemData mItem;
    public ItemData Item { get { return mItem; } }

    private Inventory mInventory;
    private int mSlotIndex;

    [SerializeField] private Image mItemImage;

    public void Initialize(Inventory inventory, int index)
    {
        Refresh();
        mInventory = inventory;
        mSlotIndex = index;
    }
    public void AddItem(ItemData item)
    {
        mItem = item;
        mItemImage.sprite = mItem.Icon;
        Refresh();
    }
    public void ClearSlot()
    {
        mItem = null;
        mItemImage.sprite = null;
        Refresh();
    }
    private void Refresh()
    {
        bool hasItem = mItem != null;
        gameObject.SetActive(hasItem);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        mInventory.OnSlotClicked(mSlotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mItem == null) return;
        mItemImage.sprite = mItem.HoverIcon != null ? mItem.HoverIcon : mItem.Icon;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mItem == null) return;
        mItemImage.sprite = mItem.Icon;
    }
}
