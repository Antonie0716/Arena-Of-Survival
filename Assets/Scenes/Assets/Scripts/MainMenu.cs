using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla o menu principal do jogo.
/// Possui as opções Jogar, Como Jogar e Sair.
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

        // Liga cada botão ao método correspondente.
        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayButton);
        }

        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.AddListener(HowToPlayButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitButton);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackButton);
        }

        ShowMain();
    }

    void Update()
    {
        // Se estiver na tela "Como Jogar",
        // ESC volta para o menu principal.
        if (howToPlayPanel != null)
        {
            if (howToPlayPanel.activeSelf)
            {
                if (GameInput.PausePressed)
                {
                    ShowMain();
                }
            }
        }
    }

    /// <summary>
    /// Ação do botão Jogar.
    /// </summary>
    void PlayButton()
    {
        AudioManager.Play(Sfx.Click);

        Play();
    }

    /// <summary>
    /// Ação do botão Como Jogar.
    /// </summary>
    void HowToPlayButton()
    {
        AudioManager.Play(Sfx.Click);

        ShowHowToPlay();
    }

    /// <summary>
    /// Ação do botão Voltar.
    /// </summary>
    void BackButton()
    {
        AudioManager.Play(Sfx.Click);

        ShowMain();
    }

    /// <summary>
    /// Ação do botão Sair.
    /// </summary>
    void QuitButton()
    {
        AudioManager.Play(Sfx.Click);

        Quit();
    }

    /// <summary>
    /// Carrega a cena principal do jogo.
    /// </summary>
    public void Play()
    {
        SceneManager.LoadScene(
            gameSceneName
        );
    }

    /// <summary>
    /// Mostra a tela Como Jogar.
    /// </summary>
    public void ShowHowToPlay()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Mostra o menu principal.
    /// </summary>
    public void ShowMain()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Fecha o jogo.
    /// </summary>
    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}