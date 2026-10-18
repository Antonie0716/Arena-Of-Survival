using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu principal: JOGAR, COMO JOGAR e SAIR. Funciona com o mouse.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "Arena";

    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject howToPlayPanel;

    [Header("Botões")]
    public Button playButton;
    public Button howToPlayButton;
    public Button quitButton;
    public Button backButton;

    void Awake()
    {
        Time.timeScale = 1f;
        Bind(playButton, Play);
        Bind(howToPlayButton, ShowHowToPlay);
        Bind(quitButton, Quit);
        Bind(backButton, ShowMain);
        ShowMain();
    }

    void Update()
    {
        if (howToPlayPanel != null && howToPlayPanel.activeSelf && GameInput.PausePressed)
            ShowMain();
    }

    static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            AudioManager.Play(Sfx.Click);
            action();
        });
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowHowToPlay()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
