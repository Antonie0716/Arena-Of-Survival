using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// HUD (vidas, contador de inimigos, indicador de chave, mensagens) e painéis
/// de pausa, derrota e vitória. Os botões são ligados por código no Awake.
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

    readonly Queue<KeyValuePair<string, float>> messageQueue = new Queue<KeyValuePair<string, float>>();
    Coroutine messageRoutine;

    void Awake()
    {
        SetActive(pausePanel, false);
        SetActive(gameOverPanel, false);
        SetActive(victoryPanel, false);
        SetActive(messageBox, false);
        SetActive(keyIndicator, false);

        Bind(resumeButton, () => { if (GameManager.Instance != null) GameManager.Instance.Resume(); });
        Bind(pauseMenuButton, () => { if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu(); });
        Bind(retryButton, () => { if (GameManager.Instance != null) GameManager.Instance.Restart(); });
        Bind(gameOverMenuButton, () => { if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu(); });
        Bind(playAgainButton, () => { if (GameManager.Instance != null) GameManager.Instance.Restart(); });
        Bind(victoryMenuButton, () => { if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu(); });
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

    static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    public void SetLives(int current, int max)
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].gameObject.SetActive(i < max);
            hearts[i].sprite = i < current ? heartFull : heartEmpty;
        }
    }

    public void SetEnemies(int defeated, int total)
    {
        if (enemiesText != null) enemiesText.text = "INIMIGOS DERROTADOS: " + defeated + "/" + total;
    }

    public void SetHasKey(bool hasKey)
    {
        SetActive(keyIndicator, hasKey);
    }

    /// <summary>Mostra uma mensagem imediatamente, descartando as que estavam na fila.</summary>
    public void ShowMessage(string text, float duration)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = null;
        messageQueue.Clear();
        messageQueue.Enqueue(new KeyValuePair<string, float>(text, duration));
        messageRoutine = StartCoroutine(ProcessMessages());
    }

    /// <summary>Adiciona uma mensagem para ser exibida depois da atual.</summary>
    public void QueueMessage(string text, float duration)
    {
        messageQueue.Enqueue(new KeyValuePair<string, float>(text, duration));
        if (messageRoutine == null) messageRoutine = StartCoroutine(ProcessMessages());
    }

    IEnumerator ProcessMessages()
    {
        while (messageQueue.Count > 0)
        {
            KeyValuePair<string, float> m = messageQueue.Dequeue();
            SetActive(messageBox, true);
            if (messageText != null) messageText.text = m.Key;
            yield return new WaitForSecondsRealtime(m.Value);
        }
        SetActive(messageBox, false);
        messageRoutine = null;
    }

    void HideMessages()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = null;
        messageQueue.Clear();
        SetActive(messageBox, false);
    }

    public void ShowPause(bool show)
    {
        SetActive(pausePanel, show);
    }

    public void ShowGameOver()
    {
        HideMessages();
        SetActive(pausePanel, false);
        SetActive(gameOverPanel, true);
    }

    public void ShowVictory()
    {
        HideMessages();
        SetActive(pausePanel, false);
        SetActive(victoryPanel, true);
    }
}
