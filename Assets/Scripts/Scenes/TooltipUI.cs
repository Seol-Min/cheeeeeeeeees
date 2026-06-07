using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [SerializeField] private Vector2 offset = new Vector2(16f, -16f);

    private RectTransform rect;

    private void Awake()
    {
        Instance = this;
        rect = panel.GetComponent<RectTransform>();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel.activeSelf) MoveToMouse();
    }

    public void Show(ItemData item)
    {
        tooltipText.text = $"{item.ItemName}  |  {item.BuyPrice}G\n{item.Description}";
        panel.SetActive(true);
        MoveToMouse();
    }

    public void Hide() => panel.SetActive(false);

    private void MoveToMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.x += offset.x * 0.01f;
        worldPos.y += offset.y * 0.01f;
        rect.position = worldPos;
    }
}