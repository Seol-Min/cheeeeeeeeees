using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Shop/ItemData")]
public class ItemData : ScriptableObject
{

    [Header("Main Info")]
    [SerializeField] private ItemType mItemType;
    [SerializeField] private string mItemName;
    [TextArea] [SerializeField] private string mDescription;
    [SerializeField] private Sprite mIcon;
    [SerializeField] private Sprite mHoverIcon;

    [Header("Shop Information")]
    [SerializeField] private int buyPrice;
    [SerializeField] private float mSpawnPoint;
    public int ItemID { get { return (int)mItemType; } }
    public ItemType ItemType { get { return mItemType; } }
    public string ItemName { get { return mItemName; } }
    public string Description { get { return mDescription; } }
    public Sprite Icon { get { return mIcon; } }
    public Sprite HoverIcon { get { return mHoverIcon; } }
    public int BuyPrice { get { return buyPrice; } }
    public float SpawnPoint { get { return mSpawnPoint; } }

    public virtual void Use(bool isWhiteTurn, Inventory inventory = null, int slotIndex = -1)
    {
        Debug.Log("Using " + ItemName);
    }
}
public enum ItemType
{
    None = 0,
    Reroll = 1,
    Call = 2,
    Reward = 3,
    Poop = 4,
    Back = 5,
    Arrest = 6,
    Promotion = 7,
    Crown = 8,
    Queen = 9,
}