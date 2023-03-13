using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;

public class MainMenuScript : MonoBehaviour
{
    [Header("Ui objects")]
    public AudioSource menuASource;
    public AudioClip[] clickSounds;
    [Space]
    public GameObject tutorialPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    [Space]
    public Button startGameButton;
    public Button tutorialButton;
    public Button settingsPanelButton;
    public Button creditsButton;
    public Button quitGameButton;
    [Space]
    public Button closeSettingsButton;
    public Button closeTutorialButton;
    public Button closeCreditsButton;
    [Space]
    public Slider gameVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider masterVolumeSlider;
    public Slider sensitivitySlider;
    [Space]
    public TextMeshProUGUI scoreText;
    [Header("Extra settings")]
    public float volumeChangeThreshold;
    public AudioMixer audioMix;
    [Space]
    public string gameAudioParameter;
    public string musicAudioParameter;
    public string masterVolumeParameter;



    public void StartGame()
    {
        PlayerClickSound();
        SceneManager.LoadScene("GameScene");
    }

    bool tutorialPanelOpen = false;
    public void FlipTutorialPanel()
    {
        PlayerClickSound();
        if (tutorialPanelOpen == true)
        {
            tutorialPanel.SetActive(false);
            tutorialPanelOpen = false;
        }
        else
        {
            tutorialPanel.SetActive(true);
            tutorialPanelOpen = true;
        }
    }

    public void CloseTutorialPanel()
    {
        PlayerClickSound();
        tutorialPanel.SetActive(false);
        tutorialPanelOpen = false;
    }

    bool settingsPanelOpen = false;
    public void FlipSettingsPanel()
    {
        PlayerClickSound();
        if (settingsPanelOpen)
        {
            settingsPanel.SetActive(false);
            settingsPanelOpen = false;
        }
        else
        {
            settingsPanel.SetActive(true);
            settingsPanelOpen = true;
        }
    }

    public void CloseSettingsPanel()
    {
        PlayerClickSound();
        settingsPanel.SetActive(false);
        settingsPanelOpen = false;
    }


    bool creditsPanelOpen = false;
    public void FlipCreditsPanel()
    {
        PlayerClickSound();
        if (creditsPanelOpen)
        {
            creditsPanel.SetActive(false);
            creditsPanelOpen = false;
        }
        else
        {
            creditsPanel.SetActive(true);
            creditsPanelOpen = true;
        }
    }

    public void CloseCreditsPanel()
    {
        PlayerClickSound();
        creditsPanel.SetActive(true);
        creditsPanelOpen = true;
    }


    // Start is called before the first frame update
    void Start()
    {
        //disable active element
        tutorialPanel.SetActive(false);
        tutorialPanelOpen = false;

        settingsPanel.SetActive(false);
        settingsPanelOpen = false;

        creditsPanel.SetActive(false);
        creditsPanelOpen = false;

        //setup buttons
        startGameButton.onClick.AddListener(StartGame);
        tutorialButton.onClick.AddListener(FlipTutorialPanel);
        settingsPanelButton.onClick.AddListener(FlipSettingsPanel);
        closeCreditsButton.onClick.AddListener(CloseCreditsPanel);


        closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        closeTutorialButton.onClick.AddListener(CloseTutorialPanel);
        creditsButton.onClick.AddListener(FlipCreditsPanel);

        quitGameButton.onClick.AddListener(OnQuitGamePressed);

        //setup sliders
        gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChange);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChange);
        sensitivitySlider.onValueChanged.AddListener(OnSensitityChange);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        //load settngs
        LoadPreferences();
    }

    void LoadPreferences()
    {
        SetScoreText(PlayerPrefs.GetInt("HighScore"));

        curSensitivityMultiplier = PlayerPrefs.GetFloat("sensitivity", 1f);
        sensitivitySlider.value = curSensitivityMultiplier;

        curGameMusicMultiplier = PlayerPrefs.GetFloat("musicVolume", 0f);
        musicVolumeSlider.value = curGameMusicMultiplier;

        curGameVolueMutliplier = PlayerPrefs.GetFloat("gameVolume", 0f);
        gameVolumeSlider.value = curGameVolueMutliplier;

        curMasterVolumeMultiplier = PlayerPrefs.GetFloat("masterVolume", 0f);
        masterVolumeSlider.value = curMasterVolumeMultiplier;

        SetGameVolume();
        SetMusicVolume();
        SetMasterVolume();
    }

    void SetScoreText(int value)
    {
        scoreText.text = "Highscore: " + value.ToString();
    }

    private float curSensitivityMultiplier = 0f;
    public void OnSensitityChange(float newValue)
    {
        curSensitivityMultiplier = newValue;
        PlayerPrefs.SetFloat("sensitivity", newValue);
    }

    private float curGameVolueMutliplier = 0f;
    public void OnGameVolumeChange(float newValue)
    {
        curGameVolueMutliplier = newValue;
        PlayerPrefs.SetFloat("gameVolume", newValue);
        SetGameVolume();
    }

    private float curGameMusicMultiplier = 0f;
    public void OnMusicVolumeChange(float newValue)
    {
        curGameMusicMultiplier = newValue;
        PlayerPrefs.SetFloat("musicVolume", newValue);
        SetMusicVolume();
    }

    float curMasterVolumeMultiplier = 0f;
    public void OnMasterVolumeChanged(float newValue)
    {
        curMasterVolumeMultiplier = newValue;
        PlayerPrefs.SetFloat("masterVolume", newValue);
        SetMasterVolume();
    }

    void PlayerClickSound()
    {
        AudioClip targClip = clickSounds[Random.Range(0, clickSounds.Length)];
        menuASource.PlayOneShot(targClip);
    }

    void SetGameVolume()
    {
        audioMix.SetFloat(gameAudioParameter, 0f + (curGameVolueMutliplier * volumeChangeThreshold));
    }

    void SetMusicVolume()
    {
        audioMix.SetFloat(musicAudioParameter, 0f + (curGameMusicMultiplier * volumeChangeThreshold));
    }

    void SetMasterVolume()
    {
        audioMix.SetFloat(masterVolumeParameter, 0f + (curMasterVolumeMultiplier * volumeChangeThreshold));
    }

    public void OnQuitGamePressed()
    {
        Application.Quit();
    }
}
