using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    // check save file
    private bool hasSaveData;

    void Start()
    {
        // active continue
        hasSaveData = PlayerPrefs.HasKey("SaveExists");
        settingsPanel.SetActive(false);
    }

    public void OnStartButton()
    {
        // new game
        SoundControl.Instance.PlaySound("Button");
        PlayerPrefs.SetInt("SaveExists", 1);
        SceneManager.LoadScene("Tutorial_Scene");
        //SceneManager.LoadScene("GameScene");
    }

    public void OnSettingsButton()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuitButton()
    {
        SoundControl.Instance.PlaySound("Button");
        Application.Quit();
        //UnityEditor.EditorApplication.isPlaying = false; // for editor
    }

    public void OnContinueButton()
    {
        SoundControl.Instance.PlaySound("Button");
        Control.Instance.pausePanel.SetActive(false);
    }

    public void OnExitButton()
    {
        SoundControl.Instance.PlaySound("Button");
        SceneManager.LoadScene("MainMenu");
    }
}
