using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ShopSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ItemData mItem;
    public ItemData Item { get { return mItem; } }

    private Shop mShop;
    private int mSlotIndex;
    private bool isRerolling = false;

    [SerializeField] private Image mItemImage;
    [SerializeField] private GameObject rerollEffect;

    public void Initialize(Shop shop, int index)
    {
        mShop = shop;
        mSlotIndex = index;
    }

    public void SetItem(ItemData item)
    {
        mItem = item;
        
        if (item == null)
        {
            mItemImage.sprite = null;
            return;
        }

        mItemImage.sprite = item.Icon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isRerolling) return;
        mShop.OnSlotClicked(mSlotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mItem == null) return;
        mItemImage.sprite = mItem.HoverIcon != null ? mItem.HoverIcon : mItem.Icon;
        TooltipUI.Instance?.Show(mItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mItem == null) return;
        mItemImage.sprite = mItem.Icon;
        TooltipUI.Instance?.Hide();
    }

    public void RerollAnim(ItemData item)
    {
        StartCoroutine(RerollCoroutine(item));
    }

    private IEnumerator RerollCoroutine(ItemData item)
    {
        isRerolling = true;
        mItemImage.enabled = false;
        rerollEffect.SetActive(true);
        Animator anim = rerollEffect.GetComponent<Animator>();
        anim.SetTrigger("Reroll");
        yield return null;
        float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength - 0.5f);
        rerollEffect.SetActive(false);
        SetItem(item);
        mItemImage.enabled = true;
        isRerolling = false;
    }
}