using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinUI : MonoBehaviour
{
    [SerializeField] private GameObject win1;
    [SerializeField] private GameObject win2;

    private void Awake()
    {
        win1.SetActive(false);
        win2.SetActive(false);
    }
    private void OnEnable()
    {
        StartCoroutine(WinAnimAndLoadScene());
    }

    private IEnumerator WinAnimAndLoadScene()
    {
        yield return null;
        if (Control.Instance.Win != -1)
        {
            if (Control.Instance.Win == 0)
            {
                win1.SetActive(true);
            }
            else if (Control.Instance.Win == 1)
            {
                win2.SetActive(true);
            }
            SoundControl.Instance.PlaySound("Win");
            yield return new WaitForSeconds(10f);
            SceneManager.LoadScene("MainMenu");
        }
    }
}
