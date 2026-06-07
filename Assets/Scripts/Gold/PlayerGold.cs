using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using static UnityEditor.PlayerSettings; //for editor

public class PlayerGold : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mGoldText;
    [SerializeField] private int startGold;
    [SerializeField] private Image image;

    private Animator animator;
    private RectTransform rect;
    private Vector2 pos;

    private int mGold;
    public int Gold { get { return mGold; } }

    private void Start()
    {
        rect = image.GetComponent<RectTransform>();
        pos = rect.anchoredPosition;
        animator = image.GetComponent<Animator>();
        mGold = startGold;
        UpdateUI();
    }

    private void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Gold_earn"))
        {
            image.rectTransform.sizeDelta = new Vector2(159, 165);
            pos.y = 226.6f;
            rect.anchoredPosition = pos;
        }
        else
        {
            image.rectTransform.sizeDelta = new Vector2(120, 120);
            pos.y = 218.1f;
            rect.anchoredPosition = pos;
        }
    }

    public void AddGold(int amount)
    {
        animator.SetBool("earn", true);
        StopAllCoroutines();
        StartCoroutine(CountMoney(amount));
        UpdateUI();
    }

    // Call when buying
    public bool SpendGold(int amount)
    {
        mGold -= amount;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        mGoldText.text = mGold.ToString();
    }
    
    private IEnumerator CountMoney(int amount)
    {
        while (amount > 0)
        {
            mGold++;
            UpdateUI();
            amount--;
            yield return new WaitForSeconds(0.09f);
        }
        animator.SetBool("earn", false);
    }
}