using UnityEngine;

public class SoundControl : MonoBehaviour
{
    public static SoundControl Instance { get; private set; }

    private AudioSource audioSource;

    [Header("AudioClips")]
    [SerializeField] private AudioClip[] buttonSounds;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip[] moveSounds;
    [SerializeField] private AudioClip[] itemSounds;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }
    public void PlaySound(string what, int index = -1, bool moved = true)
    {
        switch (what)
        {
            case "Button":
                audioSource.PlayOneShot(buttonSounds[0]);
                break;
            case "Buy":
                audioSource.PlayOneShot(buttonSounds[1]);
                break;
            case "Item":
                audioSource.PlayOneShot(itemSounds[index]);
                break;
            case "Move":
                if (moved)
                {
                    audioSource.PlayOneShot(moveSounds[0]);
                }
                else
                {
                    audioSource.PlayOneShot(moveSounds[1]);
                }
                break;
            case "Win":
                audioSource.PlayOneShot(winSound);
                break;
            case "Click":
                audioSource.PlayOneShot(buttonSounds[2]);
                break;
            case "Esc":
                audioSource.Stop();
                audioSource.PlayOneShot(buttonSounds[3]);
                break;
            case "Pause":
                audioSource.Stop();
                audioSource.PlayOneShot(buttonSounds[4]);
                break;
        }
    }
}
