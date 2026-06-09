using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private List<ItemData> itemPool;
    [SerializeField] private List<ShopSlot> shopSlots;
    [SerializeField] private Inventory p1Inventory;
    [SerializeField] private Inventory p2Inventory;
    [SerializeField] private Control control;
    [SerializeField] private PlayerGold p1Gold;
    [SerializeField] private PlayerGold p2Gold;
    public static Shop Instance { get; private set; }
    public Inventory GetCurrentInventory() => control.isWhiteTurn ? p1Inventory : p2Inventory;

    void Awake() { Instance = this; }

    private void Start()
    {
        for (int i = 0; i < shopSlots.Count; i++)
            shopSlots[i].Initialize(this, i);

        Reroll();
    }

    // Buy
    public void OnSlotClicked(int slotIndex)
    {
        if (Control.Instance.IsGameOver) return;
        if (Control.Instance.IsPaused) return;
        if (Control.Instance.IsChangingTurn) return;
        ShopSlot slot = shopSlots[slotIndex];
        if (slot.Item == null) return;

        PlayerGold currentGold = control.isWhiteTurn ? p1Gold : p2Gold;

        if (currentGold.Gold < slot.Item.BuyPrice)
        {
            control.StartCoroutine(control.WriteStateText("골드가 부족합니다!"));
            SoundControl.Instance.PlaySound("Click");
            return;
        }

        Inventory inventory = control.isWhiteTurn ? p1Inventory : p2Inventory;
        if (slot.Item.ItemType != ItemType.Reroll && slot.Item.ItemType != ItemType.Call && inventory.IsFull())
        {
            control.StartCoroutine(control.WriteStateText("인벤토리가 가득 찼습니다!"));
            SoundControl.Instance.PlaySound("Click");
            return;
        }
        if (slot.Item.ItemType == ItemType.Call && !Control.Instance.HasEmptyCallTile(control.isWhiteTurn))
        {
            control.StartCoroutine(control.WriteStateText("빈 타일이 없습니다!"));
            SoundControl.Instance.PlaySound("Click");
            return;
        }
        if (slot.Item.ItemType == ItemType.Reroll || slot.Item.ItemType == ItemType.Call)
            slot.Item.Use(control.isWhiteTurn);
        else
            inventory.GiveItem(slot.Item);

        SoundControl.Instance.PlaySound("Buy");
        currentGold.SpendGold(slot.Item.BuyPrice);
        ReplaceSlot(slotIndex);
    }

    // Replace one slot
    private void ReplaceSlot(int slotIndex)
    {
        List<ItemData> exclude = shopSlots
            .Where((s, i) => i != slotIndex && s.Item != null)
            .Select(s => s.Item)
            .ToList();

        ItemData next = PickRandomItem(exclude);
        shopSlots[slotIndex].RerollAnim(next);
    }

    public void Reroll()
    {
        List<ItemData> picked = new List<ItemData>();

        for (int i = 0; i < shopSlots.Count; i++)
        {
            ItemData next = PickRandomItem(picked);
            if (next == null) break;
            picked.Add(next);
            shopSlots[i].RerollAnim(next);
        }
    }

    private ItemData PickRandomItem(List<ItemData> exclude)
    {
        List<ItemData> pool = itemPool
            .Where(item => !exclude.Contains(item))
            .ToList();

        if (pool.Count == 0) return null;

        float totalPoint = 0f;
        List<float> itemPoints = new List<float>();

        foreach (var item in pool)
        {
            itemPoints.Add(item.SpawnPoint);
            totalPoint += item.SpawnPoint;
        }

        float roll = Random.Range(0f, totalPoint);
        float sum = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            sum += itemPoints[i];
            if (roll <= sum)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }
}
