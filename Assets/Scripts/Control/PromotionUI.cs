using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PromotionUI : MonoBehaviour
{
    public static PromotionUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button bishopButton;
    [SerializeField] private Button rookButton;

    [SerializeField] private Sprite whiteBishop;
    [SerializeField] private Sprite whiteRook;
    [SerializeField] private Sprite blackBishop;
    [SerializeField] private Sprite blackRook;

    private Units targetUnit;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(Units unit, bool isWhite)
    {
        targetUnit = unit;
        panel.SetActive(true);

        if (isWhite)
        {
            bishopButton.image.sprite = whiteBishop;
            rookButton.image.sprite = whiteRook;
        }
        else
        {
            bishopButton.image.sprite = blackBishop;
            rookButton.image.sprite = blackRook;
        }
            bishopButton.onClick.RemoveAllListeners();
        rookButton.onClick.RemoveAllListeners();

        bishopButton.onClick.AddListener(() => Promote(UnitType.Bishop));
        rookButton.onClick.AddListener(() => Promote(UnitType.Rook));
    }

    private void Promote(UnitType type)
    {
        Control.Instance.PromoteUnit(targetUnit, type);
        panel.SetActive(false);
    }
}
