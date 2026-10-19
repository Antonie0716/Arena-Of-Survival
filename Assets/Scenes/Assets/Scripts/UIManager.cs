using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a interface do jogo:
/// vidas, inimigos derrotados, chave,
/// mensagens, pausa, derrota e vitória.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public Text enemiesText;
    public GameObject keyIndicator;

    [Header("Mensagens")]
    public GameObject messageBox;
    public Text messageText;

    [Header("Painéis")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Botões")]
    public Button resumeButton;
    public Button pauseMenuButton;
    public Button retryButton;
    public Button gameOverMenuButton;
    public Button playAgainButton;
    public Button victoryMenuButton;

    // Fila de mensagens que serão exibidas.
    Queue<KeyValuePair<string, float>> messageQueue =
        new Queue<KeyValuePair<string, float>>();

    Coroutine messageRoutine;

    void Awake()
    {
        SetActive(pausePanel, false);
        SetActive(gameOverPanel, false);
        SetActive(victoryPanel, false);
        SetActive(messageBox, false);
        SetActive(keyIndicator, false);

        // Liga cada botão a um método.
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeButton);
        }

        if (pauseMenuButton != null)
        {
            pauseMenuButton.onClick.AddListener(PauseMenuButton);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryButton);
        }

        if (gameOverMenuButton != null)
        {
            gameOverMenuButton.onClick.AddListener(GameOverMenuButton);
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgainButton);
        }

        if (victoryMenuButton != null)
        {
            victoryMenuButton.onClick.AddListener(VictoryMenuButton);
        }
    }

    // =================================================
    // BOTÕES
    // =================================================

    void ResumeButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resume();
        }
    }

    void PauseMenuButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    void RetryButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
    }

    void GameOverMenuButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    void PlayAgainButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
    }

    void VictoryMenuButton()
    {
        AudioManager.Play(Sfx.Click);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    /// <summary>
    /// Ativa ou desativa um objeto da interface.
    /// </summary>
    static void SetActive(GameObject gameObject, bool value)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(value);
        }
    }

    /// <summary>
    /// Atualiza os corações mostrados na tela.
    /// </summary>
    public void SetLives(int current, int max)
    {
        if (hearts == null)
        {
            return;
        }

        // Percorre todas as posições do array de corações.
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                // Verifica se o coração deve aparecer.
                if (i < max)
                {
                    hearts[i].gameObject.SetActive(true);
                }
                else
                {
                    hearts[i].gameObject.SetActive(false);
                }

                // Verifica se o coração está cheio ou vazio.
                if (i < current)
                {
                    hearts[i].sprite = heartFull;
                }
                else
                {
                    hearts[i].sprite = heartEmpty;
                }
            }
        }
    }

    /// <summary>
    /// Atualiza o contador de inimigos derrotados.
    /// </summary>
    public void SetEnemies(int defeated, int total)
    {
        if (enemiesText != null)
        {
            enemiesText.text =
                "INIMIGOS DERROTADOS: "
                + defeated
                + "/"
                + total;
        }
    }

    /// <summary>
    /// Mostra ou esconde o indicador da chave.
    /// </summary>
    public void SetHasKey(bool hasKey)
    {
        SetActive(
            keyIndicator,
            hasKey
        );
    }

    /// <summary>
    /// Mostra uma mensagem imediatamente.
    /// Remove as mensagens anteriores da fila.
    /// </summary>
    public void ShowMessage(
        string text,
        float duration
    )
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = null;

        messageQueue.Clear();

        KeyValuePair<string, float> message =
            new KeyValuePair<string, float>(
                text,
                duration
            );

        messageQueue.Enqueue(message);

        messageRoutine =
            StartCoroutine(
                ProcessMessages()
            );
    }

    /// <summary>
    /// Adiciona uma nova mensagem para
    /// aparecer depois da mensagem atual.
    /// </summary>
    public void QueueMessage(
        string text,
        float duration
    )
    {
        KeyValuePair<string, float> message =
            new KeyValuePair<string, float>(
                text,
                duration
            );

        messageQueue.Enqueue(message);

        if (messageRoutine == null)
        {
            messageRoutine =
                StartCoroutine(
                    ProcessMessages()
                );
        }
    }

    /// <summary>
    /// Exibe as mensagens que estão esperando.
    /// </summary>
    IEnumerator ProcessMessages()
    {
        // Continua enquanto houver mensagens na fila.
        while (messageQueue.Count > 0)
        {
            KeyValuePair<string, float> currentMessage =
                messageQueue.Dequeue();

            SetActive(
                messageBox,
                true
            );

            if (messageText != null)
            {
                messageText.text =
                    currentMessage.Key;
            }

            yield return new WaitForSecondsRealtime(
                currentMessage.Value
            );
        }

        SetActive(
            messageBox,
            false
        );

        messageRoutine = null;
    }

    /// <summary>
    /// Remove e esconde todas as mensagens.
    /// </summary>
    void HideMessages()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = null;

        messageQueue.Clear();

        SetActive(
            messageBox,
            false
        );
    }

    /// <summary>
    /// Mostra ou esconde a tela de pausa.
    /// </summary>
    public void ShowPause(bool show)
    {
        SetActive(
            pausePanel,
            show
        );
    }

    /// <summary>
    /// Mostra a tela de derrota.
    /// </summary>
    public void ShowGameOver()
    {
        HideMessages();

        SetActive(
            pausePanel,
            false
        );

        SetActive(
            gameOverPanel,
            true
        );
    }

    /// <summary>
    /// Mostra a tela de vitória.
    /// </summary>
    public void ShowVictory()
    {
        HideMessages();

        SetActive(
            pausePanel,
            false
        );

        SetActive(
            victoryPanel,
            true
        );
    }
}