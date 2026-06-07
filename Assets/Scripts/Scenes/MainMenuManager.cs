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
        PlayerPrefs.SetInt("SaveExists", 1);
        SceneManager.LoadScene("GameScene");
    }

    public void OnSettingsButton()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuitButton()
    {
        Application.Quit();
        //UnityEditor.EditorApplication.isPlaying = false; // for editor
    }

    public void OnContinueButton()
    {

    }

    public void OnExitButton()
    {
         SceneManager.LoadScene("MainMenu");
    }
}
