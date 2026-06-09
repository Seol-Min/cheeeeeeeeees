using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image leftBar;
    [SerializeField] private Image rightBar;
    [SerializeField] private Sprite normalLeft, highlightLeft;
    [SerializeField] private Sprite normalRight, highlightRight;

    public void OnPointerEnter(PointerEventData eventData)
    {
        leftBar.sprite = highlightLeft;
        rightBar.sprite = highlightRight;
        SoundControl.Instance.PlaySound("Pause");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        leftBar.sprite = normalLeft;
        rightBar.sprite = normalRight;
    }
}
