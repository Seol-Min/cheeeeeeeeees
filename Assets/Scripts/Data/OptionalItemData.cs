using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Optional")]
public class OptionalItemData : ItemData
{
    public override void Use(bool isWhiteTurn, Inventory inventory = null, int slotIndex = -1)
    {
        switch (ItemType)
        {
            case ItemType.Reward:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, true, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, u => !(u.bonusCaptureGold > 0 || u.poop)))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) => { target.bonusCaptureGold += 2; target.UpdateState(); };
                Control.Instance.TargetItem(isWhiteTurn, true, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, false, u => !(u.bonusCaptureGold > 0 || u.poop), this, slotIndex, inventory);
                break;

            case ItemType.Poop:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, u => !u.poop))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) => { target.poop = true; target.bonusCaptureGold = 0; target.UpdateState(); };
                Control.Instance.TargetItem(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, false, u => !u.poop, this, slotIndex, inventory);
                break;

            case ItemType.Back:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, true, null, u => { return u.previousPosition != -Vector2Int.one && u.isArrested < 0; }, true))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) =>
                {
                    Control.Instance.MoveTo(target, target.previousPosition.x, target.previousPosition.y);
                };
                Control.Instance.TargetItem(isWhiteTurn, true, null, true, u => { return u.previousPosition != -Vector2Int.one && u.isArrested < 0; }, this, slotIndex, inventory);
                break;

            case ItemType.Arrest:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, true, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, u => u.isArrested < 0))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) => { target.isArrested = 1; target.UpdateState(); Control.Instance.UpdateCheckState(); };
                Control.Instance.TargetItem(isWhiteTurn, true, new List<UnitType> { UnitType.Pawn, UnitType.Knight, UnitType.Bishop, UnitType.Rook, UnitType.Queen }, false, u => u.isArrested < 0, this, slotIndex, inventory);
                break;

            case ItemType.Promotion:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn, UnitType.Knight }))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) =>
                {
                    switch (target.data.Type)
                    {
                        case UnitType.Pawn:
                            Control.Instance.PromoteUnit(target, UnitType.Knight);
                            break;
                        case UnitType.Knight:
                            PromotionUI.Instance.Show(target, Control.Instance.isWhiteTurn);
                            break;
                    }
                };
                Control.Instance.TargetItem(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn, UnitType.Knight }, false, null, this, slotIndex, inventory);
                break;

            case ItemType.Crown:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn }, u => !u.hasCrown))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) => { target.hasCrown = true; target.UpdateState(); };
                Control.Instance.TargetItem(isWhiteTurn, false, new List<UnitType> { UnitType.Pawn }, false, u => !u.hasCrown, this, slotIndex, inventory);
                break;

            case ItemType.Queen:
                if (!Control.Instance.HasValidTarget(isWhiteTurn, false, new List<UnitType> { UnitType.Bishop, UnitType.Rook }))
                {
                    Control.Instance.StartCoroutine(Control.Instance.WriteStateText("대상이 없습니다!"));
                    inventory?.ReturnItem(this, slotIndex);
                    return;
                }
                Control.Instance.onTargetSelected = (target) =>
                {
                    Control.Instance.PromoteUnit(target, UnitType.Queen);
                };
                Control.Instance.TargetItem(isWhiteTurn, false, new List<UnitType> { UnitType.Bishop, UnitType.Rook }, false, null, this, slotIndex, inventory);
                break;
        }
    }
}