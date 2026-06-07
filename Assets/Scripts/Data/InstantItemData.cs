using UnityEngine;

[CreateAssetMenu(menuName = "Item/Instant")]
public class InstantItemData : ItemData
{
    public override void Use(bool isWhiteTurn, Inventory inventory = null, int slotIndex = -1)
    {
        switch (ItemType)
        {
            case ItemType.Reroll:
                Shop.Instance.Reroll();
                break;
            case ItemType.Call:
                Control.Instance.Call(isWhiteTurn);
                break;
        }
    }
}
