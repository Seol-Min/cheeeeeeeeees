using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] arrAudio;
    // check save file
    private bool hasSaveData;

    void Start()
    {
        // active continue
        hasSaveData = PlayerPrefs.HasKey("SaveExists");
        audioSource = GetComponent<AudioSource>();
        settingsPanel.SetActive(false);
    }

    public void OnStartButton()
    {
        // new game
        audioSource.Stop();
        PlayerPrefs.SetInt("SaveExists", 1);
        AudioClip audio = arrAudio[1];
        audioSource.Play();
        SceneManager.LoadScene("GameScene");
    }

    public void OnSettingsButton()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuitButton()
    {
        AudioClip audio = arrAudio[0];
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
